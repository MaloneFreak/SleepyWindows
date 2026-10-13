using System;
using System.IO;
using System.Text;

namespace SleepyWindows
{
    /// <summary>
    /// The only thing the app remembers is a language picked by hand. It lives in
    /// %AppData%\SleepyWindows\settings.ini and is only created when the user changes the language.
    /// Going back to "Automatic" deletes it again. Nothing is ever written to the registry.
    /// </summary>
    internal static class Settings
    {
        private const string LanguageKey = "language";

        private static string FolderPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SleepyWindows");

        private static string FilePath => Path.Combine(FolderPath, "settings.ini");

        public static string LoadLanguage()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return null;
                }

                foreach (string line in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    int equals = line.IndexOf('=');
                    if (equals > 0 && line.Substring(0, equals).Trim().Equals(LanguageKey, StringComparison.OrdinalIgnoreCase))
                    {
                        string value = line.Substring(equals + 1).Trim();
                        return value.Length == 0 ? null : value;
                    }
                }
            }
            catch (Exception)
            {
                // Unreadable settings just mean "automatic".
            }
            return null;
        }

        /// <summary>Saves a language code, or removes the setting when code is null (automatic).</summary>
        public static void SaveLanguage(string code)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    if (File.Exists(FilePath))
                    {
                        File.Delete(FilePath);
                    }
                    if (Directory.Exists(FolderPath) && Directory.GetFileSystemEntries(FolderPath).Length == 0)
                    {
                        Directory.Delete(FolderPath);
                    }
                    return;
                }

                Directory.CreateDirectory(FolderPath);
                File.WriteAllText(FilePath, LanguageKey + "=" + code + Environment.NewLine, new UTF8Encoding(false));
            }
            catch (Exception)
            {
                // Not being able to remember the choice is harmless.
            }
        }
    }
}
