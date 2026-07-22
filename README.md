# BOSS — Blackout ScreenSaver

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D4?logo=windows)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![Architecture](https://img.shields.io/badge/arch-x64-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

**BOSS** (**B**lack**o**ut **S**creen**S**aver) is a Windows screensaver that blacks out and powers off every connected monitor, including the second and third display that NVIDIA and AMD drivers often refuse to turn off on their own.

It works on single, dual, and triple-screen setups. Each monitor gets its own black window, so there are no gaps, no taskbar peeking through, and no display that stays lit while the others go dark. When you come back, the first key or mouse movement wakes the screens and locks Windows, every time.


![BOSS screenshot](https://github.com/user-attachments/assets/8d3f5953-ebab-458b-aa0c-f50af46a0d55)



> **[Русская версия](#boss--blackout-screensaver-ru)** ниже / below

---

## Why won't my second monitor turn off?

> *"My second monitor won't turn off when the screensaver starts."*  
> *"NVIDIA GeForce Experience keeps my monitors awake."*  
> *"DisplayPort monitor stays on even after Windows says sleep."*  
> *"Screensaver works on primary monitor but not on secondary."*

If any of those sound familiar, you've hit a well-documented multi-monitor sleep problem on Windows 10 and 11, and you are not alone.

### The NVIDIA and AMD multi-monitor sleep bug

NVIDIA GeForce Experience and its background service (NVIDIA LocalSystem Container) is one of the most common reasons monitors refuse to sleep. Drivers from the 400-series onward shipped regressions where `WM_SYSCOMMAND SC_MONITORPOWER`, the standard Windows signal to power off a display, gets silently dropped inside the GPU driver stack.

So Windows thinks your monitors are off. They aren't. The NVIDIA forum has carried threads about this for years, and users on [r/nvidia](https://www.reddit.com/r/nvidia/), [r/Monitors](https://www.reddit.com/r/Monitors/), [r/pcmasterrace](https://www.reddit.com/r/pcmasterrace/), [r/Windows11](https://www.reddit.com/r/Windows11/) and [r/buildapc](https://www.reddit.com/r/buildapc/) report the same symptoms:

- ✗ Second or third monitor stays lit after the screensaver kicks in
- ✗ DisplayPort monitors ignore sleep commands (HDMI is less affected)
- ✗ Monitors wake immediately when they shouldn't
- ✗ GeForce Experience overlay or ShadowPlay keeps the display active
- ✗ The screensaver activates visually but the backlight never shuts off

AMD users hit similar problems on multi-monitor setups, usually milder, and most often when mixing DisplayPort with HDMI or when AMD Software: Adrenalin Edition features are running.

### How BOSS fixes it

BOSS doesn't wait for Windows to coordinate monitor power. It acts directly:

1. It covers every screen at once with a dedicated fullscreen black window per monitor. That gives you burn-in protection and a real blackout no matter what the driver does.
2. It powers off each monitor individually over DDC/CI (the VESA MCCS "power mode" command), talking straight to the panel instead of asking the GPU driver. That is what makes the second and third monitor actually go dark when a plain DPMS broadcast leaves them lit. Monitors that don't speak DDC/CI still get the DPMS broadcast as a fallback, and Auto mode figures out which is which.
3. When you return, it wakes every monitor (a DDC/CI-off panel won't come back on its own), locks Windows, and verifies the lock actually took effect before it exits, so you are never left at an unlocked desktop or a dark side monitor.

You don't need to change drivers, edit your power plan, or run as administrator. It's one `.scr` file: drop it in and go.

---

## Features

- Triple, dual, or single monitor, with one dedicated black window per display and no gaps
- Reliable per-monitor power-off over DDC/CI (VESA MCCS, VCP 0xD6) so the second and third monitor actually go dark, with the `WM_SYSCOMMAND SC_MONITORPOWER` DPMS broadcast as a fallback
- Auto-detects each monitor's DDC/CI support and picks the method; a built-in **monitor test** shows which screens really turned off
- Exits on any mouse movement (5 px dead zone) or any keypress
- Guaranteed lock on wake: global mouse and keyboard hooks catch input regardless of window focus, the display is woken so the lock screen is visible, and the lock is verified with a retry and a fallback
- Full screensaver protocol: `/s` run, `/c` settings, `/p` preview
- 9-language interface (RU EN DE FR ES IT PT PL ZH) with a flag picker
- One-click per-user install that sets BOSS as your active screensaver, verifies the version, and removes old copies, all without administrator rights
- A single self-contained `.exe` / `.scr` with no installer and no dependencies

---

## Requirements

- Windows 10 or 11 (x64)
- .NET 10 SDK, only if you build from source ([download](https://dotnet.microsoft.com/download))

---

## Download

Grab the latest `PowerOffScreensaver.scr` from the [Releases page](https://github.com/blvckstn/BlackOutScreensaverStn/releases/latest), then follow the install steps below. If you'd rather build it yourself, see the next section.

---

## Build from source

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

## Install as a screensaver

The easiest way needs no administrator rights:

1. Download the `.scr` from Releases (or run `.\publish.ps1`) and run it.
2. On first launch BOSS runs a quick system check, then offers **Install into Windows**. Click it.
3. That copies BOSS to your user profile as `Blackout ScreenSaver`, sets it as the active Windows screensaver, verifies the installed version, and removes any older copies.

The installer writes only to your own user account (`HKCU` and `%LocalAppData%`), so Windows never asks for elevation. You'll see it as **Blackout ScreenSaver** in the Windows screen saver list.

Prefer to do it by hand? Right-click `PowerOffScreensaver.scr`, choose **Install**, or copy it to `C:\Windows\System32\` from an elevated PowerShell, then pick it under **Settings → Personalization → Lock screen → Screen saver settings**.

---

## Usage

| Command | Effect |
|---|---|
| `PowerOffScreensaver.exe /s` | Launch screensaver (all monitors black, then power off) |
| `PowerOffScreensaver.exe /c` | Open settings dialog |
| `PowerOffScreensaver.exe /p 0` | Preview mode (exits immediately, code 0) |

---

## Settings

Open the settings dialog with `/c`. Every option has an inline tooltip in your chosen language.

| Setting | Default | Description |
|---|---|---|
| Lock workstation on exit | On | Locks Windows when the screensaver exits, then confirms the lock took effect |
| Monitor power-off method | Auto | Auto (DDC/CI per monitor + DPMS for the rest), DDC/CI only, DPMS only, or Both |
| Power-off delay (ms) | 500 | Pause before sending the monitor power-off command |

Use **Test monitors…** to turn each screen off for a few seconds and see which ones actually go dark. If a monitor stays lit, switch the method to **Both** or enable DDC/CI in that monitor's on-screen menu.

Settings live in `%AppData%\PowerOffScreensaver\settings.json`.

---

## How BOSS guarantees the lock

A common complaint with screensavers is that you move the mouse, the screen comes back, but the PC never locks. BOSS closes that gap with several layers that run in order:

1. System-wide low-level mouse and keyboard hooks catch input no matter which window has focus, including the input that only wakes a sleeping display.
2. Before locking, BOSS wakes the monitors so the Windows lock screen is actually drawn instead of hiding behind a dark panel.
3. It locks while the black windows still cover everything, so the desktop never flashes into view.
4. It checks whether the session really locked (via the input desktop) and retries, then falls back to a separate lock path, before the process exits.

The decision logic is covered by unit tests that never lock the build machine.

---

## Troubleshooting

**Monitors don't turn off physically.**  
`WM_SYSCOMMAND SC_MONITORPOWER` is a hint to the OS, not a direct hardware command, and some GPU drivers (especially NVIDIA with GeForce Experience running) ignore it. The black windows still cover every screen for burn-in protection, the display looks off, and the workstation locks on exit. You can enable DDC/CI in settings, disable the NVIDIA GeForce Experience overlay, or set "Turn off display" in Windows Power Options.

**Second monitor stays on, or the screensaver only works on the primary display.**  
This is the classic NVIDIA multi-monitor sleep bug. BOSS covers all `Screen.AllScreens` entries at once. If the backlight stays on despite the black window, try enabling DDC/CI in settings or turning off ShadowPlay and the NVIDIA LocalSystem Container service.

**Monitors won't wake after the screensaver, or need Ctrl+Alt+Del.**  
A panel turned off over DDC/CI won't come back from a mouse move on its own. BOSS wakes each monitor with a real input event, re-asserts the video signal, sends DDC/CI On, and verifies over DDC/CI readback that every panel is back before it locks, escalating to a display re-detect if needed. Each wake is logged to `%LocalAppData%\Blackout ScreenSaver\wake.log`. Use **Test monitors…** to confirm your panels wake (it shows how many came back); if one doesn't, switch the method to **DPMS** for that machine.

**The screensaver doesn't appear in the list.**  
Make sure `PowerOffScreensaver.scr` is in `C:\Windows\System32\`, then reopen the Screen Saver Settings dialog.

**Antivirus blocks the file.**  
BOSS uses Win32 low-level input hooks to detect mouse and keyboard activity, which some antivirus tools flag. Add an exclusion or check the Windows Defender quarantine.

---

## Architecture

```
src/PowerOffScreensaver/
├── Program.cs                    Entry point — parses /s /c /p
├── ScreensaverArgs.cs            Argument parser
├── AppSettings.cs                Settings model
├── InputGate.cs                  Dead-zone decision for raw input (pure, testable)
├── LockGuard.cs                  Lock attempt → verify → retry → fallback (pure, testable)
├── Host/
│   └── ScreensaverHost.cs        ApplicationContext — manages screensaver lifetime
├── Forms/
│   ├── BlackoutForm.cs           Fullscreen black window per monitor
│   └── SettingsForm.cs           Settings dialog with 9-language switcher + tooltips
├── Localization/
│   └── Strings.cs                Static localization — RU EN DE FR ES IT PT PL ZH
└── Services/                     All Win32 P/Invoke lives here only
    ├── MonitorPowerController.cs Orchestrates DDC/CI + DPMS by chosen mode
    ├── MonitorPowerService.cs    SC_MONITORPOWER off + on/wake (DPMS)
    ├── DdcCiService.cs           Per-monitor power via dxva2 (VESA MCCS VCP 0xD6)
    ├── WorkstationLockService.cs LockWorkStation() + rundll32 fallback
    ├── DesktopLockProbe.cs       Lock-state verification (OpenInputDesktop)
    ├── GlobalInputHook.cs        System-wide low-level mouse + keyboard hooks
    ├── InstallerService.cs       Per-user install/verify/cleanup (no admin)
    └── SettingsService.cs        JSON load / save
```

---

---

# BOSS — Blackout ScreenSaver <sup>RU</sup>

![Platform](https://img.shields.io/badge/платформа-Windows%2010%2F11-0078D4?logo=windows)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)

**BOSS** (**B**lack**o**ut **S**creen**S**aver) — хранитель экрана для Windows, который закрывает чёрными окнами и выключает все подключённые мониторы, включая второй и третий экран, которые NVIDIA и AMD часто отказываются выключать штатными средствами.

Работает на одном, двух и трёх мониторах. Каждый экран получает своё чёрное окно, поэтому не остаётся ни зазоров, ни просвечивающей панели задач, ни дисплея, который светится, пока остальные погасли. Когда вы возвращаетесь, первое нажатие клавиши или движение мыши будит экраны и блокирует Windows — каждый раз.

---

## Почему не выключается второй монитор?

> *«Второй монитор не выключается при активации хранителя экрана.»*  
> *«NVIDIA GeForce Experience не даёт мониторам уйти в сон.»*  
> *«Монитор на DisplayPort остаётся включённым, хотя Windows говорит, что он спит.»*  
> *«Хранитель работает только на основном мониторе.»*

Если что-то из этого вам знакомо, вы столкнулись с хорошо известной проблемой сна мониторов на Windows 10 и 11, и вы такой не один.

### Баг сна мониторов NVIDIA и AMD

NVIDIA GeForce Experience и его фоновый сервис NVIDIA LocalSystem Container — одна из самых частых причин, по которым мониторы не засыпают. Начиная с драйверов серии 400 стандартный сигнал Windows `WM_SYSCOMMAND SC_MONITORPOWER` молча теряется внутри стека GPU-драйвера.

В итоге Windows считает, что мониторы выключены. На деле нет. Форум NVIDIA годами полнится такими ветками, а пользователи на r/nvidia, r/Monitors, r/pcmasterrace, r/Windows11 и r/buildapc описывают одни и те же симптомы:

- ✗ Второй или третий монитор не гаснет после активации хранителя
- ✗ Мониторы на DisplayPort игнорируют команды сна (HDMI ведёт себя лучше)
- ✗ Мониторы сразу просыпаются обратно
- ✗ Оверлей GeForce Experience или ShadowPlay не даёт дисплею отключиться
- ✗ Хранитель запускается визуально, но подсветка так и не гаснет

Пользователи AMD сталкиваются с похожими проблемами на мультимониторных конфигурациях, обычно мягче, и чаще всего при смешивании DisplayPort с HDMI или при включённых функциях AMD Software: Adrenalin Edition.

### Как BOSS это исправляет

BOSS не ждёт, пока Windows скоординирует выключение мониторов. Он действует напрямую:

1. Сразу закрывает все экраны отдельными полноэкранными чёрными окнами. Это даёт защиту от выгорания и настоящее затемнение независимо от поведения драйвера.
2. Выключает каждый монитор по отдельности через DDC/CI (команда «power mode» из VESA MCCS), обращаясь напрямую к панели, а не к драйверу видеокарты. Именно это гасит второй и третий монитор там, где обычной DPMS-рассылки не хватает. Мониторы без DDC/CI получают DPMS-рассылку как запасной вариант, а режим «Авто» сам определяет, кому что.
3. При возврате будит каждый монитор (панель, выключенная по DDC/CI, сама не включится), блокирует Windows и проверяет, что блокировка действительно сработала, прежде чем завершиться — так что вы не останетесь ни у разблокированного стола, ни с погасшим боковым монитором.

Не нужно менять драйверы, править план электропитания или запускать от администратора. Это один файл `.scr`: скопировал и работает.

---

## Возможности

- Поддержка triple / dual / single монитор, по отдельному чёрному окну на каждый дисплей, без зазоров
- Надёжное выключение каждого монитора по DDC/CI (VESA MCCS, VCP 0xD6) — второй и третий экран действительно гаснут, а DPMS-рассылка `WM_SYSCOMMAND SC_MONITORPOWER` работает как запасной вариант
- Автоопределение поддержки DDC/CI у каждого монитора и выбор метода; встроенный **тест мониторов** показывает, какие экраны реально погасли
- Выход при любом движении мыши (мёртвая зона 5 пикселей) или нажатии клавиши
- Гарантированная блокировка при пробуждении: глобальные хуки мыши и клавиатуры ловят ввод независимо от фокуса окна, дисплей будится, чтобы экран блокировки был виден, а сама блокировка проверяется с повтором и резервным путём
- Полный протокол хранителя экрана: `/s` запуск, `/c` настройки, `/p` превью
- 9 языков интерфейса (RU EN DE FR ES IT PT PL ZH) с переключателем-флажком
- Установка в один клик для пользователя: назначает BOSS активной заставкой, проверяет версию и удаляет старые копии — без прав администратора
- Один самодостаточный `.exe` / `.scr` без установщика и зависимостей

---

## Требования

- Windows 10 или 11 (x64)
- .NET 10 SDK, только для сборки из исходников ([скачать](https://dotnet.microsoft.com/download))

---

## Скачать

Возьмите последний `PowerOffScreensaver.scr` со [страницы релизов](https://github.com/blvckstn/BlackOutScreensaverStn/releases/latest) и выполните установку ниже. Если хотите собрать сами, смотрите следующий раздел.

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

Самый простой способ не требует прав администратора:

1. Скачайте `.scr` из релизов (или запустите `.\publish.ps1`) и запустите файл.
2. При первом запуске BOSS делает быструю проверку системы и предлагает **Установить в Windows**. Нажмите кнопку.
3. BOSS скопирует себя в профиль пользователя как `Blackout ScreenSaver`, назначит активной заставкой Windows, проверит установленную версию и удалит старые копии.

Установщик пишет только в ваш профиль (`HKCU` и `%LocalAppData%`), поэтому Windows не запрашивает повышение прав. В списке заставок Windows программа будет называться **Blackout ScreenSaver**.

Предпочитаете вручную? Правый клик на `PowerOffScreensaver.scr` → **Установить**, либо скопируйте в `C:\Windows\System32\` из PowerShell с правами администратора, затем выберите её в **Параметры → Персонализация → Экран блокировки → Параметры заставки**.

---

## Использование

| Команда | Действие |
|---|---|
| `PowerOffScreensaver.exe /s` | Запустить хранитель экрана |
| `PowerOffScreensaver.exe /c` | Открыть настройки |
| `PowerOffScreensaver.exe /p 0` | Режим превью (завершается сразу, код 0) |

---

## Настройки

Откройте диалог настроек через `/c`. У каждого параметра есть всплывающая подсказка на выбранном языке.

| Параметр | По умолчанию | Описание |
|---|---|---|
| Блокировать рабочую станцию при выходе | Вкл | Блокирует Windows при выходе из хранителя и подтверждает, что блокировка сработала |
| Метод отключения мониторов | Авто | Авто (DDC/CI на каждый монитор + DPMS для остальных), только DDC/CI, только DPMS или оба |
| Задержка перед отключением (мс) | 500 | Пауза перед отправкой команды мониторам |

Кнопка **Тест мониторов…** выключает каждый экран на несколько секунд, чтобы вы увидели, какие реально гаснут. Если монитор не гаснет, переключите метод на **Оба** или включите DDC/CI в экранном меню монитора.

Настройки хранятся в `%AppData%\PowerOffScreensaver\settings.json`.

---

## Как BOSS гарантирует блокировку

Частая претензия к хранителям экрана: вы двигаете мышь, экран возвращается, а компьютер так и не блокируется. BOSS закрывает этот пробел несколькими слоями, которые работают по очереди:

1. Системные низкоуровневые хуки мыши и клавиатуры ловят ввод независимо от того, какое окно в фокусе, включая ввод, который лишь будит спящий дисплей.
2. Перед блокировкой BOSS будит мониторы, чтобы экран блокировки Windows действительно отрисовался, а не прятался за чёрной панелью.
3. Блокировка происходит, пока чёрные окна ещё закрывают всё, поэтому рабочий стол не мелькает.
4. BOSS проверяет, действительно ли сессия заблокирована (через input desktop), повторяет попытку и при необходимости использует отдельный резервный путь до того, как процесс завершится.

Логика принятия решений покрыта unit-тестами, которые не блокируют сборочную машину.

---

## Устранение неполадок

**Мониторы не выключаются физически.**  
Команда `WM_SYSCOMMAND SC_MONITORPOWER` — подсказка ОС, а не прямое управление железом, и некоторые GPU-драйверы (особенно NVIDIA с запущенным GeForce Experience) её игнорируют. Чёрные окна всё равно покрывают все экраны для защиты от выгорания, экран выглядит выключенным, а рабочая станция блокируется при выходе. Можно включить DDC/CI в настройках, отключить оверлей GeForce Experience или настроить «Выключить экран» в плане электропитания Windows.

**Второй монитор остаётся включённым, или хранитель работает только на основном экране.**  
Это классический баг NVIDIA с мультимониторным сном. BOSS покрывает все `Screen.AllScreens` одновременно. Если подсветка остаётся, попробуйте включить DDC/CI в настройках или отключить ShadowPlay и сервис NVIDIA LocalSystem Container.

**Мониторы не просыпаются после хранителя или требуют Ctrl+Alt+Del.**  
Панель, выключенная по DDC/CI, сама от движения мыши не включается. BOSS будит каждый монитор реальным событием ввода, заново подаёт видеосигнал, шлёт DDC/CI On и по DDC-чтению проверяет, что каждый монитор вернулся в работу, — ещё до блокировки, с эскалацией через переустановку видеорежима при необходимости. Каждое пробуждение пишется в `%LocalAppData%\Blackout ScreenSaver\wake.log`. Кнопкой **Тест мониторов…** можно убедиться, что панели просыпаются (показывает, сколько вернулось); если какая-то не встаёт — выберите для этой машины метод **DPMS**.

**Хранитель не появляется в списке Windows.**  
Убедитесь, что `PowerOffScreensaver.scr` находится в `C:\Windows\System32\`, и переоткройте диалог «Параметры заставки».

**Антивирус блокирует файл.**  
BOSS использует Win32-хуки для перехвата ввода мыши и клавиатуры, что некоторые антивирусы помечают. Добавьте исключение или проверьте карантин Windows Defender.
