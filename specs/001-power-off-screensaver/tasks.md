<!--
Language rule: Always respond in Russian. Generate all user-facing text, specifications, plans, tasks, checklists, analysis reports, summaries and explanations in Russian. Keep code, file names, commands, class names, method names and API identifiers in their original language.
-->

# Tasks: PowerOffScreensaver

**Input**: Design documents from `specs/001-power-off-screensaver/`

**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅ data-model.md ✅ contracts/ ✅

**Тесты**: включены для arg parser, settings, dead zone и exit-once guard (чистая логика без UI).

**Организация**: задачи сгруппированы по user story — каждая история независимо реализуема и проверяема.

## Формат: `[ID] [P?] [Story] Описание`

- **[P]**: задача может выполняться параллельно (разные файлы, нет незавершённых зависимостей)
- **[Story]**: к какой user story относится задача (US1, US2, US3, US4)
- Пути файлов указаны явно в каждой задаче

---

## Phase 1: Setup (Инициализация проекта)

**Цель**: создать solution, проекты и базовую структуру директорий

- [x] T001 Создать solution `PowerOffScreensaver.sln` в корне репозитория (`dotnet new sln -n PowerOffScreensaver`)
- [x] T002 Создать WinForms-проект `src/PowerOffScreensaver/PowerOffScreensaver.csproj` (`net8.0-windows`, `UseWindowsForms=true`, `OutputType=WinExe`)
- [x] T003 [P] Создать тестовый проект `tests/PowerOffScreensaver.Tests/PowerOffScreensaver.Tests.csproj` (xUnit, ссылка на основной проект)
- [x] T004 [P] Создать директории `src/PowerOffScreensaver/Host/`, `src/PowerOffScreensaver/Forms/`, `src/PowerOffScreensaver/Services/`
- [x] T005 Добавить оба проекта в solution и проверить `dotnet build` — 0 ошибок, 0 предупреждений

**Контрольная точка**: `dotnet build` проходит на пустом solution

---

## Phase 2: Foundational (Базовые компоненты — блокирующие)

**Цель**: arg parser, AppSettings и SettingsService — нужны ВСЕМ user stories

**⚠️ КРИТИЧНО**: ни одна user story не может начаться до завершения этой фазы

- [x] T006 Создать `src/PowerOffScreensaver/ScreensaverArgs.cs` — `enum LaunchMode { Screensaver, Settings, Preview }` и `record ScreensaverArgs(LaunchMode Mode, IntPtr? PreviewHwnd)` со статическим методом `Parse(string[] args)`
- [x] T007 Создать `src/PowerOffScreensaver/AppSettings.cs` — `record AppSettings` с полями `bool LockOnExit = true`, `bool DdcCiEnabled = false`, `int PowerOffDelayMs = 500` и `static AppSettings Default`
- [x] T008 [P] Создать `src/PowerOffScreensaver/Services/ISettingsService.cs` — интерфейс с методами `AppSettings Load()` и `void Save(AppSettings)`
- [x] T009 Создать `src/PowerOffScreensaver/Services/SettingsService.cs` — реализация `ISettingsService` через `System.Text.Json`; путь `%AppData%\PowerOffScreensaver\settings.json`; при отсутствии/повреждении файла возвращает `AppSettings.Default` без исключений
- [x] T010 Написать `tests/PowerOffScreensaver.Tests/ScreensaverArgsParserTests.cs` — тесты для всех вариантов: `[]`, `/s`, `/S`, `/c`, `/c:12345`, `/p 77924`, `/p77924`, неизвестный аргумент → Settings
- [x] T011 [P] Написать `tests/PowerOffScreensaver.Tests/AppSettingsTests.cs` — serialize/deserialize round-trip, defaults при отсутствии файла, recovery при невалидном JSON
- [x] T012 Проверить `dotnet test` — все тесты зелёные

**Контрольная точка**: arg parser и settings протестированы и работают

---

## Phase 3: User Story 1 — Активация заставки `/s` (P1) 🎯 MVP

