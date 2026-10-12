using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace SleepyWindows
{
    internal enum PowerAction
    {
        Shutdown = 0,
        Restart = 1,
        Sleep = 2,
        Hibernate = 3,
    }

    /// <summary>
    /// Shutdown and restart are handed to Windows itself (shutdown.exe /s or /r with /t), so they
    /// still happen even if the app is closed. Windows has no built-in timer for sleep or
    /// hibernate, so for those the app keeps its own countdown and calls SetSuspendState at the end.
    /// </summary>
    internal static class PowerControl
    {
        public const int Success = 0;
        public const int ErrorShutdownIsScheduled = 1190;   // ERROR_SHUTDOWN_IS_SCHEDULED
        public const int ErrorNoShutdownInProgress = 1116;  // ERROR_NO_SHUTDOWN_IN_PROGRESS
        public const int ErrorCouldNotRun = -1;

        public static bool UsesWindowsScheduler(PowerAction action)
        {
            return action == PowerAction.Shutdown || action == PowerAction.Restart;
        }

        /// <summary>Runs "shutdown /s|/r /t seconds /c comment". Returns shutdown.exe's exit code.</summary>
        public static int ScheduleWithWindows(PowerAction action, int seconds, string comment)
        {
            string mode = action == PowerAction.Restart ? "/r" : "/s";
            string safeComment = comment.Replace("\"", "'");
            return RunShutdownExe(mode + " /t " + seconds + " /c \"" + safeComment + "\"");
        }

        /// <summary>Runs "shutdown /a". Returns shutdown.exe's exit code.</summary>
        public static int AbortWindowsSchedule()
        {
            return RunShutdownExe("/a");
        }

        /// <summary>Puts the PC to sleep or into hibernation right now. Returns false if Windows refused.</summary>
        public static bool SuspendNow(PowerAction action)
        {
            var state = action == PowerAction.Hibernate ? PowerState.Hibernate : PowerState.Suspend;
            return Application.SetSuspendState(state, false, false);
        }

        private static int RunShutdownExe(string arguments)
        {
            try
            {
                // Full path so a stray shutdown.exe elsewhere on PATH can never be picked up.
                string exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shutdown.exe");
                var info = new ProcessStartInfo(exe, arguments)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };

                using (var process = Process.Start(info))
                {
                    if (process == null || !process.WaitForExit(15000))
                    {
                        return ErrorCouldNotRun;
                    }
                    return process.ExitCode;
                }
            }
            catch (Exception)
            {
                return ErrorCouldNotRun;
            }
        }
    }
}
