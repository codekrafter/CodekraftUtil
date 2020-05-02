using System;
using System.Runtime.InteropServices;
using System.Text;

using HWND = System.IntPtr;

namespace CodekraftUtil
{
    /*
     * Helper for user32 and win32 methods
     */
    class WindowsHelper
    {
        [DllImport("USER32.DLL")]
        public static extern int GetWindowText(HWND hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("USER32.DLL")]
        public static extern int GetWindowTextLength(HWND hWnd);

        [DllImport("USER32.DLL")]
        public static extern bool IsWindowVisible(HWND hWnd);

        [DllImport("USER32.DLL")]
        public static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(HWND hWnd, ref WINDOWPLACEMENT lpwndpl);

        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        public static WINDOWPLACEMENT GetWindowPlacement(HWND hWnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            GetWindowPlacement(hWnd, ref placement);
            return placement;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetForegroundWindow(HWND hWnd);


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(HWND handle, out int processId);

        public static int GetWindowThreadProcessId(HWND handle)
        {
            int processId;
            GetWindowThreadProcessId(handle, out processId);

            return processId;
        }

        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(HWND hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        public struct POINT
        {
            public long x;
            public long y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(ref POINT point);

        public static POINT GetCursorPos()
        {
            POINT point = new POINT();
            GetCursorPos(ref point);
            return point;
        }

        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        public static void LeftMouseClick(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(HWND hWnd, ref RECT rectangle);

        public struct RECT
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        public static RECT GetWindowRect(HWND hWnd)
        {
            RECT rect = new RECT();
            GetWindowRect(hWnd, ref rect);
            return rect;
        }

        [DllImport("user32")]
        public static extern IntPtr SetWinEventHook(int minEvent, int maxEvent, IntPtr hModule, WinEventProcDelegate proc, int procId, int threadId, int flags);
        public delegate void WinEventProcDelegate(IntPtr hHook, int ev, IntPtr hwnd, int objectId, int childId, int eventThread, int eventTime);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetClassName(HWND hWnd, StringBuilder lpClassName, int nMaxCount);

        public static string GetClassName(HWND hWnd)
        {
            StringBuilder className = new StringBuilder(100);
            GetClassName(hWnd, className, className.Capacity);
            return className.ToString();
        }
    }
}
