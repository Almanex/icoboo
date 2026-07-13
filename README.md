[ English ](README.md) • [ Русский ](docs/README_RU.md) • [ Deutsch ](docs/README_DE.md)

# IconForge

*Native icon generator for Windows 11*

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-blue)](https://www.microsoft.com/windows)
[![Framework: .NET 8.0](https://img.shields.io/badge/Framework-.NET%208.0-blue)](https://dotnet.microsoft.com/download)
[![UI: WinUI 3](https://img.shields.io/badge/UI-WinUI%203-blue)](https://learn.microsoft.com/windows/apps/winui/winui3/)
[![Share](https://img.shields.io/twitter/url?style=social&url=https%3A%2F%2Fgithub.com%2FAlmanex%2Ficoboo)](https://twitter.com/intent/tweet?text=Check%20out%20IconForge%20-%20A%20native%20icon%20generator%20for%20Windows%2011!&url=https%3A%2F%2Fgithub.com%2FAlmanex%2Ficoboo)

IconForge is a lightweight native Windows application developed on the WinUI 3 (Windows App SDK) framework and C#. It is designed for batch generation of icon sets for Windows (`.ico`, `Assets`) and Android (`Adaptive Icons`) from a single source image in PNG or SVG format.

For a detailed walkthrough of all options and features, see the [User Guide](docs/GUIDE.md).

## Application Interface

<details open>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Show ] 1. Main Window - File Drag and Drop</b></summary>
  <br/>
  <p align="center"><img src="Screenshots/screenshot1.png" width="95%" /></p>
</details>
<details>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Show ] 2. Settings and Target Directory Selection</b></summary>
  <br/>
  <p align="center"><img src="Screenshots/screenshot2.png" width="95%" /></p>
</details>
<details>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Show ] 3. Android Adaptive Icon Configuration and Color Swatches</b></summary>
  <br/>
  <p align="center"><img src="Screenshots/screenshot3.png" width="95%" /></p>
</details>
<details>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Show ] 4. Shell Integration - Windows Explorer Context Menu</b></summary>
  <br/>
  <p align="center"><img src="Screenshots/screenshot4.png" width="95%" /></p>
</details>
<details>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Show ] 5. Active Process State and Toast Notifications</b></summary>
  <br/>
  <p align="center"><img src="Screenshots/screenshot5.png" width="95%" /></p>
</details>

---

## Main Features

### Generating Icon Packs

* **Windows (Classic .ico):**
  * Build a multi-format `.ico` file containing resolutions: `16x16`, `24x24`, `32x32`, `48x48`, `64x64`, `128x128`, `256x256` pixels.
  * **Micro-sharpening:** For small sizes (16-48px), a special contour sharpening filter is automatically applied to prevent blurriness in Windows Explorer.
* **Windows Modern Assets (PNG):**
  * Export individual images for the manifest of modern Windows applications (`Square44x44Logo`, `Square150x150Logo`, `StoreLogo`) at all system scales: `scale-100`, `scale-125`, `scale-150`, `scale-200`, `scale-400`.
* **Android (Adaptive and Legacy Icons):**
  * Layer separation: the logo is automatically positioned inside the safe-zone (72dp) of the `Foreground.png` layer, and the `Background.png` layer is filled with the selected color or a texture file.
  * Export by project folder structure (`mipmap-mdpi` to `mipmap-xxxhdpi`).
  * Generate a round Legacy icon (`ic_launcher.png`) by masking and layering.
  * Export promo icon for Google Play Console in size `512x512` pixels.

### Modern Windows 11 Interface (UI/UX)

* Translucent Mica Alt system material (adapts to desktop wallpaper).
* Full support for Windows 11 system Dark and Light themes.
* Interactive Drag-and-Drop zone with dynamic border color changes and built-in preview for PNG/SVG files.
* Quick color swatches for choosing the background color of Android adaptive icons.

### System Integration (Shell Integration)

* **Explorer context menu:** Option to embed the "Generate icons in IconForge" item directly into the Windows Explorer menu when right-clicking a PNG/SVG file. Registration occurs locally in the `HKEY_CURRENT_USER` hive and does not require administrator rights (UAC).
* **Toast Notifications:** When processing is complete, the app sends a native Windows 11 toast notification with an interactive button to open the destination folder.

---

## Tech Stack

| Layer / Component | Technology | Details / Purpose |
| --- | --- | --- |
| Language | C# (.NET 8.0) | net8.0-windows target framework |
| UI Platform | WinUI 3 | Windows App SDK 2.2.0 |
| Graphics Rendering | SkiaSharp | Lanczos3 resize and filtering |
| SVG Rendering | Svg.Skia | Rendering vector graphics to raster |
| Packaging Type | Unpackaged Self-Contained | Runs without global Windows App Runtime installation |

---

## File Export Structure

After generation, the following directory structure is created in the selected destination folder:

```text
[Destination_Folder]/
├── Windows/
│   ├── app_icon.ico
│   └── Assets/
│       ├── Square44x44Logo.scale-100.png
│       ├── Square44x44Logo.scale-200.png
│       └── ... (all assets in all scales)
└── Android/
    ├── play_store_512.png
    └── res/
        ├── mipmap-mdpi/
        │   ├── ic_launcher.png
        │   ├── ic_launcher_background.png
        │   └── ic_launcher_foreground.png
        └── mipmap-xxxhdpi/ ...
```

---

## Getting Started

### Prerequisites

* [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later.

### Building and Running from Console

1. Clone the repository:
   ```powershell
   git clone https://github.com/Almanex/icoboo.git
   cd icoboo
   ```
2. Compile the project:
   ```powershell
   dotnet build
   ```
3. Launch the application:
   ```powershell
   dotnet run
   ```

### Publishing (Self-Contained EXE with Assets)

To generate a single executable package:
```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```
This compilation merges assemblies into a single executable `IconForge.exe` and copies the `Assets/` folder alongside it inside the `publish/` directory.

> [!IMPORTANT]
> The `Assets/` folder **must** be kept in the same directory as `IconForge.exe` for the application to load UI assets and start successfully. When distributing the app, package both the executable and the `Assets/` directory together (e.g., in a ZIP archive).

---

## Contributing

We welcome contributions! Please open an issue or submit a pull request if you want to improve the application.

---

## Versioning

We use [SemVer](https://semver.org/) for versioning. For the versions available, see the tags on this repository.

---

## Authors

* **Almanex** - *Initial development* - [Almanex Profile](https://github.com/Almanex)

---

## License

This project is licensed under the MIT License - see the `LICENSE` file for details.