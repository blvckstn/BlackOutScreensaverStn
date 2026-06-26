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
├── AppSettings.cs                settings record (LockOnExit, DdcCiEnabled, PowerOffDelayMs, Language)
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
    ├── MonitorPowerService.cs    WM_SYSCOMMAND SC_MONITORPOWER (off + on/wake)
    ├── WorkstationLockService.cs LockWorkStation() + rundll32 fallback
    ├── DesktopLockProbe.cs       OpenInputDesktop lock-state verification
    ├── GlobalInputHook.cs        WH_MOUSE_LL + WH_KEYBOARD_LL system-wide input
    ├── SettingsService.cs        JSON %AppData%\PowerOffScreensaver\settings.json
    └── NullDdcCiService.cs       DDC/CI stub (default)
```

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
- Текущая: **1.4**
- Файл: `src/PowerOffScreensaver/PowerOffScreensaver.csproj` → `<Version>X.Y</Version>`
- Автоотображение в заголовке окна настроек
- Инкрементировать на 0.1 при каждом значимом изменении

## Git-правила
1. Рабочая ветка: `main-clean`
2. Коммиты: `git -c user.email="noreply@github.com" -c user.name="blvckstn" commit`
3. AI-файлы (.specify/, specs/, CLAUDE.md) НЕ в main-clean (см. .gitignore)
4. Приватный бранч `private` — для AI снапшотов

## Тесты
xUnit 2.9.2 на net10.0-windows, 108 тестов, `dotnet test`

## Команды
```powershell
dotnet build PowerOffScreensaver.slnx  # сборка
dotnet test                             # тесты
.\publish.ps1                           # publish → publish\*.exe + *.scr
```

## Бэклог
- [ ] DDC/CI реализация (Phase 6)
- [ ] GitHub Actions CI
- [ ] Release workflow с тегами
