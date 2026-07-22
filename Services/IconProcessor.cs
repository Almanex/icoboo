using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using SkiaSharp;
using Svg.Skia;
using IconForge.Helpers;

namespace IconForge.Services
{
    public class IconProcessor
    {
        public struct ProcessingOptions
        {
            public string InputPath { get; set; }
            public string OutputPath { get; set; }
            public string AndroidBgColorHex { get; set; }
            public string? AndroidBgImagePath { get; set; }
            public bool GenerateWindowsIco { get; set; }
            public bool GenerateWindowsAssets { get; set; }
            public bool GenerateAndroidAdaptive { get; set; }
            public bool GenerateFaviconPackage { get; set; }
            public bool GenerateMacIcns { get; set; }
            public float Brightness { get; set; }
            public float Contrast { get; set; }
            public bool IsGrayscale { get; set; }
            public bool IsInverted { get; set; }
            public string? TintColorHex { get; set; }
            public float CornerRadiusPercent { get; set; }
            public float PaddingPercent { get; set; }
            public bool HasDropShadow { get; set; }
        }

        public async Task ProcessAsync(ProcessingOptions options, Action<string, double> onProgress)
        {
            await Task.Run(() =>
            {
                onProgress("Инициализация генератора...", 0.0);

                if (!File.Exists(options.InputPath))
                    throw new FileNotFoundException("Исходный файл не найден", options.InputPath);

                // Create destination directories
                Directory.CreateDirectory(options.OutputPath);

                onProgress("Загрузка исходного изображения...", 0.05);
                using SKBitmap rawBitmap = LoadBaseBitmap(options.InputPath);
                using SKBitmap baseBitmap = ApplyFiltersAndStyling(
                    rawBitmap,
                    options.Brightness,
                    options.Contrast,
                    options.IsGrayscale,
                    options.IsInverted,
                    options.TintColorHex,
                    options.CornerRadiusPercent,
                    options.PaddingPercent,
                    options.HasDropShadow);

                // Parse background color for Android
                SKColor androidBgColor = SKColors.White;
                if (!string.IsNullOrWhiteSpace(options.AndroidBgColorHex))
                {
                    try
                    {
                        androidBgColor = SKColor.Parse(options.AndroidBgColorHex);
                    }
                    catch
                    {
                        androidBgColor = SKColors.White;
                    }
                }

                // Load background image if specified
                SKBitmap? androidBgImage = null;
                if (options.GenerateAndroidAdaptive && !string.IsNullOrEmpty(options.AndroidBgImagePath) && File.Exists(options.AndroidBgImagePath))
                {
                    try
                    {
                        androidBgImage = SKBitmap.Decode(options.AndroidBgImagePath);
                    }
                    catch
                    {
                        // Fallback to solid color
                    }
                }

                double progress = 0.1;

                // --- 1. Generate Windows ICO ---
                if (options.GenerateWindowsIco)
                {
                    onProgress("Генерация Windows .ico (сборка контейнера)...", progress);

                    var icoSizes = new[] { 16, 24, 32, 48, 64, 128, 256 };
                    var frames = new List<IcoEncoder.IcoFrame>();

                    foreach (var size in icoSizes)
                    {
                        // Apply sharpening to sizes 16 to 48 for high contrast in Explorer
                        bool applySharpening = (size >= 16 && size <= 48);
                        using var resized = ResizeBitmap(baseBitmap, size, size, applySharpening);
                        byte[] pngBytes = EncodeToPng(resized);
                        
                        frames.Add(new IcoEncoder.IcoFrame
                        {
                            PngData = pngBytes,
                            Width = size,
                            Height = size
                        });
                    }

                    string winDir = Path.Combine(options.OutputPath, "Windows");
                    Directory.CreateDirectory(winDir);
                    string icoPath = Path.Combine(winDir, "app_icon.ico");

                    using var fs = File.OpenWrite(icoPath);
                    // Truncate file in case it exists
                    fs.SetLength(0);
                    IcoEncoder.Encode(frames, fs);

                    progress += 0.3;
                    onProgress($"Создан мультиформатный файл: Windows\\app_icon.ico", progress);
                }

                // --- 2. Generate Windows Square Assets ---
                if (options.GenerateWindowsAssets)
                {
                    onProgress("Генерация Windows Assets (UWP/WinUI PNGs)...", progress);

                    string assetsDir = Path.Combine(options.OutputPath, "Windows", "Assets");
                    Directory.CreateDirectory(assetsDir);

                    // Setup names and their base sizes
                    var assets = new List<(string Name, int BaseSize)>
                    {
                        ("Square44x44Logo", 44),
                        ("Square150x150Logo", 150),
                        ("StoreLogo", 50)
                    };

                    var scales = new[] 
                    { 
                        (Scale: 100, Factor: 1.0),
                        (Scale: 125, Factor: 1.25),
                        (Scale: 150, Factor: 1.5),
                        (Scale: 200, Factor: 2.0),
                        (Scale: 400, Factor: 4.0)
                    };

                    int totalSteps = assets.Count * scales.Length;
                    int currentStep = 0;

                    foreach (var asset in assets)
                    {
                        foreach (var scale in scales)
                        {
                            currentStep++;
                            int targetSize = (int)Math.Round(asset.BaseSize * scale.Factor);
                            
                            // Adjust size slightly if it's odd to prevent layout issues
                            if (targetSize % 2 != 0 && scale.Scale != 125 && scale.Scale != 150)
                            {
                                // e.g. for StoreLogo scale 125, 50 * 1.25 = 62.5 -> 63 is fine.
                            }

                            using var resized = ResizeBitmap(baseBitmap, targetSize, targetSize, false);
                            byte[] pngBytes = EncodeToPng(resized);

                            string fileName = $"{asset.Name}.scale-{scale.Scale}.png";
                            string filePath = Path.Combine(assetsDir, fileName);
                            File.WriteAllBytes(filePath, pngBytes);

                            double subProgress = progress + (0.3 * ((double)currentStep / totalSteps));
                            onProgress($"Создан Windows Asset: {fileName} ({targetSize}x{targetSize}px)", subProgress);
                        }
                    }

                    progress += 0.3;
                }

                // --- 3. Generate Android Adaptive Icons ---
                if (options.GenerateAndroidAdaptive)
                {
                    onProgress("Генерация Android Adaptive Icons...", progress);

                    string androidDir = Path.Combine(options.OutputPath, "Android");
                    string resDir = Path.Combine(androidDir, "res");
                    Directory.CreateDirectory(androidDir);
                    Directory.CreateDirectory(resDir);

                    // Generate Play Store 512x512
                    using (var playStoreIcon = CreatePlayStoreIcon(baseBitmap, androidBgColor, androidBgImage))
                    {
                        byte[] playStoreBytes = EncodeToPng(playStoreIcon);
                        File.WriteAllBytes(Path.Combine(androidDir, "play_store_512.png"), playStoreBytes);
                    }
                    onProgress("Создана обложка Play Store: play_store_512.png", progress + 0.05);

                    // Slicing densities
                    // Density Name, Adaptive size (108dp base), Legacy size (48dp base)
                    var densities = new[]
                    {
                        (Name: "mipmap-mdpi", AdaptiveSize: 108, LegacySize: 48),
                        (Name: "mipmap-hdpi", AdaptiveSize: 162, LegacySize: 72),
                        (Name: "mipmap-xhdpi", AdaptiveSize: 216, LegacySize: 96),
                        (Name: "mipmap-xxhdpi", AdaptiveSize: 324, LegacySize: 144),
                        (Name: "mipmap-xxxhdpi", AdaptiveSize: 432, LegacySize: 192)
                    };

                    int totalDensities = densities.Length;
                    int currentDensityIdx = 0;

                    foreach (var d in densities)
                    {
                        currentDensityIdx++;
                        string mipmapDir = Path.Combine(resDir, d.Name);
                        Directory.CreateDirectory(mipmapDir);

                        // A. Foreground (safe-zone centered)
                        using (var fg = CreateAndroidForeground(baseBitmap, d.AdaptiveSize))
                        {
                            byte[] fgBytes = EncodeToPng(fg);
                            File.WriteAllBytes(Path.Combine(mipmapDir, "ic_launcher_foreground.png"), fgBytes);
                        }

                        // B. Background (solid or image)
                        using (var bg = CreateAndroidBackground(d.AdaptiveSize, androidBgColor, androidBgImage))
                        {
                            byte[] bgBytes = EncodeToPng(bg);
                            File.WriteAllBytes(Path.Combine(mipmapDir, "ic_launcher_background.png"), bgBytes);
                        }

                        // C. Legacy Launcher (Circular clipped composite)
                        using (var legacy = CreateAndroidLegacyLauncher(baseBitmap, d.LegacySize, androidBgColor, androidBgImage))
                        {
                            byte[] legacyBytes = EncodeToPng(legacy);
                            File.WriteAllBytes(Path.Combine(mipmapDir, "ic_launcher.png"), legacyBytes);
                        }

                        double subProgress = progress + 0.05 + (0.25 * ((double)currentDensityIdx / totalDensities));
                        onProgress($"Создан пакет Android mipmap ({d.Name}): ic_launcher.png", subProgress);
                    }
                }

                // --- 4. Generate Web & Favicon Package ---
                if (options.GenerateFaviconPackage)
                {
                    onProgress("Генерация Web & Favicon Pack...", 0.85);
                    GenerateFaviconPackage(baseBitmap, options.OutputPath);
                }

                // --- 5. Generate macOS .icns ---
                if (options.GenerateMacIcns)
                {
                    onProgress("Генерация macOS .icns пакета...", 0.92);
                    GenerateMacIcns(baseBitmap, options.OutputPath);
                }

                androidBgImage?.Dispose();
                onProgress("Генерация успешно завершена!", 1.0);
            });
        }

