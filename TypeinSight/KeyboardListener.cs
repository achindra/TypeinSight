using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TypeinSight
{
    class KeyboardListener
    {
        #region public_data
        public struct KBDLLHOOKSTRUCT
        {
            public UInt32 vkCode;
            public UInt32 scanCode;
            public UInt32 flags;
            public UInt32 time;
            public IntPtr extraInfo;
        }

        // events
        public event EventHandler<KBDLLHOOKSTRUCT> KeyUp;
        public event EventHandler<KBDLLHOOKSTRUCT> KeyDown;
        #endregion

        #region private_data
        private HookProc keyboardProc;
        IntPtr _hookHandle = IntPtr.Zero;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
        int code, HookProc func, IntPtr instance, int threadID);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(
        IntPtr hook, int code, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        // hook method called by system
        private delegate IntPtr HookProc(int code, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

        #endregion

        #region private_method
        private IntPtr HookCallback(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                if (null != KeyDown)
                {
                    KeyDown(this, lParam);
                }
            }
            else if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP)
            {
                if (null != KeyUp)
                {
                    KeyUp(this, lParam);
                }
            }
            return CallNextHookEx(_hookHandle, nCode, wParam, ref lParam);
        }
        #endregion

        #region public_method
        public void Hook()
        {
            keyboardProc = HookCallback;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProc,
                                        GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public void UnHook()
        {
            UnhookWindowsHookEx(_hookHandle);
        }
        #endregion

    }
}