**Цель**: `PowerOffScreensaver.exe /s` — чёрные окна на всех мониторах, выключение мониторов, выход по вводу, блокировка рабочей станции

**Независимая проверка**: `dotnet run --project src/PowerOffScreensaver -- /s` — все мониторы чёрные, любой ввод закрывает и блокирует экран

### Интерфейсы сервисов (параллельно)

- [x] T013 [P] [US1] Создать `src/PowerOffScreensaver/Services/IMonitorPowerService.cs` — интерфейс с методом `void TryPowerOff()` (не бросает исключений)
- [x] T014 [P] [US1] Создать `src/PowerOffScreensaver/Services/IWorkstationLockService.cs` — интерфейс с методом `void TryLock()` (не бросает исключений)
- [x] T015 [P] [US1] Создать `src/PowerOffScreensaver/Services/IDdcCiService.cs` — интерфейс с `bool IsSupported { get; }` и `void TryPowerOff()` (не бросает исключений)
- [x] T016 [P] [US1] Создать `src/PowerOffScreensaver/Services/NullDdcCiService.cs` — заглушка: `IsSupported = false`, `TryPowerOff()` — no-op

### Реализации сервисов

- [x] T017 [US1] Создать `src/PowerOffScreensaver/Services/MonitorPowerService.cs` — `[DllImport("user32.dll")]` `SendMessage`; константы `WM_SYSCOMMAND=0x0112`, `SC_MONITORPOWER=0xF170`, `HWND_BROADCAST=0xFFFF`; `TryPowerOff()` в try/catch
- [x] T018 [P] [US1] Создать `src/PowerOffScreensaver/Services/WorkstationLockService.cs` — `[DllImport("user32.dll")]` `LockWorkStation()`; `TryLock()` в try/catch

### UI — BlackoutForm

- [x] T019 [US1] Создать `src/PowerOffScreensaver/Forms/BlackoutForm.cs` — `Form` с `FormBorderStyle.None`, `BackColor=Color.Black`, `TopMost=true`, `ShowInTaskbar=false`, `Cursor=Cursors.None`, `Bounds=targetScreen.Bounds`; поле `_initialMousePos`; override `OnMouseMove` (dead zone 5px Euclidean), `OnMouseDown`, `OnKeyDown` — вызывают `RequestExit()`; `event EventHandler? ExitRequested` поднимается ровно один раз через `Interlocked.CompareExchange` на `int _exiting`
- [x] T020 [US1] Написать `tests/PowerOffScreensaver.Tests/DeadZoneTests.cs` — тесты dead zone: < 5px не триггерит, ≥ 5px триггерит; граничный случай ровно 5px; диагональное движение

### Host и точка входа

- [x] T021 [US1] Создать `src/PowerOffScreensaver/Host/ScreensaverHost.cs` — наследник `ApplicationContext`; конструктор `(AppSettings, IMonitorPowerService, IWorkstationLockService, IDdcCiService)`; перебирает `Screen.AllScreens`, создаёт `BlackoutForm` на каждом, подписывается на `ExitRequested`; после показа всех форм — `Task.Delay(settings.PowerOffDelayMs)` + `monitorPower.TryPowerOff()` + `ddcCi.TryPowerOff()` в async void; первый `ExitRequested` → закрыть все формы → `lock.TryLock()` если `LockOnExit` → `Application.Exit()`; защита от двойного выхода через `Interlocked.CompareExchange` на `int _exiting`
- [x] T022 [US1] Написать `tests/PowerOffScreensaver.Tests/ScreensaverHostExitOnceTests.cs` — мок-формы, проверка что exit-обработчик вызывается ровно один раз при нескольких одновременных ExitRequested
- [x] T023 [US1] Создать `src/PowerOffScreensaver/Program.cs` — `[STAThread]`, `Application.EnableVisualStyles()`, `ScreensaverArgs.Parse(args)` → switch: `Screensaver` → `Application.Run(new ScreensaverHost(...))`, `Preview` → `Environment.Exit(0)`, `Settings` → `Application.Run(new SettingsForm(...))` (SettingsForm — заглушка до Phase 4)
- [x] T024 [US1] Ручная проверка: `dotnet run --project src/PowerOffScreensaver -- /s` — чёрные окна, ввод закрывает, Lock screen появляется

