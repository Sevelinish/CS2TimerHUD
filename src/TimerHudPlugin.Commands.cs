using System.Globalization;
using System.Reflection;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using TimerHud.Input;
using TimerHud.Integration;
using TimerHud.Localization;
using TimerHud.Players;

namespace TimerHud;

public sealed partial class TimerHudPlugin
{
    private const char ColorDefault = (char)1;
    private const char ColorRed = (char)2;
    private const char ColorGreen = (char)4;
    private const string CommandPrefix = "css_";

    private static string RegisteredCommands { get; } = string.Join(", ", CommandNames);

    private static IEnumerable<string> CommandNames => typeof(TimerHudPlugin)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .SelectMany(method => method.GetCustomAttributes<ConsoleCommandAttribute>())
        .Select(attribute => attribute.Command)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(command => command, StringComparer.Ordinal);

    [ConsoleCommand("css_timerhud_bind", "Bind a button that starts, pauses and overwrites the timer")]
    [CommandHelper(minArgs: 0, usage: "<button>   (example: reload)", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnBindCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!TryBeginCommand(player, command, out var session))
            return;

        ExecuteBind(session, command.ArgCount > 1 ? command.GetArg(1) : string.Empty,
            message => command.ReplyToCommand(message));
    }

    [ConsoleCommand("css_timerhud", "Toggle the timer display")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnToggleCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!TryBeginCommand(player, command, out var session))
            return;

