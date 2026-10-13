using System;
using System.Threading;
using System.Windows.Forms;

namespace SleepyWindows
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // One instance at a time: opening the .exe again just brings the existing window back,
            // so two timers can't fight each other.
            using (var mutex = new Mutex(true, "SleepyWindows.SingleInstance", out bool isFirst))
            {
                if (!isFirst)
                {
                    NativeMethods.BroadcastShowMe();
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Windows language by default, unless the user picked one in the app.
                Strings.SetLanguage(Settings.LoadLanguage());
                Application.Run(new MainForm());
            }
        }
    }
}
