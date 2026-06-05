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
    string VersionPrefix
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
                "Test", "OK", "Cancel", "Language:", "Version"
            ),
            ["ru"] = new(
                "🇷🇺", "Русский",
                "PowerOffScreensaver {0} — Параметры",
                "Блокировать рабочую станцию при выходе",
                "Попробовать DDC/CI выключение мониторов (эксперим.)",
                "Задержка перед отключением (мс):",
                "Тест", "ОК", "Отмена", "Язык:", "Версия"
            ),
            ["de"] = new(
                "🇩🇪", "Deutsch",
                "PowerOffScreensaver {0} — Einstellungen",
                "Arbeitsstation beim Beenden sperren",
                "DDC/CI-Abschaltung versuchen (experimentell)",
                "Abschaltverzögerung (ms):",
                "Test", "OK", "Abbrechen", "Sprache:", "Version"
            ),
            ["fr"] = new(
                "🇫🇷", "Français",
                "PowerOffScreensaver {0} — Paramètres",
                "Verrouiller la session à la sortie",
                "Essayer l'extinction DDC/CI (expérimental)",
                "Délai avant extinction (ms) :",
                "Test", "OK", "Annuler", "Langue :", "Version"
            ),
            ["es"] = new(
                "🇪🇸", "Español",
                "PowerOffScreensaver {0} — Configuración",
                "Bloquear equipo al salir",
                "Intentar apagado DDC/CI (experimental)",
                "Retraso de apagado (ms):",
                "Probar", "OK", "Cancelar", "Idioma:", "Versión"
            ),
            ["it"] = new(
                "🇮🇹", "Italiano",
                "PowerOffScreensaver {0} — Impostazioni",
                "Blocca la workstation all'uscita",
                "Prova spegnimento DDC/CI (sperimentale)",
                "Ritardo spegnimento (ms):",
                "Test", "OK", "Annulla", "Lingua:", "Versione"
            ),
            ["pt"] = new(
                "🇵🇹", "Português",
                "PowerOffScreensaver {0} — Configurações",
                "Bloquear workstation ao sair",
                "Tentar desligar DDC/CI (experimental)",
                "Atraso de desligamento (ms):",
                "Testar", "OK", "Cancelar", "Idioma:", "Versão"
            ),
            ["pl"] = new(
                "🇵🇱", "Polski",
                "PowerOffScreensaver {0} — Ustawienia",
                "Zablokuj stację roboczą przy wyjściu",
                "Wypróbuj wyłączanie DDC/CI (eksperymentalne)",
                "Opóźnienie wyłączenia (ms):",
                "Test", "OK", "Anuluj", "Język:", "Wersja"
            ),
            ["uk"] = new(
                "🇺🇦", "Українська",
                "PowerOffScreensaver {0} — Налаштування",
                "Блокувати робочу станцію при виході",
                "Спробувати DDC/CI вимкнення моніторів (еспер.)",
                "Затримка перед вимкненням (мс):",
                "Тест", "ОК", "Скасувати", "Мова:", "Версія"
            ),
        };
}
