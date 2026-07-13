[ English ](GUIDE.md) • [ Русский ](GUIDE_RU.md) • [ Deutsch ](GUIDE_DE.md)

# IconForge Benutzerhandbuch

Willkommen beim Benutzerhandbuch für **IconForge**. Dieses Dokument bietet Schritt-für-Schritt-Anleitungen und technische Details, um die Anwendung optimal zu nutzen.

---

## 1. Schnellstart-Workflow

IconForge ist so konzipiert, dass es einfach und schnell zu bedienen ist:

1. **Quelldatei auswählen:** Ziehen Sie ein PNG- oder SVG-Bild per Drag-and-Drop in den gestrichelten Ablagebereich oder klicken Sie auf **"Datei auf der Festplatte auswählen..."**, um den Dateiauswahldialog zu öffnen.
2. **Zielordner angeben:** Legen Sie fest, wo die generierten Symbole gespeichert werden sollen, indem Sie einen Pfad eingeben oder auf das Ordnersymbol klicken.
3. **Einstellungen konfigurieren:** Wählen Sie Ihre bevorzugten Exportoptionen aus (klassisches Windows `.ico`, moderne Windows Assets oder Android Adaptive Layouts).
4. **Generieren:** Klicken Sie auf **"Symbole generieren"**. Nach Abschluss des Vorgangs wird eine Benachrichtigung angezeigt. Sie können auf die Benachrichtigung klicken oder den Zielordner öffnen, um die Dateien anzuzeigen.

> [!IMPORTANT]
> **Ressourcenabhängigkeit:** Die ausführbare Datei `IconForge.exe` benötigt das Verzeichnis `Assets/` mit den internen Anwendungssymbolen im selben Ordner, um die Benutzeroberfläche korrekt zu laden. Stellen Sie vor dem Starten sicher, dass sich der Ordner `Assets/` im selben Verzeichnis wie `IconForge.exe` befindet. Wenn Sie das ZIP-Archiv des Releases heruntergeladen haben, entpacken Sie bitte alle Dateien vollständig vor der Verwendung.

---

## 2. Symbolformate und technische Spezifikationen

### Klassische Windows .ico-Generierung

Das klassische Windows-Symbolformat (`.ico`) ist ein Container, der mehrere Auflösungen in einer einzigen Datei zusammenfasst. Dies ist wichtig, damit Symbole bei verschiedenen Explorer-Ansichtsoptionen (z. B. Details, Liste, Kacheln, mittel, groß und extra groß) gestochen scharf bleiben.

* **Bündelauflösungen:** `16x16`, `24x24`, `32x32`, `48x48`, `64x64`, `128x128`, `256x256` Pixel.
* **Mikroschärfungsfilter:** Um zu verhindern, dass niedrige Auflösungen (von `16x16` bis `48x48` Pixel) im Windows Explorer verschwommen oder matschig wirken, wendet IconForge nach der Skalierung mit dem SkiaSharp Lanczos3-Algorithmus automatisch einen benutzerdefinierten Konturschärfungsfilter an.

### Moderne Windows-Assets

Moderne Windows-Apps (UWP- und WinUI 3-Anwendungen) verwenden separate PNG-Dateien, die in ihrem Paketmanifest (`Package.appxmanifest`) deklariert sind. Diese Assets werden je nach DPI-Einstellung des Monitors skaliert.

* **Asset-Vorlagen:** `Square44x44Logo`, `Square150x150Logo`, `StoreLogo`.
* **Generierte Skalierungen:** `scale-100` (100%), `scale-125` (125%), `scale-150` (150%), `scale-200` (200%) und `scale-400` (400%).

### Android Adaptive & Legacy-Symbole

Android 8.0 (API-Level 26) führte **Adaptive Symbole** (Adaptive Icons) ein, die auf verschiedenen Gerätemodellen unterschiedliche Formen anzeigen können (Kreise, abgerundete Quadrate usw.). Um dies zu unterstützen, verlangt Android, dass Symbole aus separaten Vordergrund- und Hintergrundebenen bestehen.