        public static SKBitmap LoadBaseBitmap(string inputPath)
        {
            string ext = Path.GetExtension(inputPath).ToLowerInvariant();
            if (ext == ".svg")
            {
                var svg = new SKSvg();
                svg.Load(inputPath);
                if (svg.Picture == null) 
                    throw new InvalidDataException("Не удалось прочитать векторный SVG файл");
                
                var cull = svg.Picture.CullRect;
                if (cull.Width <= 0 || cull.Height <= 0)
                {
                    cull = new SKRect(0, 0, 100, 100);
                }

                var baseBitmap = new SKBitmap(1024, 1024);
                using (var canvas = new SKCanvas(baseBitmap))
                {
                    canvas.Clear(SKColors.Transparent);
                    float scale = Math.Min(1024f / cull.Width, 1024f / cull.Height);
                    float dx = (1024f - cull.Width * scale) / 2f;
                    float dy = (1024f - cull.Height * scale) / 2f;
                    
                    canvas.Translate(dx, dy);
                    canvas.Scale(scale);
                    canvas.DrawPicture(svg.Picture);
                }
                return baseBitmap;
            }
            else
            {
                using var codec = SKCodec.Create(inputPath);
                if (codec == null) 
                    throw new InvalidDataException("Не удалось прочитать растровое изображение (неподдерживаемый формат)");
                
                var original = SKBitmap.Decode(codec);
                if (original.Width == 1024 && original.Height == 1024)
                {
                    return original;
                }
                
                var baseBitmap = new SKBitmap(1024, 1024);
                using (var canvas = new SKCanvas(baseBitmap))
                {
                    canvas.Clear(SKColors.Transparent);
                    var rect = new SKRect(0, 0, 1024, 1024);
                    using var paint = new SKPaint 
                    { 
                        FilterQuality = SKFilterQuality.High,
                        IsAntialias = true 
                    };
                    canvas.DrawBitmap(original, rect, paint);
                }
                original.Dispose();
                return baseBitmap;
            }
        }

