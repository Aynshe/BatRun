using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Text;

namespace BatRunGuardian
{
    static class Program
    {
        private static string logFile = "";
        private static int pidToWatch = 0;
        private static Process? targetProcess;
        private static string operatorPassword = "";
        private static string baseDir = "";

        // Hook setup
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static System.Windows.Forms.Timer? _emergencyTimer;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static bool _isPromptOpen = false;
        private static Form? _dummyForm;

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (args.Length >= 2 && Directory.Exists(args[1]))
            {
                baseDir = args[1];
            }

            logFile = Path.Combine(baseDir, "Logs", "BatRunGuardian.log");
            
            try 
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);
                File.AppendAllText(logFile, $"[{DateTime.Now}] --- Guardian starting (PID Watch: {args[0]}) ---\n");
                if (args.Length >= 2) File.AppendAllText(logFile, $"[{DateTime.Now}] Use main app directory: {baseDir}\n");
            } catch { }

            if (args.Length < 1 || !int.TryParse(args[0], out pidToWatch))
            {
                Log($"Error: No valid PID provided in arguments.");
                return;
            }

            try
            {
                targetProcess = Process.GetProcessById(pidToWatch);
            }
            catch (Exception ex)
            {
                Log($"Process not found at startup: {ex.Message}");
                return;
            }

            LoadSettings();

            // Create a hidden dummy form to handle UI thread invokes
            _dummyForm = new Form() { Visible = false, ShowInTaskbar = false, WindowState = FormWindowState.Minimized };
            _ = _dummyForm.Handle; // Force handle creation

            _emergencyTimer = new System.Windows.Forms.Timer();
            _emergencyTimer.Interval = 3000;
            _emergencyTimer.Tick += (s, e) => {
                _emergencyTimer.Stop();
                if (!_isPromptOpen)
                {
                    _isPromptOpen = true;
                    LoadSettings(); // EN: Reload password before showing modal / FR: Recharger le mot de passe avant d'afficher la modale
                    ShowEmergencyModal();
                }
            };

            _hookID = SetHook(_proc);

            // Start monitoring task
            Task.Run(() => MonitorBatRun());

            // IPC Wake Watcher for UIPI bypass
            var wakeTimer = new System.Windows.Forms.Timer { Interval = 500 };
            wakeTimer.Tick += (s, e) =>
            {
                string wakePath = Path.Combine(Path.GetTempPath(), "wake_guardian.lock");
                if (File.Exists(wakePath))
                {
                    try { File.Delete(wakePath); } catch { }
                    if (!_isPromptOpen)
                    {
                        Log("Wake signal received from BatRun. Opening modal...");
                        _isPromptOpen = true;
                        LoadSettings();
                        ShowEmergencyModal();
                    }
                }
            };
            wakeTimer.Start();

            Application.Run(); // Start message pump

            UnhookWindowsHookEx(_hookID);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetPrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpDefault,
            StringBuilder lpReturnedString,
            uint nSize,
            string lpFileName);

        private static void LoadSettings()
        {
            try
            {
                string iniPath = Path.Combine(baseDir, "config.ini");
                if (File.Exists(iniPath))
                {
                    StringBuilder result = new StringBuilder(255);
                    GetPrivateProfileString("Arcade", "OperatorPassword", "", result, (uint)result.Capacity, iniPath);
                    operatorPassword = result.ToString();
                    Log($"[Settings] Loaded OperatorPassword from INI (Section [Arcade]). Length: {operatorPassword.Length}");
                }
                else
                {
                    Log($"[Settings] BatRun.ini not found at {iniPath}");
                }
            }
            catch (Exception ex)
            {
                Log($"[Settings] FAIL to load: {ex.Message}");
            }
        }

        private static void Log(string message)
        {
            try { File.AppendAllText(logFile, $"[{DateTime.Now}] {message}\n"); } catch { }
        }

        private static void MonitorBatRun()
        {
            try
            {
                Log($"Monitoring PID: {pidToWatch} (BatRun)");
                int freezeTicks = 0;
                string heartbeatPath = Path.Combine(baseDir, "Logs", "batrun.heartbeat");
                // EN: Only activate heartbeat detection once the file has been seen at least once
                // FR: N'activer la détection heartbeat qu'une fois le fichier vu au moins une fois
                bool heartbeatSeen = false;
                while (targetProcess != null && !targetProcess.HasExited)
                {
                    targetProcess.Refresh();
                    
                    bool isResponding = targetProcess.Responding;
                    bool heartbeatStale = false;

                    if (File.Exists(heartbeatPath))
                    {
                        heartbeatSeen = true; // EN: File seen at least once / FR: Fichier vu au moins une fois
                        var lastHeartbeat = File.GetLastWriteTime(heartbeatPath);
                        if ((DateTime.Now - lastHeartbeat).TotalSeconds > 15)
                        {
                            heartbeatStale = true;
                        }
                    }
                    else if (heartbeatSeen)
                    {
                        // EN: File was seen before but is now gone = stale
                        // FR: Fichier vu avant mais disparu = considéré périmé
                        heartbeatStale = true;
                    }

                    if (!targetProcess.HasExited && (!isResponding || heartbeatStale))
                    {
                        freezeTicks++;
                        if (freezeTicks >= 10)
                        {
                            Log($"FREEZE DETECTED! Responding={isResponding}, HeartbeatStale={heartbeatStale}. PID {pidToWatch} is stuck. Forcing kill...");
                            try 
                            { 
                                // EN: Use taskkill directly to avoid killing our own process tree
                                // FR: Utiliser taskkill pour ne pas se suicider (Guardian est enfant de BatRun)
                                var kill = Process.Start(new ProcessStartInfo("taskkill", $"/F /PID {pidToWatch}")
                                {
                                    UseShellExecute = false, CreateNoWindow = true
                                });
                                kill?.WaitForExit(5000);
                                if (!targetProcess.WaitForExit(5000))
                                    Log("WARNING: Process may still be running after taskkill.");
                            } 
                            catch(Exception kex) { Log($"Kill failed: {kex.Message}"); }
                            break;
                        }
                    }
                    else
                    {
                        freezeTicks = 0;
                    }
                    Thread.Sleep(1000);
                }
                
                Log($"BatRun (PID {pidToWatch}) has stopped or was killed.");
            }
            catch (Exception ex)
            {
                Log($"Stop monitoring: {ex.Message}");
            }

            // Check for normal exit lock file
            string lockFile = Path.Combine(baseDir, "BatRun.exit.lock");
            
            if (File.Exists(lockFile))
            {
                Log($"Normal exit detected via lock file. Guardian exiting.");
                try { File.Delete(lockFile); } catch { }
                Application.Exit();
                return;
            }

            // Crash
            Log($"CRASH OR FREEZE DETECTED! Cleaning up all BatRun instances...");

            // EN: Read session token BEFORE killing BatRun to allow state restore on restart
            // FR: Lire le token de session AVANT de tuer BatRun pour permettre la restauration d'état
            string? sessionToken = null;
            try {
                string statePath = Path.Combine(baseDir, "Logs", "session_state.json");
                if (File.Exists(statePath)) {
                    string json = File.ReadAllText(statePath);
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    sessionToken = doc.RootElement.GetProperty("restartToken").GetString();
                    Log($"Session token read: {sessionToken}");
                }
            } catch { }

            // EN: Kill all stray BatRun instances via targeted taskkill
            // FR: Tuer toutes les instances BatRun égarées via taskkill ciblé
            try {
                foreach (var p in Process.GetProcessesByName("BatRun")) {
                    try {
                        var kp = Process.Start(new ProcessStartInfo("taskkill", $"/F /PID {p.Id}") { UseShellExecute = false, CreateNoWindow = true });
                        kp?.WaitForExit(3000);
                        p.WaitForExit(3000);
                    } catch {}
                }
            } catch {}

            Thread.Sleep(2000);
            RestartBatRun(sessionToken);

            // EN: After restart, find the new BatRun process and continue watching it
            // FR: Après le redémarrage, trouver le nouveau processus BatRun et continuer à le surveiller
            Thread.Sleep(3000); // EN: Wait for BatRun to initialize / FR: Attendre que BatRun s'initialise
            try
            {
                var newProcesses = Process.GetProcessesByName("BatRun");
                if (newProcesses.Length > 0)
                {
                    targetProcess = newProcesses[0];
                    pidToWatch = targetProcess.Id;
                    Log($"Now watching restarted BatRun (PID: {pidToWatch}).");
                    // EN: Loop back to monitoring without exiting
                    // FR: Reboucler sur la surveillance sans quitter
                    MonitorBatRun();
                }
                else
                {
                    Log("FAILED: Could not find restarted BatRun process. Guardian exiting.");
                    if (_dummyForm != null && _dummyForm.IsHandleCreated) {
                        _dummyForm.BeginInvoke(new Action(() => Application.Exit()));
                    } else {
                        Application.Exit();
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to find new BatRun process: {ex.Message}. Guardian exiting.");
                if (_dummyForm != null && _dummyForm.IsHandleCreated) {
                    _dummyForm.BeginInvoke(new Action(() => Application.Exit()));
                } else {
                    Application.Exit();
                }
            }
        }

        private static void RestartBatRun(string? sessionToken = null)
        {
            try
            {
                string batRunPath = Path.Combine(baseDir, "BatRun.exe");
                if (File.Exists(batRunPath))
                {
                    string args = "-restarted";
                    if (!string.IsNullOrEmpty(sessionToken)) args += $" -sessiontoken {sessionToken}";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = batRunPath,
                        Arguments = args,
                        UseShellExecute = true,
                        WorkingDirectory = baseDir
                    });
                    Log($"BatRun resumed successfully (token: {sessionToken ?? "none"}).");
                }
                else
                {
                    Log($"FAILED: Could not find BatRun.exe");
                }
            }
            catch (Exception ex)
            {
                Log($"FAILED to restart BatRun: {ex.Message}");
            }
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule!)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName!), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == (int)Keys.D0 || vkCode == (int)Keys.NumPad0) // 0 key
                {
                    if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                    {
                        if (_emergencyTimer != null && !_emergencyTimer.Enabled && !_isPromptOpen)
                        {
                            _emergencyTimer.Start();
                        }
                    }
                    else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                    {
                        _emergencyTimer?.Stop();
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        // EN: P/Invoke for aggressive window positioning above all overlays
        // FR: P/Invoke pour positionner la fenêtre au-dessus de tous les overlays
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool AllowSetForegroundWindow(int dwProcessId);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;

        // EN: Force the window to the absolute top, above all other TopMost windows (including BatRun overlay)
        // FR: Forcer la fenêtre tout en haut, au-dessus de tous les overlays TopMost (y compris l'overlay BatRun)
        private static void ForceWindowToTop(IntPtr hwnd)
        {
            ShowWindow(hwnd, SW_RESTORE);
            AllowSetForegroundWindow(-1); // ASFW_ANY
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            BringWindowToTop(hwnd);
            SetForegroundWindow(hwnd);
        }

        private static void ShowEmergencyModal()
        {
            // EN: Create lock file to signal BatRun overlay to yield focus
            // FR: Créer le fichier lock pour signaler à l'overlay BatRun de céder le focus
            string guardianLock = Path.Combine(Path.GetTempPath(), "guardian_modal.lock");
            try { File.WriteAllText(guardianLock, "Guardian Modal Active"); } catch { }

            using (Form form = new Form())
            {
                form.Text = "BatRun Guardian Emergency";
                form.Size = new Size(660, 160);
                form.StartPosition = FormStartPosition.CenterScreen;
                form.TopMost = true;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.BackColor = Color.Black;
                form.ForeColor = Color.White;

                Label lbl = new Label() { Text = "Operator Password:", Left = 20, Top = 25, Width = 150, Height = 40, ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold) };
                TextBox txt = new TextBox() { Left = 180, Top = 22, Width = 280, Font=new Font("Arial", 12), UseSystemPasswordChar = true };
                Button btn = new Button() { Text = "HARD RESTART", Left = 480, Top = 20, Width = 150, Height=35, BackColor = Color.DarkRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10, FontStyle.Bold) };
                Button cancel = new Button() { Text = "CANCEL", Left = 480, Top = 60, Width = 150, Height=35, BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10, FontStyle.Bold) };
                
                Button kbToggle = new Button() { Text = "VIRTUAL KEYBOARD", Left = 180, Top = 60, Width = 280, Height = 35, BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 9, FontStyle.Bold) };

                Panel kbPanel = new Panel() { Left = 0, Top = 110, Width = 660, Height = 350, Visible = false };

                kbToggle.Click += (s, e) =>
                {
                    if (kbPanel.Visible)
                    {
                        kbPanel.Visible = false;
                        form.Height = 160;
                    }
                    else
                    {
                        kbPanel.Visible = true;
                        form.Height = 480;
                    }
                };

                // EN: State for the Shift/Caps mode
                // FR: État pour le mode Shift/Caps
                bool isShifted = false;

                Action? refreshKeys = null;
                refreshKeys = () =>
                {
                    kbPanel.Controls.Clear();
                    string[] kbRows = {
                        "1 2 3 4 5 6 7 8 9 0 - _",
                        isShifted ? "Q W E R T Y U I O P" : "q w e r t y u i o p",
                        isShifted ? "A S D F G H J K L M" : "a s d f g h j k l m",
                        isShifted ? "Z X C V B N ! ? @ #" : "z x c v b n ! ? @ #"
                    };

                    int startYkb = 10;
                    foreach (string row in kbRows)
                    {
                        int startX = 30;
                        string[] keys = row.Split(' ');
                        foreach (string key in keys)
                        {
                            Button kBtn = new Button() { Text = key, Left = startX, Top = startYkb, Width = 42, Height = 42, BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 11, FontStyle.Bold) };
                            kBtn.Click += (s, e) => { txt.Text += key; txt.SelectionStart = txt.Text.Length; txt.Focus(); };
                            kbPanel.Controls.Add(kBtn);
                            startX += 47;
                        }
                        startYkb += 47;
                    }

                    // Backspace and Clear
                    Button bkspBtn = new Button() { Text = "BACKSPACE", Left = 30, Top = startYkb, Width = 160, Height = 45, BackColor = Color.DarkRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10, FontStyle.Bold) };
                    bkspBtn.Click += (s, e) => { if (txt.Text.Length > 0) txt.Text = txt.Text.Substring(0, txt.Text.Length - 1); txt.Focus(); };
                    kbPanel.Controls.Add(bkspBtn);

                    Button clearBtn = new Button() { Text = "CLEAR", Left = 200, Top = startYkb, Width = 100, Height = 45, BackColor = Color.Orange, ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10, FontStyle.Bold) };
                    clearBtn.Click += (s, e) => { txt.Text = ""; txt.Focus(); };
                    kbPanel.Controls.Add(clearBtn);

                    // SHIFT
                    Button shiftBtn = new Button() { 
                        Text = "SHIFT", Left = 310, Top = startYkb, Width = 100, Height = 45, 
                        BackColor = isShifted ? Color.White : Color.FromArgb(60, 60, 60), 
                        ForeColor = isShifted ? Color.Black : Color.White, 
                        FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10, FontStyle.Bold) 
                    };
                    shiftBtn.Click += (s, e) => { isShifted = !isShifted; refreshKeys?.Invoke(); };
                    kbPanel.Controls.Add(shiftBtn);
                };

                refreshKeys();

                form.Controls.Add(lbl);
                form.Controls.Add(txt);
                form.Controls.Add(btn);
                form.Controls.Add(cancel);
                form.Controls.Add(kbToggle);
                form.Controls.Add(kbPanel);

                form.AcceptButton = btn;
                form.CancelButton = cancel;

                // EN: Force form to top once handle is created, and keep re-asserting during lifetime
                // FR: Forcer la fenêtre en haut dès que le handle est créé et maintenir pendant la durée de vie
                form.HandleCreated += (s, e) => ForceWindowToTop(form.Handle);
                form.Shown += (s, e) =>
                {
                    ForceWindowToTop(form.Handle);
                    txt.Focus();
                };

                form.FormClosed += (s, e) => 
                {
                    // EN: Remove lock file when modal closes
                    // FR: Supprimer le fichier lock à la fermeture du modal
                    try { if (File.Exists(guardianLock)) File.Delete(guardianLock); } catch { }
                };

                btn.Click += (s, e) =>
                {
                    // EN: Secure password logic - require the password from config.ini
                    // FR: Logique de mot de passe sécurisée - requiert le mot de passe du config.ini
                    string typed = txt.Text.Trim();
                    string expected = operatorPassword.Trim();
                    
                    // EN: If no password is set, any empty submission passes. Otherwise must match.
                    // FR: Si aucun mot de passe n'est défini, une soumission vide passe. Sinon doit correspondre.
                    bool isCorrect = typed == expected;

                    Log($"[Security] Modal validation: OperatorPasswordLength={expected.Length}, TypedLength={typed.Length}, Correct={isCorrect}");

                    if (isCorrect)
                    {
                        form.DialogResult = DialogResult.OK;
                        form.Close();
                    }
                    else
                    {
                        MessageBox.Show(form, "Access Denied", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        txt.Text = "";
                        txt.Focus();
                    }
                };
                cancel.Click += (s, e) =>
                {
                    form.DialogResult = DialogResult.Cancel;
                    form.Close();
                };

                if (form.ShowDialog() == DialogResult.OK)
                {
                    Log("Hard restart requested manually by operator.");
                    try
                    {
                        targetProcess?.Kill();
                        Log("Kill command sent successfully.");
                    }
                    catch (Exception ex)
                    {
                        Log($"Kill command failed: {ex.Message}");
                    }
                    // Monitor loop will detect exit and restart automatically.
                }
            }
            _isPromptOpen = false;
        }
    }
}
