using System;
using System.IO;
using LibVLCSharp.Shared;

namespace BatRun
{
    public static class VlcManager
    {
        private static LibVLC? _libVLC;
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        public static LibVLC Instance
        {
            get
            {
                if (!_isInitialized)
                {
                    throw new InvalidOperationException("VlcManager is not initialized. Call Initialize() first.");
                }
                return _libVLC!;
            }
        }

        public static void Initialize(Logger logger)
        {
            lock (_lock)
            {
                if (_isInitialized)
                {
                    return;
                }

                try
                {
                    string libVLCPath = Path.Combine(AppContext.BaseDirectory, "libvlc");
                    if (!Directory.Exists(libVLCPath))
                    {
                        logger.LogError($"LibVLC directory not found at: {libVLCPath}");
                        return;
                    }

                    string[] requiredDlls = { "libvlc.dll", "libvlccore.dll" };
                    foreach (var dll in requiredDlls)
                    {
                        string dllPath = Path.Combine(libVLCPath, dll);
                        if (!File.Exists(dllPath))
                        {
                            logger.LogError($"Required LibVLC DLL not found: {dllPath}");
                            return;
                        }
                    }

                    Core.Initialize(libVLCPath);
                    _libVLC = new LibVLC(
                        "--quiet",
                        "--no-video-title-show",
                        "--no-snapshot-preview",
                        "--no-stats",
                        "--no-sub-autodetect-file",
                        "--no-osd",
                        "--no-video-deco"
                    );

                    _isInitialized = true;
                    logger.LogInfo($"LibVLC initialized successfully from: {libVLCPath}");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to initialize LibVLC: {ex.Message}", ex);
                    throw;
                }
            }
        }

        public static void Dispose()
        {
            lock (_lock)
            {
                if (_isInitialized)
                {
                    _libVLC?.Dispose();
                    _libVLC = null;
                    _isInitialized = false;
                }
            }
        }
    }
}