        ExecuteToggle(session, message => command.ReplyToCommand(message));
    }

    [ConsoleCommand("css_timerhud_engl", "Switch the timer messages to English")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnLanguageEnglishCommand(CCSPlayerController? player, CommandInfo command) =>
        SetLanguage(player, command, Language.English);

    [ConsoleCommand("css_timerhud_rus", "Переключить сообщения таймера на русский")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnLanguageRussianCommand(CCSPlayerController? player, CommandInfo command) =>
        SetLanguage(player, command, Language.Russian);

    [ConsoleCommand("css_timerhud_ukr", "Переключити повідомлення таймера на українську")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnLanguageUkrainianCommand(CCSPlayerController? player, CommandInfo command) =>
        SetLanguage(player, command, Language.Ukrainian);

    [ConsoleCommand("css_timerhud_spanish", "Cambiar los mensajes del cronómetro al español")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnLanguageSpanishCommand(CCSPlayerController? player, CommandInfo command) =>
        SetLanguage(player, command, Language.Spanish);

    [ConsoleCommand("css_timerhud_deutsch", "Timer-Meldungen auf Deutsch umstellen")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnLanguageGermanCommand(CCSPlayerController? player, CommandInfo command) =>
        SetLanguage(player, command, Language.German);

    [ConsoleCommand("css_timerhud_debug", "TimerHUD diagnostics")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnDebugCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player is not null && AlreadyDispatchedFromChat(player, command))
            return;

        ExecuteDebug(message => command.ReplyToCommand(message));
    }

    private void SetLanguage(CCSPlayerController? player, CommandInfo command, Language language)
    {
        if (!TryBeginCommand(player, command, out var session))
            return;

        ExecuteLanguage(session, language, message => command.ReplyToCommand(message));
    }

    private HookResult OnSayCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null || !player.IsValid)
            return HookResult.Continue;

        var raw = command.ArgString;
        if (string.IsNullOrWhiteSpace(raw))
            return HookResult.Continue;

        var message = raw.Trim();

        if (message.Length < 2 || message[0] == '"' || !Config.ChatTriggers.Contains(message[0]))
            return HookResult.Continue;

        var payload = message[1..].Trim();
        if (payload.Length == 0)
            return HookResult.Continue;

        var separator = payload.IndexOf(' ');
        var name = separator < 0 ? payload : payload[..separator];
        var argument = separator < 0 ? string.Empty : payload[(separator + 1)..].Trim();

        DispatchFromChat(player, CommandPrefix + name.ToLowerInvariant(), argument);
        return HookResult.Continue;
    }

    private void DispatchFromChat(CCSPlayerController player, string command, string argument)
    {
        if (command is "css_timerhud_debug")
        {
            if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
                return;

            MarkChatDispatch(player.Slot);
            ExecuteDebug(message => player.PrintToChat(message));
            return;
        }

        var language = command switch
        {
            "css_timerhud_engl" => Language.English,
            "css_timerhud_rus" => Language.Russian,
            "css_timerhud_ukr" => Language.Ukrainian,
            "css_timerhud_spanish" => Language.Spanish,
            "css_timerhud_deutsch" => Language.German,
            _ => (Language?)null,
        };

        if (command is not ("css_timerhud" or "css_timerhud_bind") && language is null)
            return;

        var session = ResolveSession(player);
        if (session is null)
            return;

        MarkChatDispatch(player.Slot);

        void Reply(string text) => player.PrintToChat(text);

        if (language is { } value)
            ExecuteLanguage(session, value, Reply);
        else if (command is "css_timerhud_bind")
            ExecuteBind(session, argument, Reply);
        else
            ExecuteToggle(session, Reply);
    }

    private void ExecuteBind(TimerSession session, string argument, Action<string> reply)
    {
        var messages = session.Messages(Config);

        if (string.IsNullOrWhiteSpace(argument))
        {
            Reply(reply, messages.BindUsage);
            Reply(reply, session.BoundButton == 0
                ? messages.BindMissing
                : Format(messages.BindCurrent, ButtonCatalog.NameOf(session.BoundButton)));
            Reply(reply, Format(messages.BindButtons, ButtonCatalog.Names));
            return;
        }

        if (ButtonCatalog.IsClearKeyword(argument))
        {
            session.BoundButton = 0;
            session.ResetInput();
            session.Preferences.Button = "none";
            _preferences.MarkDirty();

            Reply(reply, messages.BindCleared, ColorRed);
            return;
        }

        if (!ButtonCatalog.TryParse(argument, out var button))
        {
            Reply(reply, Format(messages.BindUnknown, argument.Trim()), ColorRed);
            Reply(reply, Format(messages.BindButtons, ButtonCatalog.Names));
            return;
        }

        session.BoundButton = button;
        session.ResetInput();
        session.Preferences.Button = ButtonCatalog.NameOf(button);
        _preferences.MarkDirty();

        Reply(reply, Format(messages.BindSet, ButtonCatalog.NameOf(button)), ColorGreen);
        Reply(reply, messages.BindCycle);
    }

    private void ExecuteToggle(TimerSession session, Action<string> reply)
    {
        var messages = session.Messages(Config);

        if (!Config.AllowPlayerToggle)
        {
            Reply(reply, messages.ToggleDisabledByServer);
            return;
        }

        var enabled = !session.IsEnabled(Config);
        session.Preferences.Enabled = enabled;
        _preferences.MarkDirty();

        if (enabled)
        {
            session.SuspendedByErrors = false;
            session.ConsecutiveFailures = 0;
            session.NextWorldTextAttemptTick = 0;
            session.InvalidateRenderCache();
        }
        else
        {
            SafeClearHud(session);
        }

        Reply(reply,
            enabled ? messages.TimerEnabled : messages.TimerDisabled,
            enabled ? ColorGreen : ColorRed);
    }

    private void ExecuteLanguage(TimerSession session, Language language, Action<string> reply)
    {
        session.Preferences.Language = language.ToConfigValue();
        _preferences.MarkDirty();

        var messages = Translations.For(language);
        Reply(reply, Format(messages.LanguageChanged, messages.LanguageName), ColorGreen);
    }

    private void ExecuteDebug(Action<string> reply)
    {
        reply($"TimerHUD {ModuleVersion} | sessions: {_sessions.Count} | tick: {_tick}");
        reply($"config: enabled={Config.Enabled} mode={Config.DefaultRenderMode} interval={Config.UpdateInterval} " +
              $"precision={Config.TimePrecision} default_button={Config.DefaultButton} text={Config.TextUpdateMode}");
        reply($"movement hud: detection={_probe.Detection.ToConfigValue()} config_loaded={_probe.ConfigLoaded} " +
              $"occupied={_probe.OccupiedViewModels}");
        reply($"  config: {_probe.ConfigPath}");
        reply($"  preferences: {_probe.PreferencesPath}");

        foreach (var session in _sessions.Snapshot())
        {
            var name = session.Controller.IsValid ? session.Controller.PlayerName : "<invalid>";
            reply($"  [{session.Slot}] {name}: enabled={session.IsEnabled(Config)} " +
                  $"button={ButtonCatalog.NameOf(session.BoundButton)} state={session.Timer.State} " +
                  $"stacked={session.Stacked} lang={session.ResolveLanguage(Config).ToConfigValue()} " +
                  $"active={session.ActiveRenderMode?.ToString() ?? "none"} vm={session.WorldTextViewModelIndex} " +
                  $"probe_vm={session.ProbeViewModelIndex} lines={session.Lines.Count} " +
                  $"fails={session.ConsecutiveFailures}");
        }
    }

    private bool TryBeginCommand(CCSPlayerController? player, CommandInfo command, out TimerSession session)
    {
        session = null!;

        var resolved = ResolveSession(player);

        if (resolved is null)
        {
            command.ReplyToCommand(Translations.For(LanguageParser.Parse(Config.DefaultLanguage)).PlayerOnly);
            return false;
        }

        if (AlreadyDispatchedFromChat(resolved.Controller, command))
            return false;

        session = resolved;
        return true;
    }

    private TimerSession? ResolveSession(CCSPlayerController? player)
    {
        if (player is null || !player.IsValid)
            return null;

        var found = _sessions.Get(player.Slot);

        if (found is null)
        {
            found = new TimerSession(player, _preferences.GetOrCreate(player.SteamID), Config);
            _sessions.Add(found);
        }

        return found;
    }

    private void MarkChatDispatch(int slot)
    {
        if ((uint)slot < SessionRegistry.MaxSlots)
            _chatDispatchTick[slot] = _tick;
    }

    private bool AlreadyDispatchedFromChat(CCSPlayerController player, CommandInfo command) =>
        command.CallingContext == CommandCallingContext.Chat
        && (uint)player.Slot < SessionRegistry.MaxSlots
        && _chatDispatchTick[player.Slot] == _tick;

    private static string Format(string template, params object[] arguments) =>
        string.Format(CultureInfo.InvariantCulture, template, arguments);

    private static string Decorate(string message, char accent = ColorDefault) =>
        $" {ColorGreen}[TimerHUD]{ColorDefault} {accent}{message}{ColorDefault}";

    private static void Reply(Action<string> reply, string message, char accent = ColorDefault) =>
        reply(Decorate(message, accent));
}
