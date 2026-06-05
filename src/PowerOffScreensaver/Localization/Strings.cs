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
    string DiagClose
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
                "Close"
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
                "Закрыть"
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
                "Schließen"
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
                "Fermer"
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
                "Cerrar"
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
                "Chiudi"
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
                "Fechar"
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
                "Zamknij"
            ),
            ["uk"] = new(
                "🇺🇦", "Українська",
                "PowerOffScreensaver {0} — Налаштування",
                "Блокувати робочу станцію при виході",
                "Спробувати DDC/CI вимкнення моніторів (еспер.)",
                "Затримка перед вимкненням (мс):",
                "Тест", "ОК", "Скасувати", "Мова:", "Версія",
                "Перевірити",
                "BOSS — Перевірка системи",
                "✓  Всі перевірки пройдено — BOSS готовий.",
                "✗  Деякі перевірки не пройдено. Дивіться вище.",
                "Запустити",
                "Закрити"
            ),
        };
}
