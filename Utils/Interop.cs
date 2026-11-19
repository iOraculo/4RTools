using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace _4RTools.Utils
{
    internal static class Interop
    {
        // PINVOKES
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, int Msg, Keys wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        public static void SendKeyDown(IntPtr hWnd, Keys key)
        {
            if (hWnd != IntPtr.Zero && key != Keys.None)
            {
                PostMessage(hWnd, Constants.WM_KEYDOWN_MSG_ID, key, 0);
            }
        }

        public static void SendKeyPress(IntPtr hWnd, Keys key)
        {
            if (hWnd != IntPtr.Zero && key != Keys.None)
            {
                PostMessage(hWnd, Constants.WM_KEYDOWN_MSG_ID, key, 0);
                PostMessage(hWnd, Constants.WM_KEYUP_MSG_ID, key, 0);
            }
        }

        public static void SendLeftClick(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return;
            }

            PostMessage(hWnd, Constants.WM_LBUTTONDOWN, Keys.None, 0);
            Thread.Sleep(1);
            PostMessage(hWnd, Constants.WM_LBUTTONUP, Keys.None, 0);
        }
    }
}
