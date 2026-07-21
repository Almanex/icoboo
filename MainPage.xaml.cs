using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Microsoft.UI.Xaml.Navigation;
using SkiaSharp;
using IconForge.Services;
using IconForge.Helpers;

namespace IconForge
{
    public sealed partial class MainPage : Page
    {
        private string? _selectedInputPath;
        private string? _selectedOutputPath;
        private string? _selectedBgImagePath;
        private readonly IconProcessor _processor = new();
        private readonly Microsoft.Windows.ApplicationModel.Resources.ResourceLoader _resourceLoader = new();

        public MainPage()
        {
            InitializeComponent();
            Loaded += MainPage_Loaded;

            // Load embedded logo image
            try
            {
                using (var stream = typeof(MainPage).Assembly.GetManifestResourceStream("IconForge.Assets.Square44x44Logo.scale-200.png"))
                {
                    if (stream != null)
                    {
                        var bitmap = new BitmapImage();
                        var randomAccessStream = System.IO.WindowsRuntimeStreamExtensions.AsRandomAccessStream(stream);
                        bitmap.SetSource(randomAccessStream);
                        AppLogoImage.Source = bitmap;
                    }
                }
            }
            catch
            {
                // Ignore/fallback
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string filePath && !string.IsNullOrEmpty(filePath))
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    LoadFile(filePath);
                });
            }
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize Context Menu Toggle state from Registry
            try
            {
                ContextMenuToggle.IsOn = ShellIntegration.IsRegistered();
            }
            catch
            {
                ContextMenuToggle.IsEnabled = false;
            }
        }

        // --- Drag & Drop Handlers ---

        private void DropZone_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            DropZone.BorderBrush = (Brush)Application.Current.Resources["SystemControlHighlightAccentBrush"];
            DropZone.Background = (Brush)Application.Current.Resources["ControlAltFillColorTertiaryBrush"];
        }

        private void DropZone_DragLeave(object sender, DragEventArgs e)
        {
            ResetDropZoneVisuals();
        }

        private async void DropZone_Drop(object sender, DragEventArgs e)
        {
            ResetDropZoneVisuals();

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    string filePath = items[0].Path;
                    LoadFile(filePath);
                }
            }
        }

        private void DropZone_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // Click on the empty drop zone triggers file browsing
            if (_selectedInputPath == null)
            {
                BrowseButton_Click(sender, new RoutedEventArgs());
            }
        }

        private void ResetDropZoneVisuals()
        {
            DropZone.BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
            DropZone.Background = (Brush)Application.Current.Resources["ControlAltFillColorSecondaryBrush"];
        }

        // --- Loading Source File ---

        private void LoadFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext != ".png" && ext != ".svg")
            {
                ShowErrorDialog(_resourceLoader.GetString("ErrorUnsupportedFormatTitle"), _resourceLoader.GetString("ErrorUnsupportedFormatMessage"));
                return;
            }

            _selectedInputPath = filePath;

            // Update UI state
            FileNameTextBlock.Text = Path.GetFileName(filePath);
            FileTypeTextBlock.Text = ext == ".svg" ? _resourceLoader.GetString("FileTypeSvg") : _resourceLoader.GetString("FileTypePng");

            try
            {
                var fileInfo = new FileInfo(filePath);
                double sizeMb = (double)fileInfo.Length / (1024 * 1024);
                FileSizeTextBlock.Text = sizeMb < 0.1 
                    ? string.Format(_resourceLoader.GetString("FileSizeKb"), fileInfo.Length / 1024.0) 
                    : string.Format(_resourceLoader.GetString("FileSizeMb"), sizeMb);
            }
            catch
            {
                FileSizeTextBlock.Text = _resourceLoader.GetString("FileSizeUnknown");
            }

            // Display previews
            try
            {
                if (ext == ".png")
                {
                    PreviewImage.Source = new BitmapImage(new Uri(filePath));
                }
                else if (ext == ".svg")
                {
                    PreviewImage.Source = new SvgImageSource(new Uri(filePath));
                }
            }
            catch
            {
                // Fallback if local URI projection fails
                PreviewImage.Source = null;
            }

            // Automatically set default output directory
            if (string.IsNullOrEmpty(_selectedOutputPath))
            {
                string dir = Path.GetDirectoryName(filePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                string nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                _selectedOutputPath = Path.Combine(dir, $"{nameWithoutExt}_icons");
                OutputDirTextBox.Text = _selectedOutputPath;
            }

            DropZoneEmptyState.Visibility = Visibility.Collapsed;
            DropZoneLoadedState.Visibility = Visibility.Visible;
        }

        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedInputPath = null;
            PreviewImage.Source = null;
            
            DropZoneLoadedState.Visibility = Visibility.Collapsed;
            DropZoneEmptyState.Visibility = Visibility.Visible;
        }

        // --- Browse Buttons ---

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Instance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".svg");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                LoadFile(file.Path);
            }
        }

        private async void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Instance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add("*");

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                _selectedOutputPath = folder.Path;
                OutputDirTextBox.Text = folder.Path;
            }
        }

        // --- Android Background customization ---

        private void BgColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string hex = BgColorTextBox.Text.Trim();
            if (hex.StartsWith("#") && (hex.Length == 7 || hex.Length == 9))
            {
                try
                {
                    var color = SKColor.Parse(hex);
                    var mediaColor = Windows.UI.Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
                    BgColorPreview.Background = new SolidColorBrush(mediaColor);
                }
                catch
                {
                    // Ignore parsing errors while typing
                }
            }
        }

        private void Swatch_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                BgColorTextBox.Text = hex;
            }
        }

        private void UseBgImage_Changed(object sender, RoutedEventArgs e)
        {
            if (BgImagePickerGrid != null)
            {
                BgImagePickerGrid.Visibility = UseBgImageCheckBox.IsChecked == true 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }

        private async void BrowseBgImageButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Instance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _selectedBgImagePath = file.Path;
                BgImagePathTextBox.Text = file.Path;
            }
        }

        // --- Context Menu Handler ---

        private void ContextMenuToggle_Toggled(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ContextMenuToggle.IsOn)
                {
                    ShellIntegration.Register();
                }
                else
                {
                    ShellIntegration.Unregister();
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog(_resourceLoader.GetString("ErrorIntegrationTitle"), string.Format(_resourceLoader.GetString("ErrorIntegrationMessage"), ex.Message));
                // Revert toggle state
                ContextMenuToggle.Toggled -= ContextMenuToggle_Toggled;
                ContextMenuToggle.IsOn = !ContextMenuToggle.IsOn;
                ContextMenuToggle.Toggled += ContextMenuToggle_Toggled;
            }
        }

        // --- Generation process ---

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrEmpty(_selectedInputPath))
            {
                ShowErrorDialog(_resourceLoader.GetString("ErrorNoFileTitle"), _resourceLoader.GetString("ErrorNoFileMessage"));
                return;
            }

            _selectedOutputPath = OutputDirTextBox.Text.Trim();
            if (string.IsNullOrEmpty(_selectedOutputPath))
            {
                ShowErrorDialog(_resourceLoader.GetString("ErrorNoOutputDirTitle"), _resourceLoader.GetString("ErrorNoOutputDirMessage"));
                return;
            }

            bool genIco = WindowsIcoCheckBox.IsChecked == true;
            bool genAssets = WindowsAssetsCheckBox.IsChecked == true;
            bool genAndroid = AndroidAdaptiveCheckBox.IsChecked == true;

            if (!genIco && !genAssets && !genAndroid)
            {
                ShowErrorDialog(_resourceLoader.GetString("ErrorNoParamsTitle"), _resourceLoader.GetString("ErrorNoParamsMessage"));
                return;
            }

            // Lock controls
            ToggleUI(false);
            LogTextBlock.Text = "";
            ProgressCard.Visibility = Visibility.Visible;

            var options = new IconProcessor.ProcessingOptions
            {
                InputPath = _selectedInputPath,
                OutputPath = _selectedOutputPath,
                AndroidBgColorHex = BgColorTextBox.Text.Trim(),
                AndroidBgImagePath = UseBgImageCheckBox.IsChecked == true ? _selectedBgImagePath : null,
                GenerateWindowsIco = genIco,
                GenerateWindowsAssets = genAssets,
                GenerateAndroidAdaptive = genAndroid
            };

            var startTime = DateTime.Now;

            try
            {
                await _processor.ProcessAsync(options, (status, progressValue) =>
                {
                    // Marshall to UI Thread
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        ProgressStatusTextBlock.Text = status;
                        GenerationProgressBar.Value = progressValue * 100;
                        LogTextBlock.Text += $"[{DateTime.Now:HH:mm:ss}] {status}\n";
                    });
                });

                var elapsed = DateTime.Now - startTime;
                LogTextBlock.Text += $"========================================\n";
                LogTextBlock.Text += string.Format(_resourceLoader.GetString("SuccessMessage"), elapsed.TotalMilliseconds) + "\n";
                
                // Show completion Toast Notification
                ShowCompletionToast(_selectedOutputPath);
            }
            catch (Exception ex)
            {
                LogTextBlock.Text += $"[ERROR] {ex.Message}\n";
                ShowErrorDialog(_resourceLoader.GetString("ErrorGenerationTitle"), ex.Message);
            }
            finally
            {
                ToggleUI(true);
            }
        }

        private void ToggleUI(bool enable)
        {
            GenerateButton.IsEnabled = enable;
            BrowseButton.IsEnabled = enable;
            
            // Disable inputs during generation
            DropZone.AllowDrop = enable;
            WindowsIcoCheckBox.IsEnabled = enable;
            WindowsAssetsCheckBox.IsEnabled = enable;
            AndroidAdaptiveCheckBox.IsEnabled = enable;
            BgColorTextBox.IsEnabled = enable;
            UseBgImageCheckBox.IsEnabled = enable;
            BgImagePathTextBox.IsEnabled = enable;
            BrowseBgImageButton.IsEnabled = enable;
            
            if (BgImagePickerGrid != null)
            {
                BgImagePickerGrid.Visibility = (UseBgImageCheckBox.IsChecked == true) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }

        private void ShowCompletionToast(string outputPath)
        {
            try
            {
                string folderUri = new Uri(outputPath).AbsoluteUri;
                string title = _resourceLoader.GetString("ToastTitle");
                string message = _resourceLoader.GetString("ToastMessage");
                string buttonText = _resourceLoader.GetString("ToastButtonText");
                
                var toastXml = new XmlDocument();
                string xml = $@"
<toast>
    <visual>
        <binding template='ToastGeneric'>
            <text>{title}</text>
            <text>{message}</text>
        </binding>
    </visual>
    <actions>
        <action content='{buttonText}' arguments='{folderUri}' activationType='protocol' />
    </actions>
</toast>";
                toastXml.LoadXml(xml);
                var toast = new ToastNotification(toastXml);
                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
            catch
            {
                // Fallback if notifications fail
            }
        }

        private async void ShowErrorDialog(string title, string content)
        {
            if (XamlRoot == null)
            {
                Loaded += async (s, e) =>
                {
                    if (XamlRoot == null) return;
                    var dialog = new ContentDialog
                    {
                        Title = title,
                        Content = content,
                        CloseButtonText = "ОК",
                        XamlRoot = XamlRoot
                    };
                    try
                    {
                        await dialog.ShowAsync();
                    }
                    catch
                    {
                        // Ignore secondary dialog failures
                    }
                };
                return;
            }

            var errDialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "ОК",
                XamlRoot = XamlRoot
            };
            try
            {
                await errDialog.ShowAsync();
            }
            catch
            {
                // Ignore dialog exceptions
            }
        }
    }
}