        public static SKBitmap ResizeBitmap(SKBitmap source, int width, int height, bool applySharpening)
        {
            SKBitmap current = source;
            bool isTemporary = false;

            using var highPaint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };

            // Progressive multi-octave downscaling (1024 -> 512 -> 256 -> 128 -> 64 -> 32 -> 16)
            while (current.Width >= width * 2 && current.Height >= height * 2)
            {
                int nextW = current.Width / 2;
                int nextH = current.Height / 2;
                var stepBmp = new SKBitmap(nextW, nextH);
                using (var canvas = new SKCanvas(stepBmp))
                {
                    canvas.Clear(SKColors.Transparent);
                    var srcRect = new SKRect(0, 0, current.Width, current.Height);
                    var dstRect = new SKRect(0, 0, nextW, nextH);
                    canvas.DrawBitmap(current, srcRect, dstRect, highPaint);
                }
                if (isTemporary)
                {
                    current.Dispose();
                }
                current = stepBmp;
                isTemporary = true;
            }

            var finalBmp = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(finalBmp))
            {
                canvas.Clear(SKColors.Transparent);
                var srcRect = new SKRect(0, 0, current.Width, current.Height);
                var dstRect = new SKRect(0, 0, width, height);
                canvas.DrawBitmap(current, srcRect, dstRect, highPaint);

                if (applySharpening && width <= 48)
                {
                    using var temp = new SKBitmap(finalBmp.Width, finalBmp.Height);
                    using (var tempCanvas = new SKCanvas(temp))
                    {
                        tempCanvas.Clear(SKColors.Transparent);
                        tempCanvas.DrawBitmap(finalBmp, 0, 0);
                    }
                    canvas.Clear(SKColors.Transparent);
                    
                    float[] kernel = {
                        0, -0.08f, 0,
                        -0.08f, 1.32f, -0.08f,
                        0, -0.08f, 0
                    };
                    
                    var kernelSize = new SKSizeI(3, 3);
                    var kernelOffset = new SKPointI(1, 1);
                    
                    using var filter = SKImageFilter.CreateMatrixConvolution(
                        kernelSize, kernel, 1.0f, 0.0f, kernelOffset, SKShaderTileMode.Clamp, false);
                    
                    using var sharpenPaint = new SKPaint
                    {
                        ImageFilter = filter,
                        IsAntialias = true
                    };
                    
                    canvas.DrawBitmap(temp, 0, 0, sharpenPaint);
                }
            }

