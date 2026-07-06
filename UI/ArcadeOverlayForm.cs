using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.UI
{
    // EN: Overlay form for arcade mode displaying messages, countdowns and service buttons.
    // FR: Formulaire d'overlay pour le mode arcade affichant messages, décomptes et boutons de service.
    public partial class ArcadeOverlayForm : Form
    {
        // UI controls are now declared in ArcadeOverlayForm.Designer.cs
        private Label[] taskSwitcherItemLabels = null!;
        private System.Windows.Forms.Timer? _stickyFocusTimer;
        private System.Windows.Forms.Timer? _messageTimer;
        private readonly ArcadeManager? _manager;

        public event EventHandler? OperatorRequested;
        public event EventHandler? FreePlayToggleRequested;
        public event EventHandler? AddCreditsRequested;
        public event EventHandler? LockRequested;
        public event EventHandler? InterfaceRequested;

        private bool _isMiniMode = false;
        private double _baseOpacity;

        // EN: Cache for File.Exists(guardian_modal.lock) to avoid disk I/O every tick
        // FR: Cache pour File.Exists afin d'éviter des accès disque à chaque tick
        private bool _guardianActiveCache = false;
        private DateTime _guardianCacheExpiry = DateTime.MinValue;
        private static readonly string _guardianLockPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "guardian_modal.lock");

        private bool _canStealFocus = true;
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool CanStealFocus 
        { 
            get 
            {
                // EN: Block focus theft if Guardian modal is active (IPC lock in TEMP) OR if manually disabled
                // FR: Bloquer le vol de focus si la modale Guardian est active (Lock IPC dans TEMP) OU si désactivé manuellement
                return _canStealFocus && !GetCachedGuardianLockState();
            }
            set { _canStealFocus = value; }
        }

        private IntPtr _previousForegroundWindow = IntPtr.Zero;
        private bool _isClickThrough = false;
        private bool _didStealFocus = false;
        
        const int GWL_EXSTYLE = -20;
        const int WS_EX_TRANSPARENT = 0x20;
        const int WS_EX_NOACTIVATE = 0x08000000;
        const int WS_EX_LAYERED = 0x80000;
        private bool _preventActivation = false;

        public ArcadeOverlayForm(double opacity = 0.85, ArcadeManager? manager = null)
        {
            _baseOpacity = opacity;
            _manager = manager;
            
            InitializeComponent();
            InitializeOverlayState();
        }

        private void InitializeOverlayState()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.FromArgb(1, 1, 1);
            this.Opacity = Math.Min(0.99, _baseOpacity);
            this.Bounds = Screen.PrimaryScreen!.Bounds;
            
            _stickyFocusTimer = new System.Windows.Forms.Timer();
            // EN: 250ms instead of 100ms — restoring a minimized window in 250ms is imperceptible, but saves ~60% CPU on old hardware
            // FR: 250ms au lieu de 100ms — restaurer une fenêtre en 250ms est imperceptible, mais économise ~60% CPU sur ancien matériel
            _stickyFocusTimer.Interval = 250;
            _stickyFocusTimer.Tick += StickyFocusTimer_Tick;
            _stickyFocusTimer.Start();

            RepositionLabels();
        }

        private void StickyFocusTimer_Tick(object? sender, EventArgs e)
        {
            // EN: Use cached File.Exists to avoid disk I/O every 250ms
            // FR: Utiliser le cache File.Exists pour éviter un accès disque à chaque tick
            bool isGuardianActive = GetCachedGuardianLockState();
            var mgr = _manager;
            if (mgr == null || mgr.IsGuardianModalActive || isGuardianActive || mgr.IsOperatorUnlocked) return;

            if (Visible && !this.IsDisposed && _manager != null)
            {
                bool isSessionActive = _manager.IsSessionActive;
                bool closing = _manager.IsInternalClosing; // Phase 1: Session ending countdown
                bool waiting = _manager.IsTimeoutActive;    // Phase 2: Game locked, waiting for coin
                
                // EN: If game is minimized during session, countdown OR waiting phase, RESTORE IT
                // FR : Si le jeu est réduit pendant la session, le décompte OU la phase d'attente, LE RESTAURER
                if (isSessionActive || closing || waiting)
                {
                    IntPtr gameHwnd = _manager.CurrentGameHwnd;
                    bool skipRestore = false;

                    // EN: If Alt-Tab is allowed, check if the foreground window is in the whitelist
                    // FR: Si Alt-Tab est autorisé, vérifier si la fenêtre au premier plan est dans la liste blanche
                    if (isSessionActive && _manager.AllowAltTab)
                    {
                        IntPtr fgHwnd = NativeMethods.GetForegroundWindow();
                        if (fgHwnd != IntPtr.Zero && fgHwnd != gameHwnd)
                        {
                            NativeMethods.GetWindowThreadProcessId(fgHwnd, out uint fgPid);
                            try
                            {
                                string fgProcessName = System.Diagnostics.Process.GetProcessById((int)fgPid).ProcessName.ToLower();
                                if (_manager.AllowedForegroundWindows.Contains(fgProcessName) || fgProcessName == "explorer" || fgProcessName == "taskmgr")
                                {
                                    skipRestore = true;
                                }
                            }
                            catch { }
                        }
                    }

                    if (!skipRestore && gameHwnd != IntPtr.Zero && NativeMethods.IsWindow(gameHwnd) && NativeMethods.IsIconic(gameHwnd))
                    {
                        NativeMethods.ShowWindowAsync(gameHwnd, NativeMethods.SW_RESTORE);
                        
                        // EN: Only activate if the user is supposed to be playing or in the final countdown
                        if (isSessionActive || closing)
                        {
                            NativeMethods.SetForegroundWindow(gameHwnd);
                        }
                        else
                        {
                            // EN: For INSERT COIN, ensure it's restored behind BatRun without taking focus
                            NativeMethods.SetWindowPos(gameHwnd, (IntPtr)1, 0, 0, 0, 0, // HWND_BOTTOM
                                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
                        }
                    }
                }
                
                // EN: Only take focus aggressively IF the game is GONE (process dead)
                // FR: NE prendre le focus agressivement que si le jeu est MORT (processus terminé)
                bool isGameAlive = _manager != null && _manager.CurrentGameHwnd != IntPtr.Zero && NativeMethods.IsWindow(_manager.CurrentGameHwnd) && NativeMethods.IsWindowVisible(_manager.CurrentGameHwnd);
                
                if (CanStealFocus && _didStealFocus && !isSessionActive && !closing)
                {
                    // EN: If game is alive, stay passive to avoid minimization
                    // FR : Si le jeu est vivant, rester passif pour éviter la réduction
                    ForceForegroundWindow(this.Handle, activate: !isGameAlive);
                }
            }
        }

        // --- Event Handlers for Designer ---

        private void OperatorButton_Click(object? sender, EventArgs e) => OperatorRequested?.Invoke(this, EventArgs.Empty);
        private void OperatorButton_GotFocus(object? sender, EventArgs e) => this.Focus();
        private void FreePlayButton_Click(object? sender, EventArgs e) => FreePlayToggleRequested?.Invoke(this, EventArgs.Empty);
        private void FreePlayButton_GotFocus(object? sender, EventArgs e) => this.Focus();
        private void AddCreditsButton_Click(object? sender, EventArgs e) => AddCreditsRequested?.Invoke(this, EventArgs.Empty);
        private void AddCreditsButton_GotFocus(object? sender, EventArgs e) => this.Focus();
        private void LockRequested_Click(object? sender, EventArgs e) => LockRequested?.Invoke(this, EventArgs.Empty);
        private void InterfaceRequested_Click(object? sender, EventArgs e) => InterfaceRequested?.Invoke(this, EventArgs.Empty);

        // --- Core Logic ---

        public void ForceForegroundWindow(IntPtr targetHwnd, bool activate = true)
        {
            if (targetHwnd == IntPtr.Zero) return;
            
            if (NativeMethods.IsIconic(targetHwnd))
            {
                NativeMethods.ShowWindow(targetHwnd, NativeMethods.SW_RESTORE);
            }

            if (activate)
            {
                NativeMethods.AllowSetForegroundWindow(-1); 

                IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
                uint foregroundThreadId = NativeMethods.GetWindowThreadProcessId(foregroundWindow, out uint fgPid);
                uint targetThreadId = NativeMethods.GetWindowThreadProcessId(targetHwnd, out _);
                
                bool isForegroundSuspended = _manager != null && _manager.LastPausedPid == fgPid && fgPid != 0;

                if (isForegroundSuspended) return; 

                if (foregroundThreadId != 0 && foregroundThreadId != targetThreadId)
                {
                    NativeMethods.AttachThreadInput(foregroundThreadId, targetThreadId, true);
                    NativeMethods.ShowWindowAsync(targetHwnd, NativeMethods.SW_SHOW);
                    NativeMethods.BringWindowToTop(targetHwnd);
                    NativeMethods.SetForegroundWindow(targetHwnd);
                    NativeMethods.AttachThreadInput(foregroundThreadId, targetThreadId, false);
                }
                else
                {
                    NativeMethods.ShowWindowAsync(targetHwnd, NativeMethods.SW_SHOW);
                    NativeMethods.BringWindowToTop(targetHwnd);
                    NativeMethods.SetForegroundWindow(targetHwnd);
                }

                if (targetHwnd == this.Handle)
                {
                    if (CanStealFocus) { this.Activate(); this.Focus(); }
                }

                if (NativeMethods.GetForegroundWindow() != targetHwnd)
                {
                    NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[2];
                    inputs[0].type = NativeMethods.INPUT_KEYBOARD;
                    inputs[0].ki.wVk = 0x12; // VK_MENU (ALT)
                    inputs[1].type = NativeMethods.INPUT_KEYBOARD;
                    inputs[1].ki.wVk = 0x12;
                    inputs[1].ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;
                    NativeMethods.SendInput(2, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
                    NativeMethods.SetForegroundWindow(targetHwnd);
                }
            }
            else
            {
                NativeMethods.SetWindowPos(targetHwnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, 
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
            }
        }

        private bool GetCachedGuardianLockState()
        {
            if (DateTime.Now > _guardianCacheExpiry)
            {
                _guardianActiveCache = System.IO.File.Exists(_guardianLockPath);
                _guardianCacheExpiry = DateTime.Now.AddMilliseconds(1000);
            }
            return _guardianActiveCache;
        }

        public void SetMiniMode(bool mini)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetMiniMode(mini)));
                return;
            }

            _isMiniMode = mini;
            
            if (mini)
            {
                this.messageLabel.Visible = false;
                this.countdownLabel.Visible = false;
                this.operatorButton.Visible = false;
                this.freePlayButton.Visible = false;
                this.addCreditsButton.Visible = false;

                this.BackColor = Color.FromArgb(45, 45, 48);
                this.Opacity = 1.0;

                this.Size = new Size(180, 40);
                Rectangle screenBounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 800, 600);
                this.Location = new Point((screenBounds.Width - 180) / 2, 10);

                this.lockMiniButton.Location = new Point(4, 4);
                this.interfaceMiniButton.Location = new Point(115, 4);

                _isClickThrough = false;
                UpdateStyles();

                if (!this.Visible) this.Show();
                
                this.lockMiniButton.BringToFront();
                this.interfaceMiniButton.BringToFront();
                this.Refresh();
            }
            else
            {
                this.lockMiniButton.Visible = false;
                this.interfaceMiniButton.Visible = false;
                this.messageLabel.Visible = true;
                this.countdownLabel.Visible = true;
                this.operatorButton.Visible = true;
                this.freePlayButton.Visible = true;
                this.addCreditsButton.Visible = true;

                this.BackColor = Color.FromArgb(1, 1, 1);
                this.Opacity = Math.Min(0.99, _baseOpacity);
                this.Bounds = Screen.PrimaryScreen!.Bounds;
                
                RepositionLabels();
            }
            this.BringToFront();
            NativeMethods.SetWindowPos(this.Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
            
            this.Invalidate();
            RefreshMiniButtonsVisibility();
        }

        public void RefreshMiniButtonsVisibility()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(RefreshMiniButtonsVisibility));
                return;
            }

            if (_isMiniMode && _manager != null)
            {
                bool show = !_manager.HideOperatorButtons;
                this.Visible = show; 
                this.lockMiniButton.Visible = show;
                this.interfaceMiniButton.Visible = show;
                
                if (show)
                {
                    this.lockMiniButton.BringToFront();
                    this.interfaceMiniButton.BringToFront();
                    NativeMethods.SetWindowPos(this.Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
                        NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RepositionLabels();
        }

        private void RepositionLabels()
        {
            if (this.messageLabel == null || this.countdownLabel == null) return;

            int msgHeight = 320; 
            int gap = 10;
            int cntHeight = 120; 
            int totalHeight = msgHeight + (this.countdownLabel.Visible ? gap + cntHeight : 0);

            int startY = (this.Height - totalHeight) / 2;

            this.messageLabel.Size = new Size(this.Width, msgHeight);
            this.messageLabel.Location = new Point(0, startY);

            this.countdownLabel.Size = new Size(this.Width, cntHeight);
            this.countdownLabel.Location = new Point(0, startY + msgHeight + gap);

            int marginX = 20;
            int marginY = 80; 
            int btnWidth = 40;
            int btnHeight = 30;
            int btnSpacing = 5;

            if (this.addCreditsButton != null) this.addCreditsButton.Location = new Point(this.Width - marginX - btnWidth, this.Height - marginY - btnHeight);
            if (this.freePlayButton != null) this.freePlayButton.Location = new Point((this.addCreditsButton?.Left ?? 0) - btnSpacing - btnWidth, this.Height - marginY - btnHeight);
            if (this.operatorButton != null) this.operatorButton.Location = new Point((this.freePlayButton?.Left ?? 0) - btnSpacing - btnWidth, this.Height - marginY - btnHeight);
        }

        public void RefreshLayout()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(RefreshLayout));
                return;
            }

            if (_isMiniMode)
            {
                Rectangle screenBounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 800, 600);
                this.Location = new Point((screenBounds.Width - 180) / 2, 10);
                this.BringToFront();
                lockMiniButton.BringToFront();
                interfaceMiniButton.BringToFront();
            }
            else
            {
                this.Bounds = Screen.PrimaryScreen!.Bounds;
            }
            
            this.UpdateStyles();
            this.Invalidate();
        }

        public void UpdateAlertText(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateAlertText(message)));
                return;
            }
            this.messageLabel.Text = message;
        }

        public void HideOverlay(bool restoreFocus = true)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => HideOverlay(restoreFocus)));
                return;
            }

            if (_isMiniMode)
            {
                SetMiniMode(true);
                return;
            }

            this.Visible = false;
            _isClickThrough = false;

            if (restoreFocus && _previousForegroundWindow != IntPtr.Zero)
            {
                ForceForegroundWindow(_previousForegroundWindow);
            }
            
            _previousForegroundWindow = IntPtr.Zero;
        }

        public void ShowMessage(string message, bool isAlert = false, bool activate = true)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => ShowMessage(message, isAlert, activate)));
                return;
            }

            if (_isMiniMode)
            {
                SetMiniMode(false);
                _isMiniMode = true; 
            }

            if (this.IsDisposed) return;
            if (!CanStealFocus) return;
            if (!isAlert) activate = false;

            bool currentStateMatchesAlert = (isAlert && !_isClickThrough) || (!isAlert && _isClickThrough);
            if (this.Visible && currentStateMatchesAlert)
            {
                this.messageLabel.Text = message;
                this.countdownLabel.Visible = false;
                return;
            }

            if (isAlert)
            {
                _didStealFocus = true;
                _isClickThrough = false;
                _preventActivation = !activate;
            }
            else
            {
                _didStealFocus = false;
                _isClickThrough = true;
                _preventActivation = true;
            }

            int exStyle = NativeMethods.GetWindowLong(this.Handle, GWL_EXSTYLE);
            if (_isClickThrough) exStyle |= WS_EX_TRANSPARENT; else exStyle &= ~WS_EX_TRANSPARENT;
            if (_preventActivation) exStyle |= (int)WS_EX_NOACTIVATE; else exStyle &= ~(int)WS_EX_NOACTIVATE;
            NativeMethods.SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);

            this.BackColor = Color.FromArgb(1, 1, 1);
            this.Opacity = Math.Min(0.99, _baseOpacity);
            this.messageLabel.Text = message;
            this.countdownLabel.Visible = false;

            if (isAlert) this.Bounds = Screen.PrimaryScreen!.Bounds;

            if (!this.Visible) this.Visible = true;

            NativeMethods.SetWindowPos(this.Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, 
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | (activate ? 0 : NativeMethods.SWP_NOACTIVATE) | NativeMethods.SWP_SHOWWINDOW);

            if (activate)
            {
                IntPtr currentFg = NativeMethods.GetForegroundWindow();
                if (currentFg != IntPtr.Zero && currentFg != this.Handle)
                {
                    NativeMethods.GetWindowThreadProcessId(currentFg, out uint fgPid);
                    if (fgPid != (uint)System.Diagnostics.Process.GetCurrentProcess().Id)
                    {
                        _previousForegroundWindow = currentFg;
                    }
                }

                if (CanStealFocus) { this.Activate(); this.Focus(); }
                
                if (isAlert)
                {
                    if (CanStealFocus) ForceForegroundWindow(this.Handle, activate: true);
                }
            }
        }

        public void ShowOperatorMessage(string message, int durationSeconds)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ShowOperatorMessage(message, durationSeconds)));
                return;
            }

            ShowMessage(message, isAlert: false);

            if (_messageTimer != null)
            {
                _messageTimer.Stop();
                _messageTimer.Dispose();
            }

            _messageTimer = new System.Windows.Forms.Timer();
            _messageTimer.Interval = durationSeconds * 1000;
            _messageTimer.Tick += (s, e) =>
            {
                _messageTimer.Stop();
                HideOverlay();
            };
            _messageTimer.Start();
        }

        public void ShowCountdown(string message, int secondsLeft, bool isAlert = false, bool activate = true)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => ShowCountdown(message, secondsLeft, isAlert, activate)));
                return;
            }

            if (_isMiniMode)
            {
                SetMiniMode(false);
                _isMiniMode = true; 
            }

            if (!CanStealFocus) return;
            if (!isAlert) activate = false;

            bool currentStateMatchesAlert = (isAlert && !_isClickThrough) || (!isAlert && _isClickThrough);
            if (this.Visible && currentStateMatchesAlert)
            {
                this.messageLabel.Text = message;
                this.countdownLabel.Text = secondsLeft.ToString();
                if (!this.countdownLabel.Visible) this.countdownLabel.Visible = true;
                return;
            }

            this.messageLabel.Text = message;
            this.countdownLabel.Text = secondsLeft.ToString();
            this.countdownLabel.Visible = true;

            if (isAlert)
            {
                this.Opacity = Math.Min(0.99, _baseOpacity);
                _didStealFocus = true;
                _isClickThrough = false;
                _preventActivation = !activate;
            }
            else
            {
                this.Opacity = Math.Min(0.99, _baseOpacity * 0.7);
                _didStealFocus = false;
                _isClickThrough = true;
                _preventActivation = true;
            }

            int exStyle = NativeMethods.GetWindowLong(this.Handle, GWL_EXSTYLE);
            if (_isClickThrough) exStyle |= WS_EX_TRANSPARENT; else exStyle &= ~WS_EX_TRANSPARENT;
            if (_preventActivation) exStyle |= (int)WS_EX_NOACTIVATE; else exStyle &= ~(int)WS_EX_NOACTIVATE;
            NativeMethods.SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);

            if (!this.Visible) this.Visible = true;

            NativeMethods.SetWindowPos(this.Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, 
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | (activate ? 0 : NativeMethods.SWP_NOACTIVATE) | NativeMethods.SWP_SHOWWINDOW);

            if (activate && isAlert)
            {
                if (CanStealFocus) ForceForegroundWindow(this.Handle, activate: activate);
            }

            this.Refresh();
        }

        public void ShowTaskSwitcher(List<string> appNames, int selectedIndex)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => ShowTaskSwitcher(appNames, selectedIndex)));
                return;
            }

            // EN: Expand from mini mode to full-screen WITHOUT losing our mini-mode tracking flag.
            // FR: Passer du mode mini au plein écran SANS perdre notre flag de suivi du mode mini.
            bool wasMiniMode = _isMiniMode;
            if (_isMiniMode)
            {
                SetMiniMode(false);
                _isMiniMode = wasMiniMode; // EN: Restore the flag so HideTaskSwitcher can restore mini mode correctly.
            }

            // EN: Explicitly set full-screen bounds — mandatory on shell-less arcade cabinets.
            // FR: Définir explicitement les limites plein écran — obligatoire sur borne sans shell.
            this.Bounds = Screen.PrimaryScreen!.Bounds;

            // EN: Hide all standard overlay elements. Only the taskSwitcherPanel should be visible.
            // FR: Masquer tous les éléments overlay standards. Seul le taskSwitcherPanel doit être visible.
            this.messageLabel.Visible = false;
            this.countdownLabel.Visible = false;
            this.operatorButton.Visible = false;
            this.freePlayButton.Visible = false;
            this.addCreditsButton.Visible = false;
            this.lockMiniButton.Visible = false;
            this.interfaceMiniButton.Visible = false;

            // EN: Rebuild the items list in the task switcher panel.
            // FR: Reconstruire la liste des éléments dans le panel du task switcher.
            if (this.taskSwitcherItemLabels != null)
            {
                foreach (var lbl in this.taskSwitcherItemLabels)
                {
                    this.taskSwitcherPanel.Controls.Remove(lbl);
                    lbl.Dispose();
                }
            }

            this.taskSwitcherItemLabels = new Label[appNames.Count];
            int currentY = 80;

            for (int i = 0; i < appNames.Count; i++)
            {
                Label lbl = new Label();
                lbl.Text = appNames[i];
                lbl.Font = new Font("Segoe UI", 20F, i == selectedIndex ? FontStyle.Bold : FontStyle.Regular);
                lbl.ForeColor = i == selectedIndex ? Color.White : Color.Gray;
                lbl.BackColor = Color.Transparent;
                lbl.AutoSize = true;
                lbl.Padding = new Padding(10);
                lbl.Location = new Point(20, currentY);
                this.taskSwitcherPanel.Controls.Add(lbl);
                this.taskSwitcherItemLabels[i] = lbl;
                currentY += 60;
            }

            int maxWidth = 400;
            foreach (var lbl in this.taskSwitcherItemLabels)
            {
                if (lbl.PreferredWidth + 40 > maxWidth) maxWidth = lbl.PreferredWidth + 40;
            }
            
            foreach (var lbl in this.taskSwitcherItemLabels)
            {
                lbl.AutoSize = false;
                lbl.Width = maxWidth - 40;
                lbl.Height = 50;
            }

            this.taskSwitcherPanel.Size = new Size(maxWidth, currentY + 20);

            Rectangle screenBounds = this.Bounds;
            this.taskSwitcherPanel.Location = new Point(
                (screenBounds.Width - this.taskSwitcherPanel.Width) / 2,
                (screenBounds.Height - this.taskSwitcherPanel.Height) / 2
            );

            this.taskSwitcherPanel.Visible = true;
            this.taskSwitcherPanel.BringToFront();

            // EN: CRITICAL FIX for shell-less arcade cabinets (no explorer.exe):
            //     WS_EX_TRANSPARENT prevents painting in a shell-less environment because Windows
            //     defers the paint until "siblings" are painted — which never happens without a Desktop Shell.
            //     This is exactly why Lock/BR buttons also had to use this same no-transparent pattern.
            //     We use WS_EX_NOACTIVATE ONLY to avoid stealing focus from the game.
            // FR: CORRECTIF CRITIQUE pour les bornes sans shell (sans explorer.exe) :
            //     WS_EX_TRANSPARENT empêche le rendu quand il n'y a pas de shell Windows car Windows
            //     diffère le paint jusqu'à ce que les "siblings" soient peints — ce qui n'arrive jamais sans Desktop Shell.
            //     C'est exactement pourquoi les boutons Lock/BR ont aussi dû utiliser ce même pattern sans transparence.
            //     On utilise UNIQUEMENT WS_EX_NOACTIVATE pour ne pas voler le focus au jeu.
            _isClickThrough = false;
            _preventActivation = true;
            _didStealFocus = false;

            int exStyle = NativeMethods.GetWindowLong(this.Handle, GWL_EXSTYLE);
            exStyle &= ~WS_EX_TRANSPARENT;           // EN: REMOVE transparent — it kills rendering without a shell.
            exStyle |= (int)WS_EX_NOACTIVATE;        // EN: Keep NOACTIVATE so we don't steal focus.
            NativeMethods.SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);

            this.BackColor = Color.FromArgb(1, 1, 1);
            this.Opacity = Math.Min(0.99, _baseOpacity);

            if (!this.Visible) this.Visible = true;

            NativeMethods.SetWindowPos(this.Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, 
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);

            // EN: Force a full repaint of the form and all children — critical on shell-less systems.
            // FR: Forcer un repaint complet du formulaire et de ses enfants — critique sur borne sans shell.
            this.Invalidate(true);
            this.Refresh();
        }

        public void HideTaskSwitcher()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(HideTaskSwitcher));
                return;
            }

            this.taskSwitcherPanel.Visible = false;

            // EN: Restore click-through style that was removed for the task switcher display.
            // FR: Restaurer le style click-through qui avait été retiré pour l'affichage du task switcher.
            int exStyle = NativeMethods.GetWindowLong(this.Handle, GWL_EXSTYLE);
            exStyle |= WS_EX_TRANSPARENT;
            NativeMethods.SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);
            _isClickThrough = true;

            HideOverlay(false);
        }

        private void FocusEmulationStationProcess()
        {
            try
            {
                var esProcesses = System.Diagnostics.Process.GetProcessesByName("emulationstation");
                if (esProcesses.Length > 0)
                {
                    ForceForegroundWindow(esProcesses[0].MainWindowHandle);
                }
            }
            catch { }
        }

        protected override bool ShowWithoutActivation => _preventActivation;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter || keyData == Keys.Space) return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_MOUSEACTIVATE = 0x0021;
            const int MA_NOACTIVATE = 3;
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_CLOSE = 0xF060;

            if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt32() & 0xFFF0) == SC_CLOSE)
            {
                this.Hide();
                return;
            }
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_manager != null && _manager.IsLocked && !_manager.IsInternalClosing)
            {
                if (e.CloseReason == CloseReason.UserClosing || e.CloseReason == CloseReason.TaskManagerClosing)
                {
                    e.Cancel = true;
                    _manager.TriggerLocalOperatorPrompt();
                    return;
                }
            }
            else if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
            }
            base.OnFormClosing(e);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= (0x80 | WS_EX_LAYERED);
                if (_preventActivation) cp.ExStyle |= WS_EX_NOACTIVATE;
                if (_isClickThrough) cp.ExStyle |= WS_EX_TRANSPARENT;
                return cp;
            }
        }
    }
}


