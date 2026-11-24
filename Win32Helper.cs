using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopCompanion
{
    public static class Win32Helper
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOOLWINDOW = 0x00000080;

        public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        public const int DWMWA_CLOAKED = 14;

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X, Y; }

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

        public static void SetClickThrough(IntPtr hWnd, bool enable)
        {
            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            if (enable)
                SetWindowLong(hWnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
            else
                SetWindowLong(hWnd, GWL_EXSTYLE, exStyle & ~WS_EX_TRANSPARENT);
        }
public const uint SWP_NOSIZE = 0x0001;
public const uint SWP_NOZORDER = 0x0004;
public const uint SWP_NOACTIVATE = 0x0010;
public const uint SWP_ASYNCWINDOWPOS = 0x4000;

[DllImport("user32.dll", SetLastError = true)]
public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

[DllImport("winmm.dll")]
public static extern uint timeBeginPeriod(uint uPeriod);

[DllImport("winmm.dll")]
public static extern uint timeEndPeriod(uint uPeriod);

/// <summary>
/// Мгновенное перемещение окна на частоте 144-260 Гц в обход медленных свойств WPF Left/Top
/// </summary>
public static void MoveWindowFast(IntPtr hWnd, int pixelX, int pixelY)
{
    SetWindowPos(hWnd, IntPtr.Zero, pixelX, pixelY, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
}

        public static List<RECT> GetWindowPlatforms(IntPtr ignoreHwnd)
        {
            var platforms = new List<RECT>();

            EnumWindows((hWnd, lParam) =>
            {
                if (hWnd == ignoreHwnd || !IsWindowVisible(hWnd) || IsIconic(hWnd))
                    return true;

                DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int isCloaked, sizeof(int));
                if (isCloaked != 0) return true;

                var sb = new StringBuilder(256);
                if (GetWindowText(hWnd, sb, 256) == 0) return true;
                string title = sb.ToString();
                if (title == "Program Manager" || title == "Windows Input Experience") return true;

                if (DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT rect, Marshal.SizeOf(typeof(RECT))) == 0)
                {
                    if ((rect.Right - rect.Left) > 120 && (rect.Bottom - rect.Top) > 80)
                        platforms.Add(rect);
                }
                return true;
            }, IntPtr.Zero);

            return platforms;
        }

        public static List<string> GetActiveWindowTitles(IntPtr ignoreHwnd)
        {
            var titles = new List<string>();
            EnumWindows((hWnd, lParam) =>
            {
                if (hWnd == ignoreHwnd || !IsWindowVisible(hWnd) || IsIconic(hWnd)) return true;
                DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int isCloaked, sizeof(int));
                if (isCloaked != 0) return true;

                var sb = new StringBuilder(256);
                if (GetWindowText(hWnd, sb, 256) > 0)
                {
                    string title = sb.ToString();
                    if (title != "Program Manager" && title != "Windows Input Experience" && !titles.Contains(title))
                        titles.Add(title);
                }
                return true;
            }, IntPtr.Zero);
            return titles;
        }
    }
}