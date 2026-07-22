using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SnapIcon;

/// <summary>
/// The application window. This hosts a Frame that displays pages. Add your
/// UI and logic to MainPage.xaml / MainPage.xaml.cs instead of here so you
/// can use Page features such as navigation events and the Loaded lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();

        var loader = new Microsoft.Windows.ApplicationModel.Resources.ResourceLoader();
        string appTitle = loader.GetString("AppTitle");
        Title = appTitle;
        AppTitleBar.Title = appTitle;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Load and set app icon from embedded resources
        string tempIconPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"SnapIcon_AppIcon_{System.Diagnostics.Process.GetCurrentProcess().Id}.ico");
        try
        {
            using (var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("SnapIcon.Assets.AppIcon.ico"))
            {
                if (stream != null)
                {
                    using (var fileStream = System.IO.File.Create(tempIconPath))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
            }
            if (System.IO.File.Exists(tempIconPath))
            {
                AppWindow.SetIcon(tempIconPath);
                TitleBarImageIconSource.ImageSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(tempIconPath));
            }
            
            // Clean up the temp file on close
            this.Closed += (s, e) =>
            {
                try
                {
                    if (System.IO.File.Exists(tempIconPath))
                    {
                        System.IO.File.Delete(tempIconPath);
                    }
                }
                catch { }
            };
        }
        catch
        {
            // Ignore/fallback
        }

        // Parse command line arguments for initial file path (from context menu)
        string? initialFilePath = null;
        try
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                string possiblePath = args[1];
                if (File.Exists(possiblePath))
                {
                    string ext = System.IO.Path.GetExtension(possiblePath).ToLowerInvariant();
                    if (ext == ".png" || ext == ".svg")
                    {
                        initialFilePath = possiblePath;
                    }
                }
            }
        }
        catch
        {
            // Ignore arguments parsing errors
        }

        // Navigate the root frame to the main page on startup.
        RootFrame.Navigate(typeof(MainPage), initialFilePath);
    }
}
