namespace TimerHud.Localization;

public static class Translations
{
    private static readonly MessageCatalog EnglishCatalog = new()
    {
        LanguageName = "English",
        LanguageChanged = "Language set to {0}.",
        PlayerOnly = "This command is available to players only.",
        TimerEnabled = "Timer display enabled.",
        TimerDisabled = "Timer display disabled.",
        ToggleDisabledByServer = "Toggling the timer display is disabled on this server.",
        BindUsage = "Usage: !timerhud_bind <button>. Use !timerhud_bind none to remove the bind.",
        BindButtons = "Available buttons: {0}.",
        BindCurrent = "Current button: {0}.",
        BindMissing = "No button is bound yet.",
        BindUnknown = "Unknown button: {0}.",
        BindSet = "Button {0} is now bound to the timer.",
        BindCleared = "Timer button removed.",
        BindCycle = "Presses cycle in this order: 1) start, 2) pause, 3) overwrite.",
        RunStarted = "Timer started.",
        RunPaused = "Timer paused at {0}.",
        RunOverwritten = "Timer reset, {0} kept as the temporary value.",
    };

    private static readonly MessageCatalog RussianCatalog = new()
    {
        LanguageName = "Русский",
        LanguageChanged = "Язык переключён на {0}.",
        PlayerOnly = "Команда доступна только игроку.",
        TimerEnabled = "Таймер включён.",
        TimerDisabled = "Таймер выключен.",
        ToggleDisabledByServer = "Переключение таймера запрещено настройками сервера.",
        BindUsage = "Использование: !timerhud_bind <кнопка>. Команда !timerhud_bind none снимает бинд.",
        BindButtons = "Доступные кнопки: {0}.",
        BindCurrent = "Текущая кнопка: {0}.",
        BindMissing = "Кнопка ещё не назначена.",
        BindUnknown = "Неизвестная кнопка: {0}.",
        BindSet = "Кнопка {0} назначена на таймер.",
        BindCleared = "Кнопка таймера снята.",
        BindCycle = "Нажатия идут по кругу: 1) старт, 2) пауза, 3) перезапись.",
        RunStarted = "Таймер запущен.",
        RunPaused = "Таймер на паузе: {0}.",
        RunOverwritten = "Таймер сброшен, {0} сохранено как временное значение.",
    };

    private static readonly MessageCatalog GermanCatalog = new()
    {
        LanguageName = "Deutsch",
        LanguageChanged = "Sprache auf {0} umgestellt.",
        PlayerOnly = "Dieser Befehl steht nur Spielern zur Verfügung.",
        TimerEnabled = "Timer-Anzeige aktiviert.",
        TimerDisabled = "Timer-Anzeige deaktiviert.",
        ToggleDisabledByServer = "Das Umschalten der Timer-Anzeige ist auf diesem Server deaktiviert.",
        BindUsage = "Verwendung: !timerhud_bind <Taste>. Mit !timerhud_bind none wird die Bindung entfernt.",
        BindButtons = "Verfügbare Tasten: {0}.",
        BindCurrent = "Aktuelle Taste: {0}.",
        BindMissing = "Es ist noch keine Taste gebunden.",
        BindUnknown = "Unbekannte Taste: {0}.",
        BindSet = "Taste {0} ist jetzt mit dem Timer verbunden.",
        BindCleared = "Timer-Taste entfernt.",
        BindCycle = "Die Tastendrücke wechseln in dieser Reihenfolge: 1) Start, 2) Pause, 3) Überschreiben.",
        RunStarted = "Timer gestartet.",
        RunPaused = "Timer bei {0} pausiert.",
        RunOverwritten = "Timer zurückgesetzt, {0} als temporärer Wert gespeichert.",
    };

    private static readonly MessageCatalog UkrainianCatalog = new()
    {
        LanguageName = "Українська",
        LanguageChanged = "Мову змінено на {0}.",
        PlayerOnly = "Команда доступна лише гравцеві.",
        TimerEnabled = "Таймер увімкнено.",
        TimerDisabled = "Таймер вимкнено.",
        ToggleDisabledByServer = "Перемикання таймера заборонено налаштуваннями сервера.",
        BindUsage = "Використання: !timerhud_bind <кнопка>. Команда !timerhud_bind none знімає бінд.",
        BindButtons = "Доступні кнопки: {0}.",
        BindCurrent = "Поточна кнопка: {0}.",
        BindMissing = "Кнопку ще не призначено.",
        BindUnknown = "Невідома кнопка: {0}.",
        BindSet = "Кнопку {0} призначено на таймер.",
        BindCleared = "Кнопку таймера знято.",
        BindCycle = "Натискання йдуть по колу: 1) старт, 2) пауза, 3) перезапис.",
        RunStarted = "Таймер запущено.",
        RunPaused = "Таймер на паузі: {0}.",
        RunOverwritten = "Таймер скинуто, {0} збережено як тимчасове значення.",
    };

    private static readonly MessageCatalog SpanishCatalog = new()
    {
        LanguageName = "Español",
        LanguageChanged = "Idioma cambiado a {0}.",
        PlayerOnly = "Este comando solo está disponible para jugadores.",
        TimerEnabled = "Cronómetro activado.",
        TimerDisabled = "Cronómetro desactivado.",
        ToggleDisabledByServer = "El servidor no permite activar o desactivar el cronómetro.",
        BindUsage = "Uso: !timerhud_bind <botón>. Usa !timerhud_bind none para quitar la asignación.",
        BindButtons = "Botones disponibles: {0}.",
        BindCurrent = "Botón actual: {0}.",
        BindMissing = "Todavía no hay ningún botón asignado.",
        BindUnknown = "Botón desconocido: {0}.",
        BindSet = "El botón {0} ahora controla el cronómetro.",
        BindCleared = "Botón del cronómetro eliminado.",
        BindCycle = "Las pulsaciones siguen este orden: 1) iniciar, 2) pausar, 3) sobrescribir.",
        RunStarted = "Cronómetro iniciado.",
        RunPaused = "Cronómetro pausado en {0}.",
        RunOverwritten = "Cronómetro reiniciado, {0} guardado como valor temporal.",
    };

    public static MessageCatalog For(Language language) => language switch
    {
        Language.Russian => RussianCatalog,
        Language.German => GermanCatalog,
        Language.Ukrainian => UkrainianCatalog,
        Language.Spanish => SpanishCatalog,
        _ => EnglishCatalog,
    };
}
