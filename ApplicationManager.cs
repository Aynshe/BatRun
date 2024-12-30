using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace BatRun
{
    public class ApplicationManager
    {
        private readonly IniFile config;
        private readonly Logger logger;
        private Dictionary<IntPtr, string> hiddenWindows = new Dictionary<IntPtr, string>();
        private HashSet<string> persistentHiddenTitles = new HashSet<string>();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        public ApplicationManager(IniFile config, Logger logger)
        {
            this.config = config;
            this.logger = logger;
            LoadHiddenWindows();
            LoadPersistentHiddenTitles();
        }

        private void LoadHiddenWindows()
        {
            try
            {
                int count = config.ReadInt("HiddenWindows", "Count", 0);
                for (int i = 0; i < count; i++)
                {
                    string windowTitle = config.ReadValue("HiddenWindows", $"Window{i}", "");
                    if (!string.IsNullOrEmpty(windowTitle))
                    {
                        // Trouver la fenêtre correspondante
                        EnumWindows((hWnd, lParam) =>
                        {
                            StringBuilder title = new StringBuilder(256);
                            GetWindowText(hWnd, title, 256);
                            if (title.ToString() == windowTitle)
                            {
                                hiddenWindows[hWnd] = windowTitle;
                            }
                            return true;
                        }, IntPtr.Zero);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error loading hidden windows: {ex.Message}", ex);
            }
        }

        public void SaveHiddenWindows()
        {
            try
            {
                // Nettoyer les anciennes entrées
                int oldCount = config.ReadInt("HiddenWindows", "Count", 0);
                for (int i = 0; i < oldCount; i++)
                {
                    config.WriteValue("HiddenWindows", $"Window{i}", "");
                }

                // Sauvegarder les nouvelles entrées
                int index = 0;
                foreach (var window in hiddenWindows)
                {
                    config.WriteValue("HiddenWindows", $"Window{index}", window.Value);
                    index++;
                }
                config.WriteValue("HiddenWindows", "Count", index.ToString());
            }
            catch (Exception ex)
            {
                logger.LogError($"Error saving hidden windows: {ex.Message}", ex);
            }
        }

        public void HideWindow(IntPtr hWnd)
        {
            try
            {
                if (hWnd != IntPtr.Zero && IsWindowVisible(hWnd))
                {
                    StringBuilder title = new StringBuilder(256);
                    GetWindowText(hWnd, title, 256);
                    string windowTitle = title.ToString();

                    if (!string.IsNullOrEmpty(windowTitle))
                    {
                        ShowWindow(hWnd, SW_HIDE);
                        hiddenWindows[hWnd] = windowTitle;
                        SaveHiddenWindows();
                        logger.LogInfo($"Window hidden: {windowTitle}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error hiding window: {ex.Message}", ex);
            }
        }

        public void ShowWindow(IntPtr hWnd)
        {
            try
            {
                if (hWnd != IntPtr.Zero && hiddenWindows.ContainsKey(hWnd))
                {
                    ShowWindow(hWnd, SW_SHOW);
                    string windowTitle = hiddenWindows[hWnd];
                    hiddenWindows.Remove(hWnd);
                    SaveHiddenWindows();
                    logger.LogInfo($"Window shown: {windowTitle}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error showing window: {ex.Message}", ex);
            }
        }

        public void ReloadHiddenWindows()
        {
            try
            {
                // Sauvegarder les fenêtres actuellement masquées
                var currentlyHidden = new Dictionary<IntPtr, string>(hiddenWindows);
                
                // Recharger depuis la configuration
                LoadHiddenWindows();

                // Fusionner avec les fenêtres actuellement masquées
                foreach (var window in currentlyHidden)
                {
                    if (!hiddenWindows.ContainsKey(window.Key))
                    {
                        hiddenWindows[window.Key] = window.Value;
                    }
                }

                // Sauvegarder la liste fusionnée
                SaveHiddenWindows();
                
                logger.LogInfo("Hidden windows list reloaded");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error reloading hidden windows: {ex.Message}", ex);
            }
        }

        public Dictionary<IntPtr, string> GetHiddenWindows()
        {
            return new Dictionary<IntPtr, string>(hiddenWindows);
        }

        public List<(IntPtr Handle, string Title)> GetVisibleWindows()
        {
            var windows = new List<(IntPtr Handle, string Title)>();
            
            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    StringBuilder title = new StringBuilder(256);
                    GetWindowText(hWnd, title, 256);
                    string windowTitle = title.ToString();
                    
                    if (!string.IsNullOrEmpty(windowTitle) && !hiddenWindows.ContainsKey(hWnd))
                    {
                        windows.Add((hWnd, windowTitle));
                    }
                }
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        public void ToggleWindowVisibility(IntPtr hWnd)
        {
            if (hiddenWindows.ContainsKey(hWnd))
            {
                ShowWindow(hWnd);
            }
            else
            {
                HideWindow(hWnd);
            }
        }

        private void LoadPersistentHiddenTitles()
        {
            try
            {
                persistentHiddenTitles.Clear();
                int count = config.ReadInt("PersistentHiddenWindows", "Count", 0);
                for (int i = 0; i < count; i++)
                {
                    string title = config.ReadValue("PersistentHiddenWindows", $"Window{i}", "");
                    if (!string.IsNullOrEmpty(title))
                    {
                        persistentHiddenTitles.Add(title);
                    }
                }
                logger.LogInfo($"Loaded {persistentHiddenTitles.Count} persistent hidden window titles");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error loading persistent hidden titles: {ex.Message}", ex);
            }
        }

        private void SavePersistentHiddenTitles()
        {
            try
            {
                int index = 0;
                foreach (var title in persistentHiddenTitles)
                {
                    config.WriteValue("PersistentHiddenWindows", $"Window{index}", title);
                    index++;
                }
                config.WriteValue("PersistentHiddenWindows", "Count", index.ToString());
                logger.LogInfo($"Saved {index} persistent hidden window titles");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error saving persistent hidden titles: {ex.Message}", ex);
            }
        }

        public void AddToPersistentHidden(string windowTitle)
        {
            if (!string.IsNullOrEmpty(windowTitle))
            {
                persistentHiddenTitles.Add(windowTitle);
                SavePersistentHiddenTitles();
                logger.LogInfo($"Added {windowTitle} to persistent hidden windows");
                
                // Masquer immédiatement toutes les fenêtres correspondantes
                EnumWindows((hWnd, lParam) =>
                {
                    StringBuilder title = new StringBuilder(256);
                    GetWindowText(hWnd, title, 256);
                    if (title.ToString() == windowTitle && IsWindowVisible(hWnd))
                    {
                        HideWindow(hWnd);
                    }
                    return true;
                }, IntPtr.Zero);
            }
        }

        public void RemoveFromPersistentHidden(string windowTitle)
        {
            if (persistentHiddenTitles.Remove(windowTitle))
            {
                SavePersistentHiddenTitles();
                logger.LogInfo($"Removed {windowTitle} from persistent hidden windows");
            }
        }

        public bool IsPersistentlyHidden(string windowTitle)
        {
            return persistentHiddenTitles.Contains(windowTitle);
        }

        public void CheckForPersistentWindows()
        {
            try
            {
                // Charger la liste des exécutables à masquer
                int count = config.ReadInt("PersistentHiddenWindows", "Count", 0);
                var executablesToHide = new HashSet<string>();
                for (int i = 0; i < count; i++)
                {
                    string execName = config.ReadValue("PersistentHiddenWindows", $"Window{i}", "");
                    if (!string.IsNullOrEmpty(execName))
                    {
                        executablesToHide.Add(execName.ToLower());
                    }
                }

                if (executablesToHide.Count == 0) return;

                // Parcourir tous les processus en cours d'exécution
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        string processName = process.ProcessName.ToLower() + ".exe";
                        if (executablesToHide.Contains(processName))
                        {
                            process.Refresh();
                            if (process.MainWindowHandle != IntPtr.Zero && IsWindowVisible(process.MainWindowHandle))
                            {
                                HideWindow(process.MainWindowHandle);
                                logger.LogInfo($"Auto-hidden window for process: {processName}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error processing window for process: {ex.Message}", ex);
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error checking for persistent windows: {ex.Message}", ex);
            }
        }

        public void ShowAllWindows()
        {
            try
            {
                var windowsToShow = new Dictionary<IntPtr, string>(hiddenWindows);
                foreach (var window in windowsToShow)
                {
                    ShowWindow(window.Key);
                }
                hiddenWindows.Clear();
                SaveHiddenWindows();
                logger.LogInfo("All windows have been shown");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error showing all windows: {ex.Message}", ex);
            }
        }

        public void CleanupOnExit()
        {
            try
            {
                // Afficher toutes les fenêtres
                ShowAllWindows();

                // Nettoyer la configuration des fenêtres masquées
                int count = config.ReadInt("HiddenWindows", "Count", 0);
                for (int i = 0; i < count; i++)
                {
                    config.WriteValue("HiddenWindows", $"Window{i}", "");
                }
                config.WriteValue("HiddenWindows", "Count", "0");

                logger.LogInfo("Hidden windows configuration cleaned up on exit");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error cleaning up hidden windows on exit: {ex.Message}", ex);
            }
        }
    }
} 