using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SleepyWindows
{
    internal sealed class LanguageInfo
    {
        public LanguageInfo(string code, string nativeName)
        {
            Code = code;
            NativeName = nativeName;
        }

        public string Code { get; }
        public string NativeName { get; }
    }

    /// <summary>
    /// UI text. Every translation is a plain "key = value" file in the lang folder
    /// (lang/en.txt, lang/de.txt, ...). They are embedded into the .exe at build time,
    /// so the app stays a single portable file. A key missing from a translation falls back to English.
    /// </summary>
    internal static class Strings
    {
        public const string AppName = "SleepyWindows";

        private const string FallbackCode = "en";
        private const string ResourcePrefix = "SleepyWindows.lang.";
        private const string ResourceSuffix = ".txt";

        private static readonly Dictionary<string, Dictionary<string, string>> Tables;
        private static readonly Dictionary<string, string> English;
        private static Dictionary<string, string> current;

        static Strings()
        {
            Tables = LoadAll();
            if (!Tables.TryGetValue(FallbackCode, out English))
            {
                English = new Dictionary<string, string>();
            }

            Available = Tables
                .Select(t => new LanguageInfo(t.Key, t.Value.TryGetValue("_name", out string name) ? name : t.Key))
                .OrderBy(l => l.NativeName, StringComparer.InvariantCultureIgnoreCase)
                .ToList();

            SetLanguage(null);
        }

        /// <summary>Languages compiled into the app, sorted by their own name.</summary>
        public static IList<LanguageInfo> Available { get; }

        /// <summary>Code of the language in use, e.g. "en", "pt", "zh-Hans".</summary>
        public static string CurrentCode { get; private set; }

        /// <summary>True when the language follows Windows instead of a choice made in the app.</summary>
        public static bool IsAutomatic { get; private set; }

        /// <summary>Culture used for month names in dates.</summary>
        public static CultureInfo DateCulture { get; private set; }

        /// <summary>Switches language. Null, empty or unknown codes mean "follow Windows".</summary>
        public static void SetLanguage(string code)
        {
            string match = FindCode(code);
            IsAutomatic = match == null;
            CurrentCode = match ?? Detect();
            current = Tables.TryGetValue(CurrentCode, out Dictionary<string, string> table) ? table : English;
            DateCulture = CultureFor(CurrentCode);
        }

        public static string Get(string key)
        {
            if (current != null && current.TryGetValue(key, out string value) && value.Length > 0)
            {
                return value;
            }
            return English.TryGetValue(key, out value) ? value : key;
        }

        private static string Format(string key, params object[] args)
        {
            try
            {
                return string.Format(CultureInfo.CurrentCulture, Get(key), args);
            }
            catch (FormatException)
            {
                // A broken placeholder in a translation shouldn't crash the app.
                return string.Format(CultureInfo.CurrentCulture, English.TryGetValue(key, out string value) ? value : key, args);
            }
        }

        // ---------------- Texts ----------------

        public static string Language => Get("Language");
        public static string LanguageAuto => Get("LanguageAuto");

        public static string WhatToDo => Get("WhatToDo");
        public static string InHowLong => Get("InHowLong");
        public static string CustomTime => Get("CustomTime");

        public static string Seconds => Get("UnitSeconds");
        public static string Minutes => Get("UnitMinutes");
        public static string Hours => Get("UnitHours");

        public static string Schedule => Get("Schedule");
        public static string Snooze => Get("Snooze");
        public static string Cancel => Get("Cancel");
        public static string CancelExisting => Get("CancelExisting");

        public static string ActionName(PowerAction action) => Get("Action" + action);
        public static string CountdownTitle(PowerAction action) => Get("Countdown" + action);

        public static string WindowsComment(PowerAction action) =>
            Get(action == PowerAction.Restart ? "CommentRestart" : "CommentShutdown");

        public static string HintWindowsScheduler => Get("HintWindowsScheduler");
        public static string HintAppTimer => Get("HintAppTimer");

        public static string Preset(int minutes)
        {
            if (minutes < 60)
            {
                return Format("PresetMinutes", minutes);
            }
            return minutes % 60 == 0
                ? Format("PresetHours", minutes / 60)
                : Format("PresetHoursMinutes", minutes / 60, minutes % 60);
        }

        public static string TargetTime(DateTime localTarget)
        {
            DateTime today = DateTime.Now.Date;
            // Time follows the user's regional settings; the month name follows the app language.
            string time = localTarget.ToString("t", CultureInfo.CurrentCulture);

            if (localTarget.Date == today)
            {
                return Format("AtTime", time);
            }
            if (localTarget.Date == today.AddDays(1))
            {
                return Format("TomorrowAt", time);
            }
            return Format("DateAt", localTarget.ToString("M", DateCulture), time);
        }

        public static string InvalidNumber => Get("InvalidNumber");
        public static string TooLong => Get("TooLong");
        public static string AlreadyScheduled => Get("AlreadyScheduled");

        public static string WindowsRefused(int code) =>
            code == PowerControl.ErrorCouldNotRun ? Get("CouldNotRun") : Format("WindowsRefused", code);

        public static string ExistingCancelled => Get("ExistingCancelled");
        public static string NothingToCancel => Get("NothingToCancel");
        public static string CloseCancelsTimer => Get("CloseCancelsTimer");
        public static string HibernateUnavailable => Get("HibernateUnavailable");
        public static string SuspendFailed => Get("SuspendFailed");

        // ---------------- Loading ----------------

        private static Dictionary<string, Dictionary<string, string>> LoadAll()
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            Assembly assembly = typeof(Strings).Assembly;

            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (!name.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                    || !name.EndsWith(ResourceSuffix, StringComparison.Ordinal))
                {
                    continue;
                }

                string code = name.Substring(ResourcePrefix.Length, name.Length - ResourcePrefix.Length - ResourceSuffix.Length);
                try
                {
                    using (Stream stream = assembly.GetManifestResourceStream(name))
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        result[code] = Parse(reader);
                    }
                }
                catch (Exception)
                {
                    // Skip a broken file; the other languages still work.
                }
            }

            return result;
        }

        /// <summary>Reads "key = value" lines. Blank lines and lines starting with # are ignored; \n means a line break.</summary>
        internal static Dictionary<string, string> Parse(TextReader reader)
        {
            var table = new Dictionary<string, string>(StringComparer.Ordinal);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed[0] == '#')
                {
                    continue;
                }

                int equals = trimmed.IndexOf('=');
                if (equals <= 0)
                {
                    continue;
                }

                string key = trimmed.Substring(0, equals).Trim();
                string value = trimmed.Substring(equals + 1).Trim().Replace("\\n", "\n");
                table[key] = value;
            }
            return table;
        }

        private static string FindCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }
            return Tables.Keys.FirstOrDefault(k => string.Equals(k, code.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Picks the translation that best matches the Windows display language.</summary>
        private static string Detect()
        {
            CultureInfo ui = CultureInfo.CurrentUICulture;

            if (ui.TwoLetterISOLanguageName == "zh")
            {
                string n = ui.Name;
                bool traditional = n.IndexOf("Hant", StringComparison.OrdinalIgnoreCase) >= 0
                                   || n.EndsWith("TW", StringComparison.OrdinalIgnoreCase)
                                   || n.EndsWith("HK", StringComparison.OrdinalIgnoreCase)
                                   || n.EndsWith("MO", StringComparison.OrdinalIgnoreCase)
                                   || n.EndsWith("CHT", StringComparison.OrdinalIgnoreCase);
                string chinese = FindCode(traditional ? "zh-Hant" : "zh-Hans");
                if (chinese != null)
                {
                    return chinese;
                }
            }

            // pt-BR -> pt, de-AT -> de, ...
            for (CultureInfo c = ui; c != null && !string.IsNullOrEmpty(c.Name); c = c.Parent)
            {
                string match = FindCode(c.Name);
                if (match != null)
                {
                    return match;
                }
            }

            return FindCode(ui.TwoLetterISOLanguageName) ?? FallbackCode;
        }

        private static CultureInfo CultureFor(string code)
        {
            try
            {
                CultureInfo language = CultureInfo.GetCultureInfo(code);
                CultureInfo regional = CultureInfo.CurrentCulture;
                // Same language as the regional settings: keep the regional variant (pt-BR vs pt-PT, ...).
                return regional.TwoLetterISOLanguageName == language.TwoLetterISOLanguageName ? regional : language;
            }
            catch (Exception)
            {
                return CultureInfo.InvariantCulture;
            }
        }
    }
}
