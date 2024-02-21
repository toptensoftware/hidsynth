using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Topten.WindowsAPI;

namespace hidsynth
{
    internal class InputHook
    {
        public InputHook()
        {

        }

        public void Run()
        {
            _sw.Restart();
            _startTime = GetTimeStamp();
            _count = 0;

            // Install keyboard hook
            _keyboardHook = WinApi.SetWindowsHookEx(WinApi.WH_KEYBOARD_LL, KeyboardHookProc, IntPtr.Zero, 0);
            _mouseHook = WinApi.SetWindowsHookEx(WinApi.WH_MOUSE_LL, MouseHookProc, IntPtr.Zero, 0);

            uint mainThreadId = WinApi.GetCurrentThreadId();

            // Quit on Ctrl+C
            Console.CancelKeyPress += cancelHandler;

            // Message loop
            WinApi.MSG msg;
            while (WinApi.GetMessage(out msg, IntPtr.Zero, 0, 0))
            {
                WinApi.DispatchMessage(ref msg);
            }

            // Clean up
            Console.CancelKeyPress -= cancelHandler;
            WinApi.UnhookWindowsHookEx(_keyboardHook);
            WinApi.UnhookWindowsHookEx(_mouseHook);
            _keyboardHook = IntPtr.Zero;
            _mouseHook = IntPtr.Zero;

            void cancelHandler(object sender, ConsoleCancelEventArgs args)
            {
                Console.WriteLine("\nCancelling...\n");
                WinApi.PostThreadMessage(mainThreadId, WinApi.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
                args.Cancel = true;
            };
        }

        long GetTimeStamp()
        {
            return _sw.ElapsedTicks * 1000000 / Stopwatch.Frequency;
        }

        HashSet<int> _heldKeys = new HashSet<int>();
        IntPtr _keyboardHook = IntPtr.Zero;
        IntPtr _mouseHook = IntPtr.Zero;
        Stopwatch _sw = new Stopwatch();
        long _startTime;
        int _count;

        public Action<InputEvent> OnEvent;

        void FireEvent(int eventId, bool press)
        {
            OnEvent?.Invoke(new InputEvent()
            {
                timeStamp = GetTimeStamp() - _startTime,
                eventId = eventId,
                press = press,
            });

            Console.Write($"{++_count} events\r");
        }


        IntPtr KeyboardHookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0)
            {
                var kbd = Marshal.PtrToStructure<WinApi.KBDLLHOOKSTRUCT>(lParam);
                var scanCode = (int)kbd.scanCode;
                var isPress = (kbd.flags & 0x80) == 0;

                if (isPress)
                {
                    if (_heldKeys.Contains(scanCode))
                        goto exit;
                    _heldKeys.Add(scanCode);
                }
                else
                {
                    _heldKeys.Remove(scanCode);
                }

                FireEvent(scanCode, isPress);
            }

        exit:
            return WinApi.CallNextHookEx(_keyboardHook, code, wParam, lParam);
        }

        IntPtr MouseHookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0)
            {
                switch ((uint)wParam.ToInt32())
                {
                    case WinApi.WM_LBUTTONDOWN:
                        FireEvent(-1, true);
                        break;

                    case WinApi.WM_RBUTTONDOWN:
                        FireEvent(-2, true);
                        break;

                    case WinApi.WM_MBUTTONDOWN:
                        FireEvent(-3, true);
                        break;

                    case WinApi.WM_LBUTTONUP:
                        FireEvent(-1, false);
                        break;

                    case WinApi.WM_RBUTTONUP:
                        FireEvent(-2, false);
                        break;

                    case WinApi.WM_MBUTTONUP:
                        FireEvent(-3, false);
                        break;
                }
            }
            return WinApi.CallNextHookEx(_keyboardHook, code, wParam, lParam);
        }
    }
}
