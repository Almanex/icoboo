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
        private readonly List<string> _batchInputPaths = new();
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
                var validPaths = new List<string>();
                foreach (var item in items)
                {
                    string ext = Path.GetExtension(item.Path).ToLowerInvariant();
                    if (ext == ".png" || ext == ".svg" || ext == ".ico")
                    {
                        validPaths.Add(item.Path);
                    }
                }

                if (validPaths.Count == 1)
                {
                    _batchInputPaths.Clear();
                    _batchInputPaths.Add(validPaths[0]);
                    LoadFile(validPaths[0]);
                }
                else if (validPaths.Count > 1)
                {
                    _batchInputPaths.Clear();
                    _batchInputPaths.AddRange(validPaths);
                    LoadBatchFiles(validPaths);
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
            if (ext != ".png" && ext != ".svg" && ext != ".ico")
            {
                ShowErrorDialog(_resourceLoader.GetString("ErrorUnsupportedFormatTitle"), _resourceLoader.GetString("ErrorUnsupportedFormatMessage"));
                return;
            }

            _selectedInputPath = filePath;

            // Update UI state
            FileNameTextBlock.Text = Path.GetFileName(filePath);
            FileTypeTextBlock.Text = ext switch
            {
                ".svg" => _resourceLoader.GetString("FileTypeSvg"),
                ".ico" => "ICO File",
                _ => _resourceLoader.GetString("FileTypePng")
            };

            if (ext == ".ico")
            {
                IcoExtractCard.Visibility = Visibility.Visible;
                var frames = IconProcessor.ExtractIcoFrames(filePath);
                IcoExtractInfoText.Text = $"Обнаружен мультиформатный ICO файл ({frames.Count} кадров).";
            }
            else
            {
                IcoExtractCard.Visibility = Visibility.Collapsed;
            }

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
                if (ext == ".png" || ext == ".ico")
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

            UpdateLivePreviews();
        }

        private void LoadBatchFiles(List<string> filePaths)
        {
            if (filePaths == null || filePaths.Count == 0) return;
            string firstPath = filePaths[0];

            _selectedInputPath = firstPath;
            FileNameTextBlock.Text = $"Пакетный режим ({filePaths.Count} файлов)";
            FileTypeTextBlock.Text = $"{filePaths.Count} файлов выбрано для генерации";
            FileSizeTextBlock.Text = "Пакетная обработка";

            IcoExtractCard.Visibility = Visibility.Collapsed;

            try
            {
                string ext = Path.GetExtension(firstPath).ToLowerInvariant();
                if (ext == ".png" || ext == ".ico") PreviewImage.Source = new BitmapImage(new Uri(firstPath));
                else if (ext == ".svg") PreviewImage.Source = new SvgImageSource(new Uri(firstPath));
            }
            catch { PreviewImage.Source = null; }

            if (string.IsNullOrEmpty(_selectedOutputPath))
            {
                string dir = Path.GetDirectoryName(firstPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                _selectedOutputPath = Path.Combine(dir, "IconForge_Batch_Output");
                OutputDirTextBox.Text = _selectedOutputPath;
            }

            DropZoneEmptyState.Visibility = Visibility.Collapsed;
            DropZoneLoadedState.Visibility = Visibility.Visible;
            UpdateLivePreviews();
        }

        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedInputPath = null;
            _batchInputPaths.Clear();
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
            picker.FileTypeFilter.Add(".ico");

            var files = await picker.PickMultipleFilesAsync();
            if (files != null && files.Count > 0)
            {
                var validPaths = files.Select(f => f.Path).ToList();
                if (validPaths.Count == 1)
                {
                    _batchInputPaths.Clear();
                    _batchInputPaths.Add(validPaths[0]);
                    LoadFile(validPaths[0]);
                }
                else
                {
                    _batchInputPaths.Clear();
                    _batchInputPaths.AddRange(validPaths);
                    LoadBatchFiles(validPaths);
                }
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
                    if (PreviewBgCustomIndicator != null)
                    {
                        PreviewBgCustomIndicator.Background = new SolidColorBrush(mediaColor);
                    }
                    UpdateLivePreviews();
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
                    string title = _resourceLoader.GetString("ContextMenuItemTitle");
                    if (string.IsNullOrEmpty(title)) title = "Сгенерировать иконки в IconForge";
                    ShellIntegration.Register(title);
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
            bool genFavicon = FaviconPackageCheckBox.IsChecked == true;
            bool genMac = MacIcnsCheckBox.IsChecked == true;

            if (!genIco && !genAssets && !genAndroid && !genFavicon && !genMac)
            {
                ShowErrorDialog(_resourceLoader.GetString("ErrorNoParamsTitle"), _resourceLoader.GetString("ErrorNoParamsMessage"));
                return;
            }

            // Lock controls
            ToggleUI(false);
            LogTextBlock.Text = "";
            ProgressCard.Visibility = Visibility.Visible;

            ShapeMask shapeMask = ShapeMask.None;
            if (ShapeMaskComboBox?.SelectedItem is ComboBoxItem maskItem && Enum.TryParse<ShapeMask>(maskItem.Tag?.ToString(), out var mask))
            {
                shapeMask = mask;
            }

            var options = new IconProcessor.ProcessingOptions
            {
                InputPath = _selectedInputPath,
                OutputPath = _selectedOutputPath,
                AndroidBgColorHex = BgColorTextBox.Text.Trim(),
                AndroidBgImagePath = UseBgImageCheckBox.IsChecked == true ? _selectedBgImagePath : null,
                GenerateWindowsIco = genIco,
                GenerateWindowsAssets = genAssets,
                GenerateAndroidAdaptive = genAndroid,
                GenerateFaviconPackage = genFavicon,
                GenerateMacIcns = genMac,
                Brightness = (float)BrightnessSlider.Value,
                Contrast = (float)ContrastSlider.Value,
                IsGrayscale = GrayscaleCheckBox.IsChecked == true,
                IsInverted = InvertCheckBox.IsChecked == true,
                TintColorHex = TintColorTextBox.Text.Trim(),
                CornerRadiusPercent = (float)CornerRadiusSlider.Value,
                PaddingPercent = (float)PaddingSlider.Value,
                HasDropShadow = DropShadowCheckBox.IsChecked == true,
                AutoCropTransparentMargins = AutoCropCheckBox?.IsChecked == true,
                SelectedShapeMask = shapeMask
            };

            var startTime = DateTime.Now;

            try
            {
                if (_batchInputPaths.Count > 1)
                {
                    int batchIndex = 0;
                    string baseOutputDir = OutputDirTextBox.Text.Trim();

                    foreach (var filePath in _batchInputPaths)
                    {
                        batchIndex++;
                        string nameNoExt = Path.GetFileNameWithoutExtension(filePath);
                        string fileOutputDir = Path.Combine(baseOutputDir, nameNoExt);

                        var itemOptions = options;
                        itemOptions.InputPath = filePath;
                        itemOptions.OutputPath = fileOutputDir;

                        await _processor.ProcessAsync(itemOptions, (status, progressValue) =>
                        {
                            double totalProgress = ((double)(batchIndex - 1) + progressValue) / _batchInputPaths.Count;
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                ProgressStatusTextBlock.Text = $"[{batchIndex}/{_batchInputPaths.Count}] {nameNoExt}: {status}";
                                GenerationProgressBar.Value = totalProgress * 100;
                                LogTextBlock.Text += $"[{DateTime.Now:HH:mm:ss}] [{nameNoExt}] {status}\n";
                            });
                        });
                    }

                    var elapsed = DateTime.Now - startTime;
                    LogTextBlock.Text += $"========================================\n";
                    LogTextBlock.Text += $"Пакетная генерация ({_batchInputPaths.Count} файлов) успешно завершена за {elapsed.TotalMilliseconds:N0} мс!\n";
                    ShowCompletionToast(_selectedOutputPath);
                }
                else
                {
                    await _processor.ProcessAsync(options, (status, progressValue) =>
                    {
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
                    ShowCompletionToast(_selectedOutputPath);
                }
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

        // --- New Feature Handlers ---

        private void FilterSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (BrightnessValueText != null && BrightnessSlider != null) 
                BrightnessValueText.Text = ((int)BrightnessSlider.Value).ToString();
            if (ContrastValueText != null && ContrastSlider != null) 
                ContrastValueText.Text = ((int)ContrastSlider.Value).ToString();
            UpdateLivePreviews();
        }

        private void FilterOption_Changed(object sender, RoutedEventArgs e)
        {
            UpdateLivePreviews();
        }

        private void TintColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TintColorTextBox == null || TintColorPreview == null) return;
            string hex = TintColorTextBox.Text.Trim();
            try
            {
                if (SkiaSharp.SKColor.TryParse(hex, out var color))
                {
                    TintColorPreview.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue));
                }
                else
                {
                    TintColorPreview.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                }
            }
            catch
            {
                TintColorPreview.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            }
            UpdateLivePreviews();
        }

        private void PreviewBg_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string mode && PreviewGridContainer != null)
            {
                switch (mode)
                {
                    case "Dark":
                        PreviewGridContainer.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 31, 31, 31));
                        break;
                    case "Light":
                        PreviewGridContainer.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 243, 243, 243));
                        break;
                    case "CustomBg":
                        PreviewGridContainer.Background = BgColorPreview.Background;
                        break;
                    default:
                        PreviewGridContainer.Background = (Brush)Application.Current.Resources["ControlAltFillColorSecondaryBrush"];
                        break;
                }
            }
        }

        private void PreviewZoom_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string zoomMode && Preview16Container != null)
            {
                if (zoomMode == "1x")
                {
                    Preview256Container.Width = 64; Preview256Container.Height = 64;
                    Preview48Container.Width = 48; Preview48Container.Height = 48;
                    Preview32Container.Width = 32; Preview32Container.Height = 32;
                    Preview16Container.Width = 16; Preview16Container.Height = 16;
                }
                else if (zoomMode == "2x")
                {
                    Preview256Container.Width = 80; Preview256Container.Height = 80;
                    Preview48Container.Width = 64; Preview48Container.Height = 64;
                    Preview32Container.Width = 48; Preview32Container.Height = 48;
                    Preview16Container.Width = 32; Preview16Container.Height = 32;
                }
            }
        }

        private void UpdateLivePreviews()
        {
            if (string.IsNullOrEmpty(_selectedInputPath) || !File.Exists(_selectedInputPath) || Preview256 == null) return;

            string path = _selectedInputPath;
            float brightness = BrightnessSlider != null ? (float)BrightnessSlider.Value : 0;
            float contrast = ContrastSlider != null ? (float)ContrastSlider.Value : 0;
            bool isGrayscale = GrayscaleCheckBox?.IsChecked == true;
            bool isInverted = InvertCheckBox?.IsChecked == true;
            string tint = TintColorTextBox?.Text?.Trim() ?? "";
            float rx = CornerRadiusSlider != null ? (float)CornerRadiusSlider.Value : 0;
            float pad = PaddingSlider != null ? (float)PaddingSlider.Value : 0;
            bool shadow = DropShadowCheckBox?.IsChecked == true;
            bool autoCrop = AutoCropCheckBox?.IsChecked == true;
            ShapeMask shapeMask = ShapeMask.None;
            if (ShapeMaskComboBox?.SelectedItem is ComboBoxItem maskItem && Enum.TryParse<ShapeMask>(maskItem.Tag?.ToString(), out var mask))
            {
                shapeMask = mask;
            }

            Task.Run(() =>
            {
                try
                {
                    using var raw = IconProcessor.LoadBaseBitmap(path);
                    if (raw == null) return;

                    using var filtered = IconProcessor.ApplyFiltersAndStyling(
                        raw,
                        brightness,
                        contrast,
                        isGrayscale,
                        isInverted,
                        tint,
                        rx,
                        pad,
                        shadow,
                        autoCrop,
                        shapeMask);

                    byte[] b256 = EncodeSkiaToPngBytes(filtered, 256);
                    byte[] b48 = EncodeSkiaToPngBytes(filtered, 48);
                    byte[] b32 = EncodeSkiaToPngBytes(filtered, 32);
                    byte[] b16 = EncodeSkiaToPngBytes(filtered, 16);

                    DispatcherQueue.TryEnqueue(async () =>
                    {
                        try
                        {
                            Preview256.Source = await BytesToBitmapImageAsync(b256);
                            Preview48.Source = await BytesToBitmapImageAsync(b48);
                            Preview32.Source = await BytesToBitmapImageAsync(b32);
                            Preview16.Source = await BytesToBitmapImageAsync(b16);
                        }
                        catch { }
                    });
                }
                catch { }
            });
        }

        private static byte[] EncodeSkiaToPngBytes(SKBitmap bmp, int size)
        {
            using var resized = IconProcessor.ResizeBitmap(bmp, size, size, applySharpening: size <= 48);
            using var image = SKImage.FromBitmap(resized);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        private static async Task<BitmapImage> BytesToBitmapImageAsync(byte[] bytes)
        {
            var bitmapImage = new BitmapImage();
            using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            using (var writer = new Windows.Storage.Streams.DataWriter(stream))
            {
                writer.WriteBytes(bytes);
                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream();
            }
            stream.Seek(0);
            await bitmapImage.SetSourceAsync(stream);
            return bitmapImage;
        }

        private void ExtractButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedInputPath) || !File.Exists(_selectedInputPath)) return;
            string outDir = OutputDirTextBox.Text.Trim();
            if (string.IsNullOrEmpty(outDir))
            {
                ShowErrorDialog("Ошибка", "Выберите папку для сохранения извлеченных кадров.");
                return;
            }

            try
            {
                Directory.CreateDirectory(outDir);
                var frames = IconProcessor.ExtractIcoFrames(_selectedInputPath);
                int count = 0;
                foreach (var f in frames)
                {
                    string name = $"ico_frame_{f.Width}x{f.Height}.png";
                    File.WriteAllBytes(Path.Combine(outDir, name), f.Data);
                    count++;
                }
                ShowCompletionToast(outDir);
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Ошибка извлечения", ex.Message);
            }
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            string path = OutputDirTextBox.Text.Trim();
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(path);
                if (folder != null)
                {
                    await Windows.System.Launcher.LaunchFolderAsync(folder);
                }
                else
                {
                    System.Diagnostics.Process.Start("explorer.exe", path);
                }
            }
            catch
            {
                try { System.Diagnostics.Process.Start("explorer.exe", path); } catch { }
            }
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            if (XamlRoot == null) return;

            string title = _resourceLoader.GetString("AboutTitle");
            if (string.IsNullOrEmpty(title)) title = "О программе IconForge";

            string version = _resourceLoader.GetString("AboutVersion");
            if (string.IsNullOrEmpty(version)) version = "Версия 1.0.3 (WinUI 3 / .NET 10)";

            string desc = _resourceLoader.GetString("AboutDescription");
            if (string.IsNullOrEmpty(desc)) desc = "Нативный мультиформатный генератор иконных пакетов для Windows, Web, macOS и Android.";

            string authorPrefix = _resourceLoader.GetString("AboutAuthor");
            if (string.IsNullOrEmpty(authorPrefix)) authorPrefix = "Разработчик:";

            var contentStack = new StackPanel { Spacing = 12, Margin = new Thickness(0, 8, 0, 0) };

            var verText = new TextBlock
            {
                Text = version,
                Style = (Microsoft.UI.Xaml.Style)Application.Current.Resources["BodyTextBlockStyle"],
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };

            var descText = new TextBlock
            {
                Text = desc,
                Style = (Microsoft.UI.Xaml.Style)Application.Current.Resources["CaptionTextBlockStyle"],
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                TextWrapping = TextWrapping.Wrap
            };

            // Harmonious Developer row with inline icon & link
            var devStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
            
            string labelText = authorPrefix.Contains(':') ? authorPrefix.Split(':')[0] + ":" : authorPrefix;
            var devPrefix = new TextBlock
            {
                Text = labelText,
                Style = (Microsoft.UI.Xaml.Style)Application.Current.Resources["CaptionTextBlockStyle"],
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                VerticalAlignment = VerticalAlignment.Center
            };

            var gitHubIcon = new FontIcon
            {
                Glyph = "\uE71B", // GitHub / Code icon
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
            };

            var gitHubLink = new HyperlinkButton
            {
                Content = "Almanex",
                NavigateUri = new Uri("https://github.com/Almanex/IconForge"),
                Padding = new Thickness(2, 0, 2, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = Microsoft.UI.Text.FontWeights.Medium
            };

            devStack.Children.Add(devPrefix);
            devStack.Children.Add(gitHubIcon);
            devStack.Children.Add(gitHubLink);

            contentStack.Children.Add(verText);
            contentStack.Children.Add(descText);
            contentStack.Children.Add(devStack);

            var dialog = new ContentDialog
            {
                Title = title,
                Content = contentStack,
                CloseButtonText = "ОК",
                XamlRoot = XamlRoot,
                RequestedTheme = ActualTheme,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style
            };

            try
            {
                await dialog.ShowAsync();
            }
            catch { }
        }
    }
}
