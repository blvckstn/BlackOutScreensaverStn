namespace PowerOffScreensaver.Localization;

public record LangStrings(
    string Flag,
    string NativeName,
    string WindowTitle,
    string LockOnExit,
    string DdcCi,
    string DelayMs,
    string TestBtn,
    string OkBtn,
    string CancelBtn,
    string LangLabel,
    string VersionPrefix,
    string CheckBtn,
    string DiagTitle,
    string DiagAllPass,
    string DiagSomeFail,
    string DiagRunNow,
    string DiagClose,
    string LockOnExitHint,
    string DelayHint,
    string DdcCiHint
);

public static class Strings
{
    private static string _lang = "en";

    public static void Set(string lang) =>
        _lang = All.ContainsKey(lang) ? lang : "en";

    public static string Current => _lang;

    public static LangStrings Get(string? lang = null) =>
        All.TryGetValue(lang ?? _lang, out var s) ? s : All["en"];

    public static readonly IReadOnlyDictionary<string, LangStrings> All =
        new Dictionary<string, LangStrings>
        {
            ["en"] = new(
                "🇬🇧", "English",
                "PowerOffScreensaver {0} — Settings",
                "Lock workstation on exit",
                "Try DDC/CI monitor power-off (experimental)",
                "Power-off delay (ms):",
                "Test", "OK", "Cancel", "Language:", "Version",
                "Check System",
                "BOSS — System Check",
                "✓  All checks passed — BOSS is ready.",
                "✗  Some checks failed. See details above.",
                "Run Screensaver",
                "Close",
                "When the screensaver is dismissed, lock Windows so a password is required to return.",
                "How long the screen stays black before the monitors are powered off.",
                "Experimental: ask monitors to power off via DDC/CI. Leave off if unsure."
            ),
            ["ru"] = new(
                "🇷🇺", "Русский",
                "PowerOffScreensaver {0} — Параметры",
                "Блокировать рабочую станцию при выходе",
                "Попробовать DDC/CI выключение мониторов (эксперим.)",
                "Задержка перед отключением (мс):",
                "Тест", "ОК", "Отмена", "Язык:", "Версия",
                "Проверить",
                "BOSS — Проверка системы",
                "✓  Все проверки пройдены — BOSS готов к работе.",
                "✗  Некоторые проверки не пройдены. Смотрите выше.",
                "Запустить",
                "Закрыть",
                "При выходе из заставки блокирует Windows — для возврата потребуется пароль.",
                "Сколько экран остаётся чёрным до отключения мониторов.",
                "Эксперимент: выключение мониторов через DDC/CI. Оставьте выключенным, если не уверены."
            ),
            ["de"] = new(
                "🇩🇪", "Deutsch",
                "PowerOffScreensaver {0} — Einstellungen",
                "Arbeitsstation beim Beenden sperren",
                "DDC/CI-Abschaltung versuchen (experimentell)",
                "Abschaltverzögerung (ms):",
                "Test", "OK", "Abbrechen", "Sprache:", "Version",
                "Prüfen",
                "BOSS — Systemprüfung",
                "✓  Alle Tests bestanden — BOSS ist bereit.",
                "✗  Einige Tests fehlgeschlagen. Siehe Details oben.",
                "Starten",
                "Schließen",
                "Sperrt Windows beim Beenden des Bildschirmschoners; zur Rückkehr ist ein Passwort nötig.",
                "Wie lange der Bildschirm schwarz bleibt, bevor die Monitore abgeschaltet werden.",
                "Experimentell: Monitore per DDC/CI abschalten. Im Zweifel deaktiviert lassen."
            ),
            ["fr"] = new(
                "🇫🇷", "Français",
                "PowerOffScreensaver {0} — Paramètres",
                "Verrouiller la session à la sortie",
                "Essayer l'extinction DDC/CI (expérimental)",
                "Délai avant extinction (ms) :",
                "Test", "OK", "Annuler", "Langue :", "Version",
                "Vérifier",
                "BOSS — Vérification système",
                "✓  Tous les tests réussis — BOSS est prêt.",
                "✗  Certains tests ont échoué. Voir les détails ci-dessus.",
                "Lancer",
                "Fermer",
                "À la sortie de l'écran de veille, verrouille Windows ; un mot de passe sera requis.",
                "Durée pendant laquelle l'écran reste noir avant l'extinction des moniteurs.",
                "Expérimental : éteindre les moniteurs via DDC/CI. Laissez désactivé en cas de doute."
            ),
            ["es"] = new(
                "🇪🇸", "Español",
                "PowerOffScreensaver {0} — Configuración",
                "Bloquear equipo al salir",
                "Intentar apagado DDC/CI (experimental)",
                "Retraso de apagado (ms):",
                "Probar", "OK", "Cancelar", "Idioma:", "Versión",
                "Verificar",
                "BOSS — Verificación del sistema",
                "✓  Todas las pruebas superadas — BOSS listo.",
                "✗  Algunas pruebas fallaron. Ver detalles arriba.",
                "Iniciar",
                "Cerrar",
                "Al salir del salvapantallas, bloquea Windows; se pedirá contraseña para volver.",
                "Cuánto tiempo la pantalla permanece en negro antes de apagar los monitores.",
                "Experimental: apagar monitores mediante DDC/CI. Déjelo desactivado si no está seguro."
            ),
            ["it"] = new(
                "🇮🇹", "Italiano",
                "PowerOffScreensaver {0} — Impostazioni",
                "Blocca la workstation all'uscita",
                "Prova spegnimento DDC/CI (sperimentale)",
                "Ritardo spegnimento (ms):",
                "Test", "OK", "Annulla", "Lingua:", "Versione",
                "Verifica",
                "BOSS — Verifica sistema",
                "✓  Tutti i test superati — BOSS è pronto.",
                "✗  Alcuni test falliti. Vedere i dettagli sopra.",
                "Avvia",
                "Chiudi",
                "All'uscita dal salvaschermo blocca Windows; per tornare sarà richiesta la password.",
                "Per quanto tempo lo schermo resta nero prima di spegnere i monitor.",
                "Sperimentale: spegnere i monitor tramite DDC/CI. Lascia disattivato in caso di dubbio."
            ),
            ["pt"] = new(
                "🇵🇹", "Português",
                "PowerOffScreensaver {0} — Configurações",
                "Bloquear workstation ao sair",
                "Tentar desligar DDC/CI (experimental)",
                "Atraso de desligamento (ms):",
                "Testar", "OK", "Cancelar", "Idioma:", "Versão",
                "Verificar",
                "BOSS — Verificação do sistema",
                "✓  Todos os testes passaram — BOSS está pronto.",
                "✗  Alguns testes falharam. Veja detalhes acima.",
                "Iniciar",
                "Fechar",
                "Ao sair da proteção de tela, bloqueia o Windows; será pedida senha para voltar.",
                "Quanto tempo a tela fica preta antes de os monitores serem desligados.",
                "Experimental: desligar monitores via DDC/CI. Deixe desativado se não tiver certeza."
            ),
            ["pl"] = new(
                "🇵🇱", "Polski",
                "PowerOffScreensaver {0} — Ustawienia",
                "Zablokuj stację roboczą przy wyjściu",
                "Wypróbuj wyłączanie DDC/CI (eksperymentalne)",
                "Opóźnienie wyłączenia (ms):",
                "Test", "OK", "Anuluj", "Język:", "Wersja",
                "Sprawdź",
                "BOSS — Sprawdzanie systemu",
                "✓  Wszystkie testy zaliczone — BOSS jest gotowy.",
                "✗  Niektóre testy nie powiodły się. Patrz szczegóły.",
                "Uruchom",
                "Zamknij",
                "Po zamknięciu wygaszacza blokuje Windows; do powrotu wymagane będzie hasło.",
                "Jak długo ekran pozostaje czarny, zanim monitory zostaną wyłączone.",
                "Eksperymentalne: wyłączanie monitorów przez DDC/CI. W razie wątpliwości pozostaw wyłączone."
            ),
            ["zh"] = new(
                "🇨🇳", "中文",
                "PowerOffScreensaver {0} — 设置",
                "退出时锁定工作站",
                "尝试通过 DDC/CI 关闭显示器（实验性）",
                "关闭延迟（毫秒）：",
                "测试", "确定", "取消", "语言：", "版本",
                "检查系统",
                "BOSS — 系统检查",
                "✓  所有检查通过 — BOSS 已就绪。",
                "✗  部分检查未通过。请查看上方详情。",
                "运行屏保",
                "关闭",
                "退出屏保时锁定 Windows，返回时需要输入密码。",
                "在关闭显示器之前，屏幕保持黑色的时长。",
                "实验性：通过 DDC/CI 关闭显示器。如不确定请保持关闭。"
            ),
        };
}