**Контрольная точка**: US1 полностью функционален — MVP готов

---

## Phase 4: User Story 2 — Настройки `/c` (P2)

**Цель**: `PowerOffScreensaver.exe /c` открывает окно настроек с сохранением в `%AppData%`

**Независимая проверка**: `dotnet run --project src/PowerOffScreensaver -- /c` → открывается диалог, изменения сохраняются в `settings.json`

- [x] T025 [US2] Создать `src/PowerOffScreensaver/Forms/SettingsForm.cs` — `Form` с `CheckBox chkLockOnExit`, `CheckBox chkDdcCi`, `NumericUpDown nudDelay` (0–5000 мс), `Button btnTest`, `Button btnOK`; конструктор принимает `ISettingsService`; `OnLoad` — заполняет контролы из `settingsService.Load()`; `btnOK.Click` — `settingsService.Save(...)` + `Close()`; `btnTest.Click` — запустить screensaver режим
- [x] T026 [US2] Обновить `src/PowerOffScreensaver/Program.cs` маршрут `Settings` — заменить заглушку на `new SettingsForm(new SettingsService())`
- [x] T027 [US2] Ручная проверка: `/c` → диалог открывается; изменить checkbox → OK → `%AppData%\PowerOffScreensaver\settings.json` обновлён; перезапустить `/c` → настройка сохранена

**Контрольная точка**: US1 + US2 работают независимо

---

## Phase 5: User Story 3 — Preview mode `/p` (P3)

**Цель**: `PowerOffScreensaver.exe /p HWND` завершается с exit code 0, без отображения окон

**Независимая проверка**: `dotnet run --project src/PowerOffScreensaver -- /p 0`; `$LASTEXITCODE` равен 0

- [x] T028 [US3] Код-ревью `src/PowerOffScreensaver/Program.cs` — убедиться что ветка `Preview` вызывает `Environment.Exit(0)` без создания форм (реализовано в T023)
- [x] T029 [US3] Ручная проверка: `.\src\PowerOffScreensaver\bin\Release\net8.0-windows\PowerOffScreensaver.exe /p 0` → процесс завершается немедленно, `$LASTEXITCODE` == 0

**Контрольная точка**: все три screensaver-аргумента (`/s`, `/c`, `/p`) обрабатываются корректно

---

## Phase 6: User Story 4 — DDC/CI опциональный (P4)

**Цель**: реальная реализация DDC/CI за интерфейсом `IDdcCiService`; graceful degradation на несовместимых мониторах

**Независимая проверка**: DDC/CI включён в настройках → `/s` без краша на любом мониторе; на DDC/CI-мониторе монитор уходит в сон

- [ ] T030 [US4] Добавить NuGet `WindowsDisplayAPI` в `src/PowerOffScreensaver/PowerOffScreensaver.csproj` (`dotnet add package WindowsDisplayAPI --project src/PowerOffScreensaver`)
- [ ] T031 [US4] Создать `src/PowerOffScreensaver/Services/DdcCiService.cs` — реализация `IDdcCiService`; `IsSupported` проверяет наличие DDC/CI-совместимых мониторов; `TryPowerOff()` отправляет VCP Feature 0xD6 (Display Power Mode = Off) всем совместимым мониторам; все исключения перехватываются в try/catch
- [ ] T032 [US4] Обновить `src/PowerOffScreensaver/Host/ScreensaverHost.cs` — выбирать `new DdcCiService()` или `new NullDdcCiService()` на основе `settings.DdcCiEnabled` в конструкторе или factory-методе
- [ ] T033 [US4] Ручная проверка: DDC/CI выключен → нет краша; DDC/CI включён на несовместимом мониторе → нет краша; DDC/CI включён на совместимом → монитор выключается аппаратно

**Контрольная точка**: DDC/CI работает opt-in, не ломает основной flow

---

