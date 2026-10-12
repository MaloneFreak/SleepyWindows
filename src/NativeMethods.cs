using System;
using System.Runtime.InteropServices;

namespace SleepyWindows
{
    internal static class NativeMethods
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19; // Windows 10 1809 - 1909
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;     // Windows 10 2004+ and Windows 11
        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);

        private static int showMeMessage = -1;

        /// <summary>Window message the second instance broadcasts so the first one shows itself (0 if unavailable).</summary>
        public static int ShowMeMessage
        {
            get
            {
                if (showMeMessage < 0)
                {
                    try
                    {
                        showMeMessage = RegisterWindowMessage("SleepyWindows.ShowMe");
                    }
                    catch (Exception)
                    {
                        showMeMessage = 0;
                    }
                }
                return showMeMessage;
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("powrprof.dll")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool IsPwrHibernateAllowed();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int RegisterWindowMessage(string message);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        /// <summary>Makes the window's title bar dark to match the app (Windows 10 1809+ only).</summary>
        public static void UseDarkTitleBar(IntPtr hwnd)
        {
            if (Environment.OSVersion.Version.Major < 10)
            {
                return;
            }

            try
            {
                int on = 1;
                if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref on, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref on, sizeof(int));
                }
            }
            catch (Exception)
            {
                // Purely cosmetic; ignore on systems without dwmapi support.
            }
        }

        /// <summary>False when hibernation is turned off on this PC (powercfg /h off).</summary>
        public static bool HibernateAllowed()
        {
            try
            {
                return IsPwrHibernateAllowed();
            }
            catch (Exception)
            {
                return true;
            }
        }

        public static void BroadcastShowMe()
        {
            try
            {
                PostMessage(HWND_BROADCAST, ShowMeMessage, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception)
            {
                // Nothing useful to do if this fails.
            }
        }
    }
}
