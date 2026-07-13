[ English ](../README.md) • [ Русский ](README_RU.md) • [ Deutsch ](README_DE.md)

# IconForge

*Нативный генератор иконок для Windows 11*

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-blue)](https://www.microsoft.com/windows)
[![Framework: .NET 8.0](https://img.shields.io/badge/Framework-.NET%208.0-blue)](https://dotnet.microsoft.com/download)
[![UI: WinUI 3](https://img.shields.io/badge/UI-WinUI%203-blue)](https://learn.microsoft.com/windows/apps/winui/winui3/)
[![Share](https://img.shields.io/twitter/url?style=social&url=https%3A%2F%2Fgithub.com%2FAlmanex%2Ficoboo)](https://twitter.com/intent/tweet?text=Check%20out%20IconForge%20-%20A%20native%20icon%20generator%20for%20Windows%2011!&url=https%3A%2F%2Fgithub.com%2FAlmanex%2Ficoboo)

IconForge — это легкое нативное Windows-приложение (утилита), разработанное на фреймворке WinUI 3 (Windows App SDK) и C#. Оно предназначено для пакетной генерации наборов иконок для Windows (`.ico`, `Assets`) и Android (`Adaptive Icons`) из одного исходного изображения формата PNG или SVG.

Подробное описание всех настроек и возможностей см. в [Руководстве пользователя](GUIDE_RU.md).

## Интерфейс приложения

<details open>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Показать ] 1. Главное окно - Drag and Drop файлов</b></summary>
  <br/>
  <p align="center"><img src="../Screenshots/screenshot1.png" width="95%" /></p>
</details>
<details>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Показать ] 2. Настройки и выбор папки назначения</b></summary>
  <br/>
  <p align="center"><img src="../Screenshots/screenshot2.png" width="95%" /></p>
</details>
<details>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Показать ] 3. Кастомизация адаптивных иконок Android и палитры цветов</b></summary>
  <br/>
  <p align="center"><img src="../Screenshots/screenshot3.png" width="95%" /></p>
</details>
<details>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Показать ] 4. Интеграция в систему - контекстное меню проводника Windows</b></summary>
  <br/>
  <p align="center"><img src="../Screenshots/screenshot4.png" width="95%" /></p>
</details>
<details>
  <summary style="cursor: pointer; padding: 6px; font-family: sans-serif;"><b>[ Показать ] 5. Активный процесс генерации и всплывающие уведомления</b></summary>
  <br/>
  <p align="center"><img src="../Screenshots/screenshot5.png" width="95%" /></p>
</details>

---

## Основные возможности

### Генерация пакетов иконок

* **Windows (Классический .ico):**
  * Сборка мультиформатного `.ico` файла, содержащего разрешения: `16x16`, `24x24`, `32x32`, `48x48`, `64x64`, `128x128`, `256x256` пикселей.
  * **Микро-шарпинг:** Для мелких размеров (16-48px) автоматически применяется специальный контурный фильтр резкости, чтобы иконка не размывалась в Проводнике Windows.
* **Windows Modern Assets (PNG):**
  * Экспорт отдельных изображений для манифеста современных Windows-приложений (`Square44x44Logo`, `Square150x150Logo`, `StoreLogo`) во всех системных масштабах: `scale-100`, `scale-125`, `scale-150`, `scale-200`, `scale-400`.
* **Android (Adaptive и Legacy Icons):**
  * Разделение слоев: логотип автоматически позиционируется внутри безопасной зоны (safe-zone 72dp) слоя `Foreground.png`, а слой `Background.png` заливается выбранным цветом или текстурным файлом.
  * Экспорт по структуре папок проекта (`mipmap-mdpi` до `mipmap-xxxhdpi`).
  * Генерация круглой Legacy-иконки (`ic_launcher.png`) путем маскирования и наложения слоев.
  * Экспорт промо-иконки для Google Play Console в размере `512x512` пикселей.

### Современный интерфейс Windows 11 (UI/UX)

* Использование системного полупрозрачного материала Mica Alt (адаптируется под обои рабочего стола).
* Полная поддержка системной Темной и Светлой темы Windows 11.
* Интерактивная зона Drag-and-Drop с динамическим изменением цвета границ и встроенным превью для файлов PNG/SVG.
* Быстрые палитры (свотчи) для выбора фонового цвета Android-иконки.

### Системная интеграция (Shell Integration)

* **Контекстное меню Проводника:** Опция встраивания пункта "Сгенерировать иконки в IconForge" прямо в меню Проводника Windows при правом клике на PNG/SVG. Регистрация происходит локально в кусте `HKEY_CURRENT_USER` и не требует прав администратора (UAC).
* **Всплывающие уведомления (Toast):** По окончании работы приложение отправляет нативное всплывающее уведомление Windows 11 с интерактивной кнопкой "Открыть папку".

---

## Стек технологий

| Компонент / Слой | Технология | Описание / Назначение |
| --- | --- | --- |
| Язык | C# (.NET 8.0) | net8.0-windows целевой фреймворк |
| UI-платформа | WinUI 3 | Windows App SDK 2.2.0 |
| Графика | SkiaSharp | Lanczos3-ресайз и фильтрация |
| Векторная графика | Svg.Skia | Отрисовка векторной графики в растр |
| Тип приложения | Unpackaged Self-Contained | Запуск без глобальной установки Windows App Runtime |

---

## Структура экспорта файлов

После генерации в выбранной папке создается следующая структура каталогов:

```text
[Папка_Назначения]/
├── Windows/
│   ├── app_icon.ico
│   └── Assets/
│       ├── Square44x44Logo.scale-100.png
│       ├── Square44x44Logo.scale-200.png
│       └── ... (все логотипы во всех масштабах)
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

## Как собрать и запустить

### Требования

* [.NET 8.0 SDK](https://dotnet.microsoft.com/download) или новее.

### Сборка и запуск из консоли

1. Клонируйте репозиторий:
   ```powershell
   git clone https://github.com/Almanex/icoboo.git
   cd icoboo
   ```
2. Скомпилируйте проект:
   ```powershell
   dotnet build
   ```
3. Запустите приложение:
   ```powershell
   dotnet run
   ```

### Публикация (Self-Contained EXE с ресурсами)

Для генерации единого исполняемого пакета:
```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```
Эта компиляция объединяет сборки в один исполняемый файл `IconForge.exe` и копирует папку `Assets/` рядом с ним в каталог `publish/`.

> [!IMPORTANT]
> Папка `Assets/` **обязательно** должна находиться в одной директории с `IconForge.exe`, чтобы приложение могло загрузить графические ресурсы интерфейса и успешно запуститься. При распространении программы упаковывайте исполняемый файл и папку `Assets/` вместе (например, в ZIP-архив).

---

## Участие в разработке

Будем рады вашему участию! Создавайте issue или присылайте pull request, если хотите предложить улучшения.

---

## Версионирование

Мы используем [SemVer](https://semver.org/) для версионирования. Доступные версии можно посмотреть по тегам в этом репозитории.

---

## Авторы

* **Almanex** - *Начальная разработка* - [Профиль Almanex](https://github.com/Almanex)

---

## Лицензия

Этот проект лицензирован по лицензии MIT - подробности см. в файле `LICENSE`.
