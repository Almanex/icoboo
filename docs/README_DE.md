[ English ](../README.md) • [ Русский ](README_RU.md) • [ Deutsch ](README_DE.md)

# IconForge

*Nativer Icon-Generator für Windows 11*

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-blue)](https://www.microsoft.com/windows)
[![Framework: .NET 8.0](https://img.shields.io/badge/Framework-.NET%208.0-blue)](https://dotnet.microsoft.com/download)
[![UI: WinUI 3](https://img.shields.io/badge/UI-WinUI%203-blue)](https://learn.microsoft.com/windows/apps/winui/winui3/)
[![Share](https://img.shields.io/twitter/url?style=social&url=https%3A%2F%2Fgithub.com%2FAlmanex%2Ficoboo)](https://twitter.com/intent/tweet?text=Check%20out%20IconForge%20-%20A%20native%20icon%20generator%20for%20Windows%2011!&url=https%3A%2F%2Fgithub.com%2FAlmanex%2Ficoboo)

<p align="center">
  <img src="https://raw.githubusercontent.com/Almanex/icoboo/main/Screenshots/cover.png" alt="IconForge main" width="49%" />
  <img src="https://raw.githubusercontent.com/Almanex/icoboo/main/Screenshots/editor.png" alt="IconForge settings" width="49%" />
</p>

---

IconForge ist eine schlanke native Windows-Anwendung, die auf dem WinUI 3 (Windows App SDK)-Framework und C# entwickelt wurde und standardmäßig 3 Sprachen (Englisch, Russisch, Deutsch) unterstützt. Sie ist für die Batch-Generierung von Symbolsätzen für Windows (`.ico`, `Assets`) und Android (`Adaptive Icons`) aus einem einzigen Quellbild im PNG- oder SVG-Format konzipiert.

Eine detaillierte Beschreibung aller Einstellungen und Funktionen finden Sie im [Benutzerhandbuch](GUIDE_DE.md).

## Anwendungsschnittstelle

<details open>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Anzeigen ] 1. Hauptfenster - Datei-Drag-and-Drop</b></summary>
  <br/>
  <p align="center"><img src="https://raw.githubusercontent.com/Almanex/icoboo/main/Screenshots/screenshot1.png" width="95%" /></p>
</details>
<details>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Anzeigen ] 2. Einstellungen und Auswahl des Zielordners</b></summary>
  <br/>
  <p align="center"><img src="https://raw.githubusercontent.com/Almanex/icoboo/main/Screenshots/screenshot2.png" width="95%" /></p>
</details>
<details>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Anzeigen ] 3. Anpassung von Android Adaptive Icons und Farbpaletten</b></summary>
  <br/>
  <p align="center"><img src="https://raw.githubusercontent.com/Almanex/icoboo/main/Screenshots/screenshot3.png" width="95%" /></p>
</details>
<details>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Anzeigen ] 4. Systemintegration - Windows Explorer-Kontextmenü</b></summary>
  <br/>
  <p align="center"><img src="https://raw.githubusercontent.com/Almanex/icoboo/main/Screenshots/screenshot4.png" width="95%" /></p>
</details>
<details>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Anzeigen ] 5. Aktiver Prozesszustand und Toast-Benachrichtigungen</b></summary>
  <br/>
  <p align="center"><img src="https://raw.githubusercontent.com/Almanex/icoboo/main/Screenshots/screenshot5.png" width="95%" /></p>
</details>

---

## Hauptmerkmale

### Generierung von Icon-Paketen

* **Windows (Klassisches .ico):**
  * Erstellen Sie eine Multiformat-`.ico`-Datei mit den Auflösungen: `16x16`, `24x24`, `32x32`, `48x48`, `64x64`, `128x128`, `256x256` Pixel.
  * **Mikroschärfung:** Für kleine Auflösungen (16-48 Pixel) wird automatisch ein spezieller Konturschärfungsfilter angewendet, um Unschärfe im Windows Explorer zu verhindern.
* **Windows Modern Assets (PNG):**
  * Exportieren Sie einzelne Bilder für das Manifest moderner Windows-Anwendungen (`Square44x44Logo`, `Square150x150Logo`, `StoreLogo`) in allen Systemmaßstäben: `scale-100`, `scale-125`, `scale-150`, `scale-200`, `scale-400`.
* **Android (Adaptive und Legacy Icons):**
  * Ebenentrennung: Das Logo wird automatisch innerhalb der sicheren Zone (72dp) der `Foreground.png`-Ebene positioniert und die `Background.png`-Ebene wird mit der ausgewählten Farbe oder einer Texturdatei gefüllt.
  * Export nach Projektordnerstruktur (`mipmap-mdpi` bis `mipmap-xxxhdpi`).
  * Erzeugen Sie ein rundes Legacy-Symbol (`ic_launcher.png`) durch Maskierung und Überlagerung.
  * Exportieren Sie das Promo-Symbol für die Google Play Console in der Größe `512x512` Pixel.

### Moderne Windows 11-Oberfläche (UI/UX)

* Verwendung des systemtransparenten Materials Mica Alt (passt sich dem Desktop-Hintergrund an).
* Volle Unterstützung für das dunkle und helle Design von Windows 11.
* Interaktiver Drag-and-Drop-Bereich mit dynamischer Änderung der Rahmenfarbe und integrierter Vorschau für PNG- und SVG-Dateien.
* Schnellfarbfelder zur Auswahl der Hintergrundfarbe für adaptive Android-Symbole.

### Systemintegration (Shell-Integration)

* **Explorer-Kontextmenü:** Option zum direkten Einbetten des Elements "Symbole in IconForge generieren" in das Windows Explorer-Kontextmenü beim Rechtsklick auf eine PNG- oder SVG-Datei. Die Registrierung erfolgt lokal im Hive `HKEY_CURRENT_USER` und erfordert keine Administratorrechte (UAC).
* **Toast-Benachrichtigungen:** Nach Abschluss der Verarbeitung sendet die App eine native Windows 11-Toast-Benachrichtigung mit einer interaktiven Schaltfläche zum Öffnen des Zielordners.

---

## Technologie-Stack

| Komponente / Schicht | Technologie | Details / Zweck |
| --- | --- | --- |
| Sprache | C# (.NET 8.0) | net8.0-windows Ziel-Framework |
| UI-Plattform | WinUI 3 | Windows App SDK 2.2.0 |
| Grafik-Rendering | SkiaSharp | Lanczos3-Größenänderung und Filterung |
| SVG-Rendering | Svg.Skia | Rendern von Vektorgrafiken in Raster |
| Verpackungsart | Unpackaged Self-Contained | Läuft ohne globale Installation der Windows App Runtime |

---

## Dateiexportstruktur

Nach der Generierung wird im ausgewählten Zielordner folgende Verzeichnisstruktur erstellt:

```text
[Zielordner]/
├── Windows/
│   ├── app_icon.ico
│   └── Assets/
│       ├── Square44x44Logo.scale-100.png
│       ├── Square44x44Logo.scale-200.png
│       └── ... (alle Assets in allen Skalierungen)
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

## Erste Schritte

### Anforderungen

* [.NET 8.0 SDK](https://dotnet.microsoft.com/download) oder neuer.

### Erstellen und Ausführen über die Konsole

1. Klonen Sie das Repository:
   ```powershell
   git clone https://github.com/Almanex/icoboo.git
   cd icoboo
   ```
2. Kompilieren Sie das Projekt:
   ```powershell
   dotnet build
   ```
3. Starten Sie die Anwendung:
   ```powershell
   dotnet run
   ```

### Veröffentlichen (eigenständige EXE mit Assets)

So generieren Sie ein einzelnes ausführbares Paket:
```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```
Diese Kompilierung führt die Assemblies in eine einzige ausführbare Datei `IconForge.exe` zusammen und kopiert den Ordner `Assets/` daneben in das Verzeichnis `publish/`.

> [!IMPORTANT]
> Der Ordner `Assets/` **muss** im selben Verzeichnis wie `IconForge.exe` aufbewahrt werden, damit die Anwendung UI-Assets laden und erfolgreich starten kann. Wenn Sie die App verteilen, packen Sie sowohl die ausführbare Datei als auch das `Assets/`-Verzeichnis zusammen (z. B. in ein ZIP-Archiv).

---

## Mitwirken

Beiträge sind herzlich willkommen! Bitte öffnen Sie ein Issue oder senden Sie einen Pull-Request, wenn Sie Verbesserungen vorschlagen möchten.

---

## Versionierung

Wir verwenden [SemVer](https://semver.org/) für die Versionierung. Die verfügbaren Versionen finden Sie unter den Tags in diesem Repository.

---

## Autoren

* **Almanex** - *Ursprüngliche Entwicklung* - [Almanex Profil](https://github.com/Almanex)

---

## Lizenz

Dieses Projekt ist unter der MIT-Lizenz lizenziert - siehe die Datei `LICENSE` für Details.
