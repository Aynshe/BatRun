using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.Input
{
    public class RawInputDeviceItem
    {
        public IntPtr Handle { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public class RawInputHandler : NativeWindow, IDisposable
    {
        private const int RID_INPUT = 0x10000003;
        private const int RIDI_DEVICENAME = 0x20000007;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private readonly Logger _logger;
        public event EventHandler<RawInputEventArgs>? KeyPressed;

        public RawInputHandler(Logger logger)
        {
            _logger = logger;
            CreateHandle(new CreateParams
            {
                X = 0,
                Y = 0,
                Width = 0,
                Height = 0,
                Style = 0x800000, // WS_DISABLED
                Parent = new IntPtr(-3) // HWND_MESSAGE
            });

            Register();
        }

        private void Register()
        {
            var rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x01; // Generic desktop controls
            rid[0].usUsage = 0x06;     // Keyboard
            rid[0].dwFlags = NativeMethods.RIDEV_INPUTSINK;
            rid[0].hwndTarget = this.Handle;

            if (!NativeMethods.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
            {
                _logger.LogError($"Failed to register raw input devices. Error code: {Marshal.GetLastWin32Error()}");
            }
            else
            {
                _logger.LogInfo("Raw input devices registered successfully.");
            }
        }

        public static List<RawInputDeviceItem> GetKeyboardDevices()
        {
            var devices = new List<RawInputDeviceItem>();
            uint deviceCount = 0;
            int dwSize = Marshal.SizeOf(typeof(RAWINPUTDEVICELIST));

            if (GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)dwSize) == 0 && deviceCount > 0)
            {
                var pRawInputDeviceList = Marshal.AllocHGlobal((int)(dwSize * deviceCount));
                GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, (uint)dwSize);

                for (int i = 0; i < deviceCount; i++)
                {
                    var rid = (RAWINPUTDEVICELIST)Marshal.PtrToStructure(
                        new IntPtr(pRawInputDeviceList.ToInt64() + (dwSize * i)),
                        typeof(RAWINPUTDEVICELIST))!;

                    if (rid.dwType == NativeMethods.RIM_TYPEKEYBOARD)
                    {
                        string deviceName = GetDeviceName(rid.hDevice);
                        if (!string.IsNullOrEmpty(deviceName))
                        {
                            devices.Add(new RawInputDeviceItem
                            {
                                Handle = rid.hDevice,
                                Name = deviceName,
                                DisplayName = ParseDeviceName(deviceName)
                            });
                        }
                    }
                }
                Marshal.FreeHGlobal(pRawInputDeviceList);
            }

            return devices;
        }

        public static string GetDeviceName(IntPtr hDevice)
        {
            uint pcbSize = 0;
            NativeMethods.GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);

            if (pcbSize > 0)
            {
                // EN: Windows API GetRawInputDeviceInfoW returns size in CHARACTERS
                // FR: L'API Windows GetRawInputDeviceInfoW retourne la taille en CARACTÈRES
                int bytesRequired = (int)pcbSize * 2;
                IntPtr pData = Marshal.AllocHGlobal(bytesRequired);
                try
                {
                    NativeMethods.GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, pData, ref pcbSize);
                    string? name = Marshal.PtrToStringUni(pData);
                    // EN: Remove null terminator if present, as it breaks string comparison with config.ini
                    // FR: Supprimer le caractère nul s'il est présent, car il casse la comparaison avec le config.ini
                    return (name?.TrimEnd('\0') ?? string.Empty).Trim();
                }
                finally
                {
                    Marshal.FreeHGlobal(pData);
                }
            }
            return string.Empty;
        }

        private static string ParseDeviceName(string rawName)
        {
            // rawName format typically: \\?\HID#VID_XXXX&PID_XXXX...
            try
            {
                if (rawName.Contains("VID_") && rawName.Contains("PID_"))
                {
                    int vidIdx = rawName.IndexOf("VID_");
                    int pidIdx = rawName.IndexOf("PID_");
                    string vid = rawName.Substring(vidIdx + 4, 4);
                    string pid = rawName.Substring(pidIdx + 4, 4);
                    return $"Keyboard (VID: {vid}, PID: {pid})";
                }
            }
            catch { }
            return rawName;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_INPUT)
            {
                ProcessRawInput(m.LParam);
            }
            base.WndProc(ref m);
        }

        private void ProcessRawInput(IntPtr hRawInput)
        {
            uint dwSize = 0;
            NativeMethods.GetRawInputData(hRawInput, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));

            if (dwSize != 0)
            {
                IntPtr pData = Marshal.AllocHGlobal((int)dwSize);
                if (NativeMethods.GetRawInputData(hRawInput, RID_INPUT, pData, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == dwSize)
                {
                    var raw = (RAWINPUT)Marshal.PtrToStructure(pData, typeof(RAWINPUT))!;

                    if (raw.header.dwType == NativeMethods.RIM_TYPEKEYBOARD)
                    {
                        bool isDown = raw.keyboard.Message == WM_KEYDOWN || raw.keyboard.Message == WM_SYSKEYDOWN;
                        bool isUp = raw.keyboard.Message == WM_KEYUP || raw.keyboard.Message == WM_SYSKEYUP;

                        if (isDown || isUp)
                        {
                            var args = new RawInputEventArgs(raw.header.hDevice, raw.keyboard.VKey, isDown);
                            KeyPressed?.Invoke(this, args);
                        }
                    }
                }
                Marshal.FreeHGlobal(pData);
            }
        }

        public void Dispose()
        {
            DestroyHandle();
        }

        [DllImport("user32.dll")]
        private static extern uint GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint puiNumDevices, uint cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICELIST
        {
            public IntPtr hDevice;
            public uint dwType;
        }
    }

    public class RawInputEventArgs : EventArgs
    {
        public IntPtr DeviceHandle { get; }
        public ushort VirtualKey { get; }
        public bool IsKeyDown { get; }

        public RawInputEventArgs(IntPtr deviceHandle, ushort virtualKey, bool isKeyDown)
        {
            DeviceHandle = deviceHandle;
            VirtualKey = virtualKey;
            IsKeyDown = isKeyDown;
        }
    }
}


