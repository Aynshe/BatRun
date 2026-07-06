using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.Input
{
    public static class KeyboardSimulator
    {
        public static void SendKeyStroke(string keyString)
        {
            if (string.IsNullOrWhiteSpace(keyString)) return;

            string[] keys = keyString.Split('+');
            List<ushort> vkeys = new List<ushort>();
            foreach (var k in keys)
            {
                string keyName = k.Trim().ToUpper();
                ushort vk = GetVirtualKey(keyName);
                if (vk != 0) vkeys.Add(vk);
            }

            if (vkeys.Count == 0) return;

            // EN: Batch ALL key downs into a single native call to prevent menu-bar glitches (Alt hold)
            // FR: Regrouper tous les appuis dans un seul appel natif pour éviter les glitchs de menu
            NativeMethods.INPUT[] inputsDown = new NativeMethods.INPUT[vkeys.Count];
            for (int i = 0; i < vkeys.Count; i++)
            {
                inputsDown[i] = CreateInput(vkeys[i], false);
            }
            NativeMethods.SendInput((uint)inputsDown.Length, inputsDown, Marshal.SizeOf(typeof(NativeMethods.INPUT)));

            Thread.Sleep(50); // EN: 50ms hold is usually enough and snappy

            vkeys.Reverse();
            NativeMethods.INPUT[] inputsUp = new NativeMethods.INPUT[vkeys.Count];
            for (int i = 0; i < vkeys.Count; i++)
            {
                inputsUp[i] = CreateInput(vkeys[i], true);
            }
            NativeMethods.SendInput((uint)inputsUp.Length, inputsUp, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }

        private static NativeMethods.INPUT CreateInput(ushort vKey, bool keyUp)
        {
            ushort scanCode = (ushort)NativeMethods.MapVirtualKey(vKey, NativeMethods.MAPVK_VK_TO_VSC);
            NativeMethods.INPUT input = new NativeMethods.INPUT();
            input.type = NativeMethods.INPUT_KEYBOARD;
            input.ki.wVk = vKey;
            input.ki.wScan = scanCode;
            input.ki.dwFlags = NativeMethods.KEYEVENTF_SCANCODE | (keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0u);
            input.ki.time = 0;
            input.ki.dwExtraInfo = IntPtr.Zero;
            return input;
        }

        private static ushort GetVirtualKey(string keyName)
        {
            // EN: Always prioritize custom mapping to avoid 'Keys.Alt' modifier flag (262144) resulting in 0
            switch (keyName)
            {
                case "ALT": return (ushort)Keys.Menu;
                case "CTRL": 
                case "CONTROL": return (ushort)Keys.ControlKey;
                case "SHIFT": return (ushort)Keys.ShiftKey;
                case "ESC":
                case "ESCAPE": return (ushort)Keys.Escape;
                case "ENTER": return (ushort)Keys.Enter;
                case "SPACE": return (ushort)Keys.Space;
            }

            if (Enum.TryParse(keyName, true, out Keys parsedKey))
            {
                // Unmask modifiers just in case, taking only the base key code
                return (ushort)((int)parsedKey & 0xFFFF);
            }

            return 0;
        }
    }
}


