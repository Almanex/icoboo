[ English ](GUIDE.md) • [ Русский ](GUIDE_RU.md) • [ Deutsch ](GUIDE_DE.md)

# IconForge User Guide

Welcome to the **IconForge** user guide. This document provides step-by-step instructions and technical details on how to get the most out of the application.

---

<p align="center">
  <img src="https://raw.githubusercontent.com/Almanex/icoboo/main/Screenshots/cover.png" alt="IconForge main" width="49%" />
  <img src="https://raw.githubusercontent.com/Almanex/icoboo/main/Screenshots/editor.png" alt="IconForge settings" width="49%" />
</p>

---

## 1. Quick Start Workflow

IconForge is designed to be straightforward and fast:

1. **Select Source File:** Drag and drop a PNG or SVG image into the dashed drop zone, or click **"Browse file on disk..."** to open a file selection dialog.
2. **Choose Destination:** Specify where you want the generated icons to be saved by entering a path or clicking the folder browser icon.
3. **Configure Settings:** Choose your preferred export options (Windows `.ico`, modern Assets, or Android Adaptive layouts).
4. **Generate:** Click **"Generate icons"**. A notification will appear once the process is complete, and you can click the notification or open the target folder to view your assets.

> [!IMPORTANT]
> **Executable Dependency:** The `IconForge.exe` single-file executable relies on the `Assets/` directory containing the app's internal icons and assets to load its UI correctly. Always ensure the `Assets/` folder is present in the same directory as `IconForge.exe` when running it. If you download a release ZIP archive, make sure to extract all contents before running the program.

---

## 2. Icon Formats and Technical Specifications

### Windows Classic .ico Generation

The Classic Windows Icon format (`.ico`) is a container that bundles multiple resolutions into a single file. This is crucial for keeping icons crisp at different Explorer view options (e.g. details, list, tiles, medium, large, and extra-large icons).

* **Bundled Resolutions:** `16x16`, `24x24`, `32x32`, `48x48`, `64x64`, `128x128`, `256x256` pixels.
* **Micro-sharpening Filter:** To prevent small resolutions (`16x16` up to `48x48` pixels) from looking blurry or muddy in the Windows Explorer shell, IconForge automatically applies a custom contour sharpening filter after scaling using the SkiaSharp Lanczos3 algorithm.

### Windows Modern Assets

Modern Windows Apps (UWP and WinUI 3 packaged applications) use separate PNG files declared in their package manifest (`Package.appxmanifest`). These assets are scaled depending on the user's monitor DPI settings.

* **Asset Templates:** `Square44x44Logo`, `Square150x150Logo`, `StoreLogo`.
* **Scales Generated:** `scale-100` (100%), `scale-125` (125%), `scale-150` (150%), `scale-200` (200%), and `scale-400` (400%).

### Android Adaptive & Legacy Icons

Android 8.0 (API level 26) introduced **Adaptive Icons**, which can display a variety of shapes across different device models (circles, squircles, rounded rectangles). To support this, Android requires icons to consist of separate foreground and background layers.

* **Foreground Layer (`ic_launcher_foreground.png`):**
  * The source image is automatically scaled down and centered inside a safe zone.
  * **Safe Zone Rule:** To prevent the logo from being cropped by different device masks, the core icon must reside within a central 72dp circle of the 108dp total canvas size. IconForge handles this positioning automatically.
* **Background Layer (`ic_launcher_background.png`):**
  * You can select a background color using a Hex code (e.g. `#FFFFFF` or `#3DDC84`), pick a predefined color from the quick swatches, or select a custom background image file (such as a pattern or texture).
* **Densities Supported:** Folder structure ranges from `mipmap-mdpi` up to `mipmap-xxxhdpi`.
* **Legacy Icon (`ic_launcher.png`):**
  * For older Android versions, a standard round legacy icon is automatically compiled by combining the foreground and background layers and applying a circular mask.
* **Google Play Console Promo Icon:**
  * Generates a high-quality `512x512` pixel PNG representation of the final icon, ready to upload to the developer console.

---

## 3. Shell Integration (Explorer Context Menu)

IconForge lets you register a shortcut directly in the Windows Explorer context menu so you can right-click any image and generate icons immediately.

### Registry Location and Privileges

* **Registry Path:** Keys are added under:
  * `HKEY_CURRENT_USER\Software\Classes\SystemFileAssociations\.png\Shell\IconForge`
  * `HKEY_CURRENT_USER\Software\Classes\SystemFileAssociations\.svg\Shell\IconForge`
* **No Administrator Rights Required:** Because the application writes to the user-specific registry hive (`HKEY_CURRENT_USER`) instead of the system-wide machine hive (`HKEY_LOCAL_MACHINE`), **you do not need administrator privileges (UAC prompt)** to toggle this feature. It is entirely sandboxed to the current Windows user profile.

---

## 4. Troubleshooting Windows Defender SmartScreen

If you compile IconForge from source or download an unsigned binary release, Windows Defender SmartScreen might block it on first launch, showing a warning: *"Windows Defender SmartScreen prevented an unrecognized app from starting."*

### Why does this happen?
This warning is standard for free, open-source software that does not have a paid digital code-signing certificate (which cost several hundred dollars annually). It does not mean the application is unsafe.

### How to bypass the warning:
1. In the SmartScreen window, click on the **"More info"** link.
2. The publisher name will show as *Unknown Publisher*.
3. Click the **"Run anyway"** button that appears at the bottom.
4. The application will launch normally and will not show the warning again.
