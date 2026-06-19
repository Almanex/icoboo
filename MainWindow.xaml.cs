using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IconForge;

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

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));

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