            if (isTemporary)
            {
                current.Dispose();
            }

            return finalBmp;
        }

        private static byte[] EncodeToPng(SKBitmap bitmap)
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        private static SKBitmap CreateAndroidForeground(SKBitmap baseBitmap, int densitySize)
        {
            var foreground = new SKBitmap(densitySize, densitySize);
            using (var canvas = new SKCanvas(foreground))
            {
                canvas.Clear(SKColors.Transparent);
                
                // Adaptive foreground inner safe-zone is 72/108 (approx 66.6%) of density size
                int innerSize = (int)Math.Round(densitySize * (72.0 / 108.0));
                int offset = (densitySize - innerSize) / 2;
                
                using var paint = new SKPaint
                {
                    FilterQuality = SKFilterQuality.High,
                    IsAntialias = true
                };
                
                var rect = new SKRect(offset, offset, offset + innerSize, offset + innerSize);
                canvas.DrawBitmap(baseBitmap, rect, paint);
            }
            return foreground;
        }

        private static SKBitmap CreateAndroidBackground(int densitySize, SKColor color, SKBitmap? backgroundImage)
        {
            var background = new SKBitmap(densitySize, densitySize);
            using (var canvas = new SKCanvas(background))
            {
                if (backgroundImage != null)
                {
                    canvas.Clear(SKColors.Transparent);
                    var rect = new SKRect(0, 0, densitySize, densitySize);
                    using var paint = new SKPaint
                    {
                        FilterQuality = SKFilterQuality.High,
                        IsAntialias = true
                    };
                    canvas.DrawBitmap(backgroundImage, rect, paint);
                }
                else
                {
                    canvas.Clear(color);
                }
            }
            return background;
        }

        private static SKBitmap CreateAndroidLegacyLauncher(SKBitmap baseBitmap, int legacySize, SKColor bgColor, SKBitmap? bgImage)
        {
            var launcher = new SKBitmap(legacySize, legacySize);
            using (var canvas = new SKCanvas(launcher))
            {
                canvas.Clear(SKColors.Transparent);
                
                // Circle clip path for standard circular legacy launcher icons
                using var path = new SKPath();
                float radius = legacySize / 2f;
                path.AddCircle(radius, radius, radius - 1f);
                canvas.ClipPath(path, antialias: true);
                
                if (bgImage != null)
                {
                    var rect = new SKRect(0, 0, legacySize, legacySize);
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
                    canvas.DrawBitmap(bgImage, rect, paint);
                }
                else
                {
                    canvas.Clear(bgColor);
                }
                
                // Foreground logo occupies about 65% of circular canvas
                int innerSize = (int)Math.Round(legacySize * 0.65);
                int offset = (legacySize - innerSize) / 2;
                var logoRect = new SKRect(offset, offset, offset + innerSize, offset + innerSize);
                
                using var logoPaint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
                canvas.DrawBitmap(baseBitmap, logoRect, logoPaint);
            }
            return launcher;
        }

        private static SKBitmap CreatePlayStoreIcon(SKBitmap baseBitmap, SKColor bgColor, SKBitmap? bgImage)
        {
            var playIcon = new SKBitmap(512, 512);
            using (var canvas = new SKCanvas(playIcon))
            {
                if (bgImage != null)
                {
                    var rect = new SKRect(0, 0, 512, 512);
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
                    canvas.DrawBitmap(bgImage, rect, paint);
                }
                else
                {
                    canvas.Clear(bgColor);
                }
                
                // Foreground logo occupies about 70% of canvas
                int innerSize = (int)Math.Round(512 * 0.70);
                int offset = (512 - innerSize) / 2;
                var logoRect = new SKRect(offset, offset, offset + innerSize, offset + innerSize);
                
                using var logoPaint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
                canvas.DrawBitmap(baseBitmap, logoRect, logoPaint);
            }
            return playIcon;
        }

        public static SKBitmap ApplyFiltersAndStyling(
            SKBitmap source,
            float brightness = 0,
            float contrast = 0,
            bool isGrayscale = false,
            bool isInverted = false,
            string? tintColorHex = null,
            float cornerRadiusPercent = 0,
            float paddingPercent = 0,
            bool hasDropShadow = false)
        {
            if (source == null) return new SKBitmap(256, 256);

            int width = source.Width;
            int height = source.Height;

            // 1. Create filtered base
            var filtered = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(filtered))
            {
                canvas.Clear(SKColors.Transparent);

                using var paint = new SKPaint { IsAntialias = true };
                var filtersList = new List<SKColorFilter>();

                if (brightness != 0 || contrast != 0)
                {
                    float c = (100f + contrast) / 100f;
                    float b = brightness / 255f; // SkiaSharp uses normalized 0..1 range for matrix offsets
                    float[] matrix = new float[]
                    {
                        c, 0, 0, 0, b,
                        0, c, 0, 0, b,
                        0, 0, c, 0, b,
                        0, 0, 0, 1, 0
                    };
                    filtersList.Add(SKColorFilter.CreateColorMatrix(matrix));
                }

                if (isGrayscale)
                {
                    filtersList.Add(SKColorFilter.CreateColorMatrix(new float[]
                    {
                        0.2126f, 0.7152f, 0.0722f, 0, 0,
                        0.2126f, 0.7152f, 0.0722f, 0, 0,
                        0.2126f, 0.7152f, 0.0722f, 0, 0,
                        0, 0, 0, 1, 0
                    }));
                }

                if (isInverted)
                {
                    filtersList.Add(SKColorFilter.CreateColorMatrix(new float[]
                    {
                        -1f,  0f,  0f, 0f, 1f,
                         0f, -1f,  0f, 0f, 1f,
                         0f,  0f, -1f, 0f, 1f,
                         0f,  0f,  0f, 1f, 0f
                    }));
                }

                if (!string.IsNullOrWhiteSpace(tintColorHex) && SKColor.TryParse(tintColorHex, out var tintColor) && tintColor.Alpha > 0)
                {
                    filtersList.Add(SKColorFilter.CreateBlendMode(tintColor, SKBlendMode.SrcIn));
                }

                if (filtersList.Count == 1)
                {
                    paint.ColorFilter = filtersList[0];
                }
                else if (filtersList.Count > 1)
                {
                    SKColorFilter current = filtersList[0];
                    for (int i = 1; i < filtersList.Count; i++)
                    {
                        current = SKColorFilter.CreateCompose(filtersList[i], current);
                    }
                    paint.ColorFilter = current;
                }

                canvas.DrawBitmap(source, new SKRect(0, 0, width, height), paint);
            }

            if (cornerRadiusPercent <= 0 && paddingPercent <= 0 && !hasDropShadow)
            {
                return filtered;
            }

            var finalBitmap = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(finalBitmap))
            {
                canvas.Clear(SKColors.Transparent);

                float pad = (Math.Min(width, height) * (paddingPercent / 100f)) / 2f;
                var drawRect = new SKRect(pad, pad, width - pad, height - pad);
                float rx = drawRect.Width * (cornerRadiusPercent / 100f);
                float ry = drawRect.Height * (cornerRadiusPercent / 100f);

                if (hasDropShadow)
                {
                    using var shadowPaint = new SKPaint
                    {
                        IsAntialias = true,
                        ImageFilter = SKImageFilter.CreateDropShadow(0, 8, 12, 12, new SKColor(0, 0, 0, 100))
                    };
                    using var rpath = new SKPath();
                    rpath.AddRoundRect(drawRect, rx, ry);
                    canvas.DrawPath(rpath, shadowPaint);
                }

                using var fillPaint = new SKPaint { IsAntialias = true };

                if (cornerRadiusPercent > 0)
                {
                    using var path = new SKPath();
                    path.AddRoundRect(drawRect, rx, ry);
                    canvas.ClipPath(path, antialias: true);
                }

                canvas.DrawBitmap(filtered, drawRect, fillPaint);
            }

            filtered.Dispose();
            return finalBitmap;
        }

        private static void GenerateMacIcns(SKBitmap baseBitmap, string outputPath)
        {
            var icnsTypes = new (string FourCC, int Size)[]
            {
                ("ic10", 1024),
                ("ic09", 512),
                ("ic08", 256),
                ("ic07", 128),
                ("ic06", 64),
                ("ic05", 32),
                ("ic04", 16)
            };

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write(new char[] { 'i', 'c', 'n', 's' });
            writer.Write((uint)0);

            foreach (var item in icnsTypes)
            {
                using var resized = ResizeBitmap(baseBitmap, item.Size, item.Size, item.Size <= 48);
                byte[] pngBytes = EncodeToPng(resized);

                foreach (char c in item.FourCC) writer.Write((byte)c);
                uint blockSize = (uint)(8 + pngBytes.Length);
                writer.Write(SwapEndian(blockSize));
                writer.Write(pngBytes);
            }

            uint totalLength = (uint)ms.Length;
            ms.Position = 4;
            writer.Write(SwapEndian(totalLength));

            string macDir = Path.Combine(outputPath, "macOS");
            Directory.CreateDirectory(macDir);
            File.WriteAllBytes(Path.Combine(macDir, "app_icon.icns"), ms.ToArray());
        }

        private static uint SwapEndian(uint value)
        {
            return ((value & 0x000000FF) << 24) |
                   ((value & 0x0000FF00) << 8) |
                   ((value & 0x00FF0000) >> 8) |
                   ((value & 0xFF000000) >> 24);
        }

        private static void GenerateFaviconPackage(SKBitmap baseBitmap, string outputPath)
        {
            string webDir = Path.Combine(outputPath, "WebFavicon");
            Directory.CreateDirectory(webDir);

            var icoSizes = new[] { 16, 32, 48 };
            var frames = new List<IcoEncoder.IcoFrame>();
            foreach (var size in icoSizes)
            {
                using var resized = ResizeBitmap(baseBitmap, size, size, true);
                frames.Add(new IcoEncoder.IcoFrame
                {
                    PngData = EncodeToPng(resized),
                    Width = size,
                    Height = size
                });
            }
            using (var fs = File.Create(Path.Combine(webDir, "favicon.ico")))
            {
                IcoEncoder.Encode(frames, fs);
            }

            var pngOutputs = new (string Filename, int Size)[]
            {
                ("favicon-16x16.png", 16),
                ("favicon-32x32.png", 32),
                ("apple-touch-icon.png", 180),
                ("android-chrome-192x192.png", 192),
                ("android-chrome-512x512.png", 512)
            };

            foreach (var item in pngOutputs)
            {
                using var resized = ResizeBitmap(baseBitmap, item.Size, item.Size, item.Size <= 48);
                File.WriteAllBytes(Path.Combine(webDir, item.Filename), EncodeToPng(resized));
            }

            string manifest = @"{
  ""name"": ""IconForge App"",
  ""short_name"": ""IconForge"",
  ""icons"": [
    {
      ""src"": ""/android-chrome-192x192.png"",
      ""sizes"": ""192x192"",
      ""type"": ""image/png""
    },
    {
      ""src"": ""/android-chrome-512x512.png"",
      ""sizes"": ""512x512"",
      ""type"": ""image/png""
    }
  ],
  ""theme_color"": ""#ffffff"",
  ""background_color"": ""#ffffff"",
  ""display"": ""standalone""
}";
            File.WriteAllText(Path.Combine(webDir, "site.webmanifest"), manifest);
        }

        public static List<(int Width, int Height, byte[] Data)> ExtractIcoFrames(string icoPath)
        {
            var results = new List<(int Width, int Height, byte[] Data)>();
            if (!File.Exists(icoPath)) return results;

            byte[] bytes = File.ReadAllBytes(icoPath);
            if (bytes.Length < 6) return results;

            ushort reserved = BitConverter.ToUInt16(bytes, 0);
            ushort type = BitConverter.ToUInt16(bytes, 2);
            ushort count = BitConverter.ToUInt16(bytes, 4);

            if (type != 1) return results;

            int offset = 6;
            for (int i = 0; i < count; i++)
            {
                if (offset + 16 > bytes.Length) break;

                byte width = bytes[offset];
                byte height = bytes[offset + 1];
                int realWidth = width == 0 ? 256 : width;
                int realHeight = height == 0 ? 256 : height;

                uint bytesInRes = BitConverter.ToUInt32(bytes, offset + 8);
                uint imageOffset = BitConverter.ToUInt32(bytes, offset + 12);

                if (imageOffset + bytesInRes <= bytes.Length)
                {
                    byte[] imageData = new byte[bytesInRes];
                    Array.Copy(bytes, (int)imageOffset, imageData, 0, (int)bytesInRes);

                    if (imageData.Length > 8 && imageData[0] == 0x89 && imageData[1] == 0x50 && imageData[2] == 0x4E && imageData[3] == 0x47)
                    {
                        results.Add((realWidth, realHeight, imageData));
                    }
                    else
                    {
                        try
                        {
                            using var bmp = SKBitmap.Decode(imageData);
                            if (bmp != null)
                            {
                                results.Add((realWidth, realHeight, EncodeToPng(bmp)));
                            }
                        }
                        catch { }
                    }
                }

                offset += 16;
            }

            return results;
        }
    }
}
