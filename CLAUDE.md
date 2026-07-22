<!--
Language rule: Always respond in Russian. Generate all user-facing text, specifications, plans, tasks, checklists, analysis reports, summaries and explanations in Russian. Keep code, file names, commands, class names, method names and API identifiers in their original language.
AI CONTEXT — not pushed to public GitHub branch (main-clean).
-->

<!-- SPECKIT START -->
Для дополнительного контекста о технологиях, структуре проекта,
командах терминала и прочей важной информации читайте текущий план
по адресу `specs/001-power-off-screensaver/plan.md`.
<!-- SPECKIT END -->

# PowerOffScreensaver / BOSS — AI Context

## Проект
**BOSS (Blackout Screensaver STN)** — Windows screensaver (.scr/.exe) на C# / .NET 10 / WinForms.
GitHub: https://github.com/blvckstn/BlackOutScreensaverStn

## Структура веток
- `main-clean` → GitHub `main`: публичный код, без AI-следов, без личных данных
- `private` (orphan): полный снапшот с AI-контекстом, спеками, .specify/
- `master`: старая ветка (устарела, заменена на main-clean)

Рабочая ветка для разработки: **main-clean**

## Архитектура
```
src/PowerOffScreensaver/
├── Program.cs                    /s /c /p entry point
├── ScreensaverArgs.cs            arg parser
├── AppSettings.cs                settings record (LockOnExit, PowerOffMode, PowerOffDelayMs, Language)
├── PowerPlan.cs                  pure: which power method(s) per mode+DDC support (Layer)
├── InputGate.cs                  dead-zone decision for raw input (pure, Layer 1)
├── LockGuard.cs                  lock attempt→verify→retry→fallback FSM (pure, Layers 3-5)
├── Localization/
│   └── Strings.cs                9-language dict (en ru de fr es it pt pl zh)
├── Host/
│   └── ScreensaverHost.cs        ApplicationContext, multi-monitor lifecycle, guaranteed lock
├── Forms/
│   ├── BlackoutForm.cs           fullscreen black window per monitor, dead zone 5px
│   └── SettingsForm.cs           settings dialog with language switcher + tooltips
└── Services/                     ALL Win32 P/Invoke here only
    ├── MonitorPowerController.cs orchestrates DDC/CI + DPMS by PowerOffMode
    ├── MonitorPowerService.cs    WM_SYSCOMMAND SC_MONITORPOWER (off + on/wake, DPMS)
    ├── DdcCiService.cs           dxva2 per-monitor power (VESA MCCS VCP 0xD6) + probe/verify
    ├── WorkstationLockService.cs LockWorkStation() + rundll32 fallback
    ├── DesktopLockProbe.cs       OpenInputDesktop lock-state verification
    ├── GlobalInputHook.cs        WH_MOUSE_LL + WH_KEYBOARD_LL system-wide input
    ├── InstallerService.cs       per-user install/verify/cleanup (HKCU + %LocalAppData%)
    └── SettingsService.cs        JSON %AppData%\PowerOffScreensaver\settings.json
```

## Надёжное отключение 3 мониторов (feature 004, см. specs/004)
DPMS (`SC_MONITORPOWER`) — глобальный запрос, NVIDIA/AMD применяют его только к
части экранов (обычно основному) → боковые мониторы горят. Решение — адресовать
каждый монитор напрямую по **DDC/CI (VESA MCCS, VCP 0xD6)** через `dxva2.dll`.
- `PowerOffMode`: Dpms (**по умолч., wake-safe**) · Auto · DdcCi · Both. Чистая
  логика — `PowerPlan`. ВАЖНО: жёсткий DDC-off (0x04/0x05) может «застрелить»
  шину DDC и оставить монитор без пробуждения (нужен физический power-cycle) —
  поэтому DDC теперь шлёт **Standby (0x02)** (шина жива, монитор будится), а
  дефолт = DPMS (всегда просыпается от ввода). DDC/Auto/Both — только по выбору.
- `DdcCiService`: `EnumDisplayMonitors`→`GetPhysicalMonitorsFromHMONITOR`,
  `SetVCPFeature(0xD6, 2=standby/1=on)`, `GetVCPFeatureAndVCPFeatureReply` для проверки.
- Пробуждение (feature 005/006, `MonitorPowerController.Wake(mode)` + `DisplaySignal`):
  DPMS-режим = быстрый разбуд (реальный ввод + SC_MONITORPOWER on, без DDC-проверки).
  DDC-режимы (Standby) = верификация по DDC-чтению ниже.
  реальный ввод `SendInput(F15)` → `SetThreadExecutionState(ES_DISPLAY_REQUIRED)`
  → DPMS ON → DDC/CI ON, затем ПРОВЕРКА по DDC-чтению (`WakePlan.AllAwake`), что
  каждый монитор вернулся в On; повторы, на последней попытке эскалация
  `ChangeDisplaySettingsEx` (переустановка видеорежима — как Ctrl+Alt+Del). Пробуждение
  идёт ДО блокировки. Итог пишется в `wake.log` (`WakeLog`). Плюс `ProcessExit`-хэндлер
  восстанавливает мониторы при любом выходе (одним проходом, без ожидания).
- `PowerOffMode.None` (feature 007) = «только чёрный экран» (окна поверх, без
  выключения питания). Нужен для DisplayPort‑мониторов, которые при DPMS‑выключении
  делают hot‑unplug (Windows перекладывает рабочий стол + звук подключения/отключения).
