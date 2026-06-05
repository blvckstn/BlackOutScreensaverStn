# BOSS — Blackout Screensaver STN

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D4?logo=windows)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![Architecture](https://img.shields.io/badge/arch-x64-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

**BOSS** (**B**lack**o**ut **S**creensaver **STN**) is a Windows screensaver that forces all connected monitors to go black and power off — including the second and third display that NVIDIA and AMD drivers stubbornly refuse to turn off on their own.

Works on **single, dual, and triple-screen setups** — each monitor gets its own dedicated black window, no gaps, no taskbar peek-through, no timing mismatches.

> **[Русская версия](#boss--blackout-screensaver-stn-ru)** ниже / below

---

## The Problem BOSS Solves

> *"My second monitor won't turn off when the screensaver starts."*  
> *"NVIDIA GeForce Experience keeps my monitors awake."*  
> *"DisplayPort monitor stays on even after Windows says sleep."*  
> *"Screensaver works on primary monitor but not on secondary."*

Sound familiar? You're not alone.

### NVIDIA & AMD Multi-Monitor Sleep Bug

**NVIDIA GeForce Experience** (and its background service, NVIDIA LocalSystem Container) is one of the most widespread causes of monitors refusing to sleep on Windows 10 and 11. Drivers from the 400-series onward introduced regressions where the `WM_SYSCOMMAND SC_MONITORPOWER` hint — the standard Windows signal to power off displays — gets silently ignored by the GPU driver stack.

The result: Windows *thinks* your monitors are off. They aren't. The NVIDIA forum has dozens of threads about this dating back years. Users on [r/nvidia](https://www.reddit.com/r/nvidia/), [r/Monitors](https://www.reddit.com/r/Monitors/), [r/pcmasterrace](https://www.reddit.com/r/pcmasterrace/), [r/Windows11](https://www.reddit.com/r/Windows11/) and [r/buildapc](https://www.reddit.com/r/buildapc/) report the same symptoms:

- ✗ Second or third monitor stays lit after screensaver kicks in
- ✗ DisplayPort monitors ignore sleep commands (HDMI is less affected)
- ✗ Monitors wake immediately when they shouldn't
- ✗ GeForce Experience overlay / ShadowPlay keeps the display active
- ✗ Screensaver activates visually but display backlight never shuts off

**AMD** users report similar (if less severe) issues with multi-monitor configurations, especially when mixing DisplayPort and HDMI connections or using AMD Software: Adrenalin Edition features.

### How BOSS Fixes It

BOSS doesn't rely on Windows to coordinate monitor power — it takes direct action:

1. **Covers every screen instantly** with a dedicated fullscreen black window per monitor (burn-in protection + visual blackout, regardless of driver behavior)
2. **Broadcasts `WM_SYSCOMMAND SC_MONITORPOWER`** as a system-wide message rather than a per-window hint, bypassing driver-level suppression in most cases
3. **Locks the workstation** on exit so your PC is secure the moment you walk away
4. Exits on the **first mouse movement or keypress** — no delay, no stuck input

No driver changes. No power plan modifications. No administrator rights needed. One `.scr` file, drop and go.

---

## Features

- **Triple / dual / single monitor** — one dedicated black window per display, zero gaps
- Sends `WM_SYSCOMMAND SC_MONITORPOWER` broadcast to power off displays
- Exits on **any mouse movement** (5 px dead zone) or **keypress**
- **Locks the workstation** on exit (configurable)
- Full screensaver protocol: `/s` run · `/c` settings · `/p` preview
- **9-language UI** — EN DE FR ES IT PT PL UK RU, switchable with flag picker
- Optional DDC/CI hardware power-off for compatible monitors
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
`WM_SYSCOMMAND SC_MONITORPOWER` is a hint to the OS, not a direct hardware command — some GPU drivers (especially NVIDIA with GeForce Experience running) ignore it. The black windows still cover all screens (burn-in protection), the display *looks* off, and the workstation locks on exit. Workarounds: enable DDC/CI in settings, disable NVIDIA GeForce Experience overlay, or configure "Turn off display" in Windows Power Options.

**Second monitor stays on / screensaver only works on primary**  
This is the classic NVIDIA multi-monitor sleep bug. BOSS is specifically designed to cover all `Screen.AllScreens` entries simultaneously. If the backlight stays on despite the black window, try enabling DDC/CI in settings or disabling ShadowPlay / NVIDIA LocalSystem Container Service.

**Screensaver doesn't appear in the list**  
Make sure `PowerOffScreensaver.scr` is in `C:\Windows\System32\` and reopen the Screen Saver Settings dialog.

**Antivirus blocks the file**  
BOSS uses Win32 low-level input hooks for mouse/keyboard detection only. Add an exclusion in your antivirus or check Windows Defender quarantine.

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
│   └── SettingsForm.cs           Settings dialog with 9-language switcher
├── Localization/
│   └── Strings.cs                Static localization — EN DE FR ES IT PT PL UK RU
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

**BOSS** (**B**lack**o**ut **S**creensaver **STN**) — хранитель экрана для Windows, который принудительно закрывает все подключённые мониторы чёрными окнами и отправляет команду выключения питания дисплеев — включая второй и третий экран, которые NVIDIA и AMD упрямо отказываются выключать штатными средствами.

Корректно работает на **одном, двух и трёх мониторах** — каждый монитор получает своё отдельное чёрное окно без зазоров и рассинхрона.

---

## Проблема, которую решает BOSS

> *«Второй монитор не выключается при активации хранителя экрана.»*  
> *«NVIDIA GeForce Experience не даёт мониторам уйти в сон.»*  
> *«Монитор на DisplayPort остаётся включённым, хотя Windows говорит, что он спит.»*  
> *«Хранитель работает только на основном мониторе.»*

### Баг сна мониторов NVIDIA и AMD

**NVIDIA GeForce Experience** (и его фоновый сервис NVIDIA LocalSystem Container) — одна из самых распространённых причин, по которым мониторы не засыпают на Windows 10 и 11. Начиная с драйверов серии 400 стандартный сигнал Windows `WM_SYSCOMMAND SC_MONITORPOWER` молча игнорируется на уровне стека GPU-драйвера.

Форум NVIDIA переполнен соответствующими ветками. Пользователи на r/nvidia, r/Monitors, r/pcmasterrace, r/Windows11 и r/buildapc описывают одни и те же симптомы:

- ✗ Второй или третий монитор не гаснет после активации хранителя
- ✗ Мониторы на DisplayPort игнорируют команды сна (HDMI ведёт себя лучше)
- ✗ Мониторы сразу просыпаются обратно
- ✗ Оверлей GeForce Experience / ShadowPlay не даёт дисплею отключиться
- ✗ Хранитель запускается визуально, но подсветка так и не гаснет

**Пользователи AMD** также сталкиваются с аналогичными (хотя и менее острыми) проблемами на мультимониторных конфигурациях — особенно при смешивании DisplayPort и HDMI или при включённых функциях AMD Software: Adrenalin Edition.

### Как BOSS это исправляет

BOSS не ждёт, пока Windows скоординирует выключение мониторов — он действует напрямую:

1. **Мгновенно закрывает все экраны** отдельными полноэкранными чёрными окнами (защита от выгорания + визуальное затемнение независимо от поведения драйвера)
2. **Рассылает `WM_SYSCOMMAND SC_MONITORPOWER`** как системное широковещательное сообщение, а не подсказку конкретному окну — обходит драйверную блокировку в большинстве случаев
3. **Блокирует рабочую станцию** при выходе, чтобы ПК был защищён с момента отхода от стола
4. Выходит при **первом движении мыши или нажатии клавиши** — без задержек

Никаких изменений драйверов. Никаких правок плана электропитания. Права администратора не нужны. Один файл `.scr` — скопировал и работает.

---

## Возможности

- Поддержка **triple / dual / single монитор** — отдельное чёрное окно на каждый дисплей
- Отправляет `WM_SYSCOMMAND SC_MONITORPOWER` для выключения мониторов через ОС
- Выход при любом **движении мыши** (мёртвая зона 5 пикселей) или **нажатии клавиши**
- **Блокирует рабочую станцию** при выходе (настраивается)
- Полный протокол хранителя экрана: `/s` запуск · `/c` настройки · `/p` превью
- **9 языков интерфейса** — RU EN DE FR ES IT PT PL UK, переключатель с флагами
- Опциональное аппаратное выключение DDC/CI для совместимых мониторов
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
Команда `WM_SYSCOMMAND SC_MONITORPOWER` — подсказка ОС, а не прямое управление железом. Некоторые GPU-драйверы (особенно NVIDIA с запущенным GeForce Experience) её игнорируют. Чёрные окна всё равно покрывают все экраны (защита от выгорания), экран выглядит выключенным, рабочая станция блокируется при выходе. Решения: включить DDC/CI в настройках, отключить оверлей GeForce Experience, или настроить «Выключить экран» в плане электропитания Windows.

**Второй монитор остаётся включённым**  
Это классический баг NVIDIA с мультимониторным сном. BOSS специально разработан для одновременного покрытия всех `Screen.AllScreens`. Если подсветка остаётся, попробуйте включить DDC/CI в настройках или отключить ShadowPlay / NVIDIA LocalSystem Container Service.

**Хранитель не появляется в списке Windows**  
Убедитесь, что `PowerOffScreensaver.scr` находится в `C:\Windows\System32\`, и переоткройте диалог «Параметры заставки».

**Антивирус блокирует файл**  
BOSS использует Win32-хуки для перехвата ввода мыши и клавиатуры (только для определения момента выхода). Добавьте исключение в антивирусе или проверьте карантин Windows Defender.
