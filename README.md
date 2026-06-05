# BOSS — Blackout Screensaver STN

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D4?logo=windows)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![Architecture](https://img.shields.io/badge/arch-x64-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

**BOSS** (**B**lack**o**ut **S**creensaver **STN**) is a Windows screensaver that covers all connected monitors with fullscreen black windows, sends a power-off command to the displays, and locks the workstation when you move the mouse or press a key.

> **[Русская версия](#boss--blackout-screensaver-stn-ru)** ниже / below

---

## Features

- Covers **all monitors simultaneously** — no gaps, no taskbar flicker
- Sends `WM_SYSCOMMAND SC_MONITORPOWER` to power off displays
- Exits on **any mouse movement** (5 px dead zone) or **keypress**
- Locks the workstation on exit via `LockWorkStation()`
- Full screensaver protocol: `/s` run · `/c` settings · `/p` preview
- Settings dialog: lock on exit toggle, DDC/CI toggle, power-off delay
- No administrator rights required
- Single self-contained `.exe` / `.scr` — no installer, no dependencies

---

## Requirements

- Windows 10 / 11 (x64)
- .NET 10 SDK — only for building from source ([download](https://dotnet.microsoft.com/download))

---

## Build from Source

```powershell
git clone https://github.com/blvckstn/BlackOutScreensaverStn.git
cd BlackOutScreensaverStn

# Debug build + tests
dotnet build
dotnet test

# Self-contained release (produces publish\PowerOffScreensaver.exe + .scr)
.\publish.ps1
```

---

## Installation as a Screensaver

1. Run `.\publish.ps1` — the output appears in `publish\`
2. **Right-click** `publish\PowerOffScreensaver.scr` → **Install**
   *(or copy it to `C:\Windows\System32\` from an elevated PowerShell)*
3. Open **Settings → Personalization → Lock screen → Screen saver settings**
4. Select **PowerOffScreensaver** from the drop-down and click **OK**

---

## Usage

| Command | Effect |
|---|---|
| `PowerOffScreensaver.exe /s` | Launch screensaver (all monitors black → power off) |
| `PowerOffScreensaver.exe /c` | Open settings dialog |
| `PowerOffScreensaver.exe /p 0` | Preview mode (exits immediately, code 0) |

---

## Settings

Open the settings dialog with `/c`:

| Setting | Default | Description |
|---|---|---|
| Lock workstation on exit | On | Calls `LockWorkStation()` when screensaver exits |
| Try DDC/CI power off | Off | Hardware monitor power-off via DDC/CI (experimental) |
| Power-off delay (ms) | 500 | Pause before sending the monitor power-off command |

Settings are stored in `%AppData%\PowerOffScreensaver\settings.json`.

---

## Troubleshooting

**Monitors don't turn off physically**  
`WM_SYSCOMMAND SC_MONITORPOWER` is a hint, not a hardware command — some GPU drivers ignore it. The black windows still appear (burn-in protection), the screen looks off to the user, and the workstation is locked on exit. Workarounds: enable DDC/CI in settings, or configure "Turn off display" in Windows Power Options.

**Screensaver doesn't appear in the list**  
Make sure `PowerOffScreensaver.scr` is in `C:\Windows\System32\` and reopen the Screen Saver Settings dialog.

**Antivirus blocks the file**  
BOSS uses Win32 low-level input hooks (mouse / keyboard detection only). Add an exclusion in your antivirus or check Windows Defender quarantine.

---

## Architecture

```
src/PowerOffScreensaver/
├── Program.cs                    Entry point — parses /s /c /p
├── ScreensaverArgs.cs            Argument parser
├── AppSettings.cs                Settings model
├── Host/
│   └── ScreensaverHost.cs        ApplicationContext — manages screensaver lifetime
├── Forms/
│   ├── BlackoutForm.cs           Fullscreen black window per monitor
│   └── SettingsForm.cs           Settings dialog
└── Services/                     All Win32 P/Invoke lives here only
    ├── MonitorPowerService.cs    SendMessage SC_MONITORPOWER
    ├── WorkstationLockService.cs LockWorkStation()
    ├── SettingsService.cs        JSON load / save
    └── NullDdcCiService.cs       DDC/CI stub (default)
```

---

---

# BOSS — Blackout Screensaver STN <sup>RU</sup>

![Platform](https://img.shields.io/badge/платформа-Windows%2010%2F11-0078D4?logo=windows)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)

**BOSS** (**B**lack**o**ut **S**creensaver **STN**) — хранитель экрана для Windows, который закрывает все подключённые мониторы чёрными окнами на весь экран, отправляет команду выключения питания дисплеев и блокирует рабочую станцию при первом движении мыши или нажатии клавиши.

---

## Возможности

- Закрывает **все мониторы одновременно** — без зазоров и мерцания панели задач
- Отправляет `WM_SYSCOMMAND SC_MONITORPOWER` для выключения мониторов
- Выход при любом **движении мыши** (мёртвая зона 5 пикселей) или **нажатии клавиши**
- Блокирует рабочую станцию при выходе через `LockWorkStation()`
- Полный протокол хранителя экрана: `/s` запуск · `/c` настройки · `/p` превью
- Диалог настроек: блокировка при выходе, DDC/CI, задержка перед выключением
- Не требует прав администратора
- Один самодостаточный `.exe` / `.scr` — без установщика и зависимостей

---

## Требования

- Windows 10 / 11 (x64)
- .NET 10 SDK — только для сборки из исходников ([скачать](https://dotnet.microsoft.com/download))

---

## Сборка из исходников

```powershell
git clone https://github.com/blvckstn/BlackOutScreensaverStn.git
cd BlackOutScreensaverStn

# Сборка и тесты
dotnet build
dotnet test

# Self-contained релиз (результат в publish\)
.\publish.ps1
```

---

## Установка хранителя экрана

1. Запустить `.\publish.ps1` — файлы появятся в папке `publish\`
2. **Правый клик** на `publish\PowerOffScreensaver.scr` → **Установить**
   *(или скопировать в `C:\Windows\System32\` из PowerShell с правами администратора)*
3. Открыть **Параметры → Персонализация → Экран блокировки → Параметры заставки**
4. Выбрать **PowerOffScreensaver** из списка и нажать **ОК**

---

## Использование

| Команда | Действие |
|---|---|
| `PowerOffScreensaver.exe /s` | Запустить хранитель экрана |
| `PowerOffScreensaver.exe /c` | Открыть настройки |
| `PowerOffScreensaver.exe /p 0` | Режим превью (завершается сразу, код 0) |

---

## Настройки

| Параметр | По умолчанию | Описание |
|---|---|---|
| Блокировать рабочую станцию при выходе | Вкл | Вызывает `LockWorkStation()` при выходе из хранителя |
| Попытаться использовать DDC/CI | Выкл | Аппаратное выключение мониторов (экспериментально) |
| Задержка перед отключением (мс) | 500 | Пауза перед отправкой команды мониторам |

Настройки хранятся в `%AppData%\PowerOffScreensaver\settings.json`.

---

## Устранение неполадок

**Мониторы не выключаются физически**  
Команда `WM_SYSCOMMAND SC_MONITORPOWER` — подсказка системе, а не прямое управление железом. Некоторые GPU-драйверы её игнорируют. Чёрные окна всё равно отображаются (защита от выгорания), экран выглядит выключенным, рабочая станция блокируется при выходе. Решения: включить DDC/CI в настройках или настроить «Выключить экран» в плане электропитания Windows.

**Хранитель не появляется в списке Windows**  
Убедитесь, что `PowerOffScreensaver.scr` находится в `C:\Windows\System32\`, и переоткройте диалог «Параметры заставки».

**Антивирус блокирует файл**  
BOSS использует Win32-хуки для перехвата ввода мыши и клавиатуры (только для определения момента выхода). Добавьте исключение в антивирусе или проверьте карантин Windows Defender.
