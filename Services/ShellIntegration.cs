using System;
using Microsoft.Win32;

namespace SnapIcon.Services
{
    public static class ShellIntegration
    {
        private const string StarShellKeyPath = @"Software\Classes\*\shell\SnapIcon";

        public static bool IsRegistered()
        {
            try
            {
                using var starKey = Registry.CurrentUser.OpenSubKey(StarShellKeyPath);
                return starKey != null;
            }
            catch
            {
                return false;
            }
        }

        public static void Register(string menuTitle = "Сгенерировать иконки в SnapIcon")
        {
            string exePath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
            if (string.IsNullOrEmpty(exePath)) return;

            try
            {
                // Register under Software\Classes\*\shell\SnapIcon with a valid AQS AppliesTo filter
                using (var starKey = Registry.CurrentUser.CreateSubKey(StarShellKeyPath))
                {
                    if (starKey != null)
                    {
                        starKey.SetValue("", menuTitle);
                        starKey.SetValue("MUIVerb", menuTitle);
                        starKey.SetValue("Icon", exePath);
                        // AQS syntax for limiting the shell verb strictly to png, svg, and ico extensions
                        starKey.SetValue("AppliesTo", "System.FileExtension:=.png OR System.FileExtension:=.svg OR System.FileExtension:=.ico");

                        using var cmdKey = starKey.CreateSubKey("Command");
                        cmdKey?.SetValue("", $"\"{exePath}\" \"%1\"");
                    }
                }
            }
            catch
            {
                // Ignore registration errors
            }
        }

        public static void Unregister()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(StarShellKeyPath, throwOnMissingSubKey: false);
            }
            catch { }
        }
    }
}