- Re‑cover при смене раскладки: `ScreensaverHost` слушает
  `SystemEvents.DisplaySettingsChanged` и пересоздаёт чёрные окна под текущие мониторы
  (иначе после hot‑unplug DP на боковых виден рабочий стол). Power‑off повторно НЕ шлём
  (чтобы не зациклить hot‑unplug) — только перекрываем.
- Тест в настройках (`MonitorTestForm`, feature 007) — ПОШАГОВЫЙ по каждому монитору:
  для каждого 5‑сек отсчёт «Сейчас данный монитор погаснет» → выключает ЭТОТ монитор
  (`DdcCiService.PowerOne`/`MonitorPowerController.PowerOffOne`; DDC точечно, DPMS
  глобально) → держит тёмным → будит всё → спрашивает Yes/No по этому монитору.
  Так видно, какой монитор на какой метод откликается. DDC per‑monitor через
  `ForEachPhysical(action, onlyIndex)`.
- UX настроек (feature 007): исправлена вёрстка (комбо не наезжает на подпись),
  убрана дублирующая кнопка «Тест», «Тест мониторов…» и «Проверить» слева,
  ОК/Отмена — справа снизу.
- Headless `/install` (Program) ставит заставку из CLI + `initialized=true`,
  пишет `%LocalAppData%\Blackout ScreenSaver\install.log`.

## Установка в систему (feature 003)
Имя в системе: **Blackout ScreenSaver** (csproj Product/Title/Description; имя в
списке заставок Windows = имя файла `Blackout ScreenSaver.scr`).
Механизм без UAC (`InstallerService`):
- копирует текущий exe в `%LocalAppData%\Blackout ScreenSaver\Blackout ScreenSaver.scr`
- пишет `HKCU\Control Panel\Desktop`: SCRNSAVE.EXE, ScreenSaveActive=1, ScreenSaverIsSecure=1, ScreenSaveTimeOut(=300 если не задан)
- `SystemParametersInfo(SPI_SETSCREENSAVEACTIVE/TIMEOUT)` — применить сразу
- удаляет старые версии (legacy папки PowerOffScreensaver/BOSS, чужие .scr/.exe в install dir, System32 best-effort)
- `GetStatus()` проверяет: установлено / актуальная версия (FileVersion) / активная заставка
Первый запуск (`/s` + !Initialized) → DiagnosticsForm с кнопкой «Install into Windows»
после проверки системы. Program.cs ставит Initialized=true после онбординга.
Чистые хелперы (`StaleArtifacts`, `VersionsMatch`, `PathsEqual`) покрыты тестами.

## Гарантированная блокировка при выходе (feature 002, см. specs/002)
Многослойный последовательный алгоритм — ЛЮБОй ввод гарантированно гасит заставку и блокирует ПК:
1. Глобальные LL-хуки мыши/клавиатуры (ввод независимо от фокуса) — `GlobalInputHook` + `InputGate`
2. Пробуждение дисплея перед блокировкой (SC_MONITORPOWER ON + ES_DISPLAY_REQUIRED)
3. Блокировка ДО закрытия форм (нет «мигания» рабочего стола)
4. Проверка факта блокировки через `DesktopLockProbe` (OpenInputDesktop) + повтор — `LockGuard`
5. Резерв: rundll32 user32.dll,LockWorkStation
Чистая логика (`InputGate`, `LockGuard`) покрыта unit-тестами с фейками — тесты НЕ блокируют сборочную машину.

## Ключевые технические решения
- **net10.0-windows** (только этот SDK установлен)
- **Нет admin прав** — приложение работает без UAC
- **Dead zone**: Euclidean > 5px триггерит выход (ровно 5px — нет)
- **Thread-safe exit**: `Interlocked.CompareExchange(ref _exiting, 1, 0)`
- **Delayed power-off**: `System.Threading.Timer` (не WinForms Timer)
- **Settings**: camelCase JSON (`lockOnExit`, `ddcCiEnabled`, `powerOffDelayMs`, `language`)
- **PublishSingleFile**: `.\publish.ps1` → `publish\PowerOffScreensaver.exe` + `.scr`
- **Версионирование**: `<Version>X.Y</Version>` в .csproj → `Assembly.GetName().Version`

## Локализация (9 языков)
Файл: `src/PowerOffScreensaver/Localization/Strings.cs`
Языки: en ru de fr es it pt pl zh (uk заменён на zh в v1.4)
Переключатель: ComboBox с flag emoji в SettingsForm, тултипы-подсказки на каждом поле
Сохранение языка: `AppSettings.Language` → `settings.json`

## Версионирование
- Текущая: **1.9**
- Файл: `src/PowerOffScreensaver/PowerOffScreensaver.csproj` → `<Version>X.Y</Version>`
- Автоотображение в заголовке окна настроек
- Инкрементировать на 0.1 при каждом значимом изменении

## Git-правила
1. Рабочая ветка: `main-clean`
2. Коммиты: `git -c user.email="noreply@github.com" -c user.name="blvckstn" commit`
3. AI-файлы (.specify/, specs/, CLAUDE.md) НЕ в main-clean (см. .gitignore)
4. Приватный бранч `private` — для AI снапшотов

## Тесты
xUnit 2.9.2 на net10.0-windows, 162 теста, `dotnet test`

## Команды
```powershell
dotnet build PowerOffScreensaver.slnx  # сборка
dotnet test                             # тесты
.\publish.ps1                           # publish → publish\*.exe + *.scr
```

## Бэклог
- [x] DDC/CI реализация (feature 004) — per-monitor VCP 0xD6 + тест в настройках
- [ ] GitHub Actions CI
- [ ] Release workflow с тегами
