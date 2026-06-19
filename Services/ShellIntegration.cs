using System;
using Microsoft.Win32;

namespace IconForge.Services
{
    public static class ShellIntegration
    {
        private static readonly string[] Extensions = { ".png", ".svg" };
        private const string ShellKeyPath = @"Software\Classes\SystemFileAssociations\{0}\Shell\IconForge";

        public static bool IsRegistered()
        {
            try
            {
                foreach (var ext in Extensions)
                {
                    string path = string.Format(ShellKeyPath, ext);
                    using var key = Registry.CurrentUser.OpenSubKey(path);
                    if (key == null) return false;
                    
                    using var commandKey = key.OpenSubKey("Command");
                    if (commandKey == null) return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Register()
        {
            string exePath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
            if (string.IsNullOrEmpty(exePath)) return;

            foreach (var ext in Extensions)
            {
                string path = string.Format(ShellKeyPath, ext);
                
                // Create key
                using var key = Registry.CurrentUser.CreateSubKey(path);
                if (key != null)
                {
                    key.SetValue("", "Сгенерировать иконки в IconForge");
                    key.SetValue("Icon", exePath);

                    using var commandKey = key.CreateSubKey("Command");
                    if (commandKey != null)
                    {
                        commandKey.SetValue("", $"\"{exePath}\" \"%1\"");
                    }
                }
            }
        }

        public static void Unregister()
        {
            foreach (var ext in Extensions)
            {
                string path = string.Format(ShellKeyPath, ext);
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
                }
                catch
                {
                    // Ignore errors
                }
            }
        }
    }
}