* **Vordergrundebene (`ic_launcher_foreground.png`):**
  * Das Quellbild wird automatisch verkleinert und innerhalb einer sicheren Zone zentriert.
  * **Sicherheitszonen-Regel:** Um zu verhindern, dass das Logo durch Gerätemasken abgeschnitten wird, muss sich das Kernsymbol innerhalb eines zentralen Kreises von 72dp bei einer Gesamtgröße von 108dp befinden. IconForge übernimmt diese Positionierung automatisch.
* **Hintergrundebene (`ic_launcher_background.png`):**
  * Sie können eine Hintergrundfarbe mithilfe eines Hex-Codes auswählen (z. B. `#FFFFFF` oder `#3DDC84`), eine vordefinierte Farbe aus den Schnellfarbfeldern wählen oder eine benutzerdefinierte Hintergrundbilddatei (z. B. ein Muster oder eine Textur) auswählen.
* **Unterstützte Dichten:** Die Ordnerstruktur reicht von `mipmap-mdpi` bis `mipmap-xxxhdpi`.
* **Legacy-Symbol (`ic_launcher.png`):**
  * Für ältere Android-Versionen wird automatisch ein rundes Standard-Legacy-Symbol erstellt, indem die Vorder- und Hintergrundebenen kombiniert und eine kreisförmige Maske angewendet werden.
* **Google Play Console Promo-Symbol:**
  * Generiert ein hochwertiges PNG-Bild mit `512x512` Pixeln, das in die Entwicklerkonsole hochgeladen werden kann.

---

## 3. Systemintegration (Explorer-Kontextmenü)

Mit IconForge können Sie eine Verknüpfung direkt im Windows Explorer-Kontextmenü registrieren, sodass Sie mit der rechten Maustaste auf ein beliebiges Bild klicken und sofort Symbole generieren können.

### Registrierungspfad und Berechtigungen

* **Registrierungspfad:** Die Schlüssel werden unter folgendem Pfad hinzugefügt:
  * `HKEY_CURRENT_USER\Software\Classes\SystemFileAssociations\.png\Shell\IconForge`
  * `HKEY_CURRENT_USER\Software\Classes\SystemFileAssociations\.svg\Shell\IconForge`
* **Keine Administratorrechte erforderlich:** Da die Anwendung in den benutzerspezifischen Registrierungs-Hive (`HKEY_CURRENT_USER`) statt in den systemweiten Hive (`HKEY_LOCAL_MACHINE`) schreibt, **benötigen Sie keine Administratorrechte (UAC-Eingabeaufforderung)**, um diese Funktion zu aktivieren oder zu deaktivieren. Sie ist vollständig auf das aktuelle Windows-Benutzerprofil beschränkt.

---

## 4. Problembehebung bei Windows Defender SmartScreen

Wenn Sie IconForge aus dem Quellcode kompilieren oder eine nicht signierte Binärdatei ausführen, blockiert Windows Defender SmartScreen die Ausführung möglicherweise beim ersten Start mit der Warnung: *"Der Computer wurde durch Windows Defender SmartScreen geschützt. Von Windows Defender SmartScreen wurde der Start einer unbekannten App verhindert."*

### Warum passiert das?
Diese Warnung ist Standard für kostenlose Open-Source-Software, die nicht über ein kostenpflichtiges digitales Codesignaturzertifikat verfügt (das jährlich mehrere hundert Dollar kostet). Dies bedeutet nicht, dass die Anwendung unsicher ist.

### So umgehen Sie die Warnung:
1. Klicken Sie im SmartScreen-Fenster auf den Link **"Weitere Informationen"** (oder **"More info"**).
2. Der Name des Herausgebers wird als *Unbekannter Herausgeber* angezeigt.
3. Klicken Sie auf die Schaltfläche **"Trotzdem ausführen"** (oder **"Run anyway"**), die unten im Fenster angezeigt wird.
4. Die Anwendung startet normal und die Warnung wird bei zukünftigen Starts nicht mehr angezeigt.