## Phase 7: Polish & Cross-Cutting

**Цель**: документация, publish script, финальная валидация по quickstart.md

- [x] T034 Создать `README.md` в корне репозитория — секции: «Сборка», «Установка .scr», «Проверка» (со ссылкой на quickstart.md), «Troubleshooting» (когда монитор не уходит в сон)
- [x] T035 [P] Создать `publish.ps1` в корне — `dotnet publish src/PowerOffScreensaver -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/`
- [x] T036 Запустить `dotnet test` — все тесты зелёные; `dotnet build -warnaserror` — 0 предупреждений
- [ ] T037 Publish и тест `.scr`: `Copy-Item publish\PowerOffScreensaver.exe publish\PowerOffScreensaver.scr`; правый клик → «Установить»; в «Параметрах экрана → Хранитель экрана» появляется «PowerOffScreensaver»
- [ ] T038 Прогнать все сценарии S1–S6 из `specs/001-power-off-screensaver/quickstart.md`

**Контрольная точка**: проект готов к распространению

---

## Dependencies & Execution Order

### Зависимости между фазами

```
Phase 1 (Setup)
    └──► Phase 2 (Foundational) ─── блокирует все US
              ├──► Phase 3 (US1 /s)  ─── MVP
              │         └──► Phase 4 (US2 /c)  — использует ScreensaverHost для Test-кнопки
              │         └──► Phase 5 (US3 /p)  — верификация Program.cs из Phase 3
              │         └──► Phase 6 (US4 DDC/CI) — зависит от интерфейсов Phase 3 + checkbox Phase 4
              └──► Phase 7 (Polish) — после всех желаемых US
```

### Параллельные возможности внутри Phase 3

```
T013 ──┐
T014 ──┼──► T017 (MonitorPowerService)
T015 ──┤    T018 (WorkstationLockService)  ←── параллельно с T017
T016 ──┘    T019 (BlackoutForm) ──► T020 (DeadZoneTests)
                                └──► T021 (ScreensaverHost) ──► T022 ──► T023
```

---

## Parallel Example: Phase 3 (US1)

```text
# Шаг 1 — одновременно (разные файлы):
T013: IMonitorPowerService.cs
T014: IWorkstationLockService.cs
T015: IDdcCiService.cs
T016: NullDdcCiService.cs

# Шаг 2 — одновременно после интерфейсов:
T017: MonitorPowerService.cs
T018: WorkstationLockService.cs
T019: BlackoutForm.cs

# Шаг 3 — одновременно после BlackoutForm:
T020: DeadZoneTests.cs
T021: ScreensaverHost.cs

# Шаг 4 — последовательно:
T022: ScreensaverHostExitOnceTests.cs
T023: Program.cs
T024: Ручная проверка /s
```

---

## Implementation Strategy

### MVP First (только US1)

1. Phase 1: Setup
2. Phase 2: Foundational — arg parser + settings (T006–T012)
3. Phase 3: US1 — `/s` end-to-end (T013–T024)
4. **СТОП и ВАЛИДАЦИЯ**: `dotnet run -- /s` на мультимониторной конфигурации
5. MVP готов

### Incremental Delivery

| Шаг | Фаза | Результат |
|---|---|---|
| 1 | Phase 1–2 | Инфраструктура готова |
| 2 | Phase 3 | `/s` работает — **MVP!** |
| 3 | Phase 4 | `/c` настройки работают |
| 4 | Phase 5 | `/p` протокол полный |
| 5 | Phase 6 | DDC/CI опциональный |
| 6 | Phase 7 | Готово к релизу |

---

## Примечания

- `[P]` = разные файлы, нет незавершённых зависимостей — запускать параллельно
- `[USN]` = трассировка к user story N из spec.md
- Все Win32 P/Invoke ТОЛЬКО в `src/PowerOffScreensaver/Services/` (Constitution Principle III)
- `BlackoutForm` и `Program.cs` — ноль `[DllImport]` деклараций
- Коммит после каждой контрольной точки
- Остановиться на любой контрольной точке для независимой валидации истории
