using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using TimerHud.Configuration;
using TimerHud.Hud;
using TimerHud.Input;
using TimerHud.Localization;
using TimerHud.Timing;

namespace TimerHud.Players;

public sealed class TimerSession
{
    public TimerSession(CCSPlayerController controller, PlayerPreferences preferences, TimerHudConfig config)
    {
        Controller = controller;
        Slot = controller.Slot;
        SteamId = controller.SteamID;
        Preferences = preferences;
        BoundButton = ButtonCatalog.Resolve(preferences.Button ?? config.DefaultButton);
    }

    public CCSPlayerController Controller { get; }
    public int Slot { get; }
    public ulong SteamId { get; }
    public PlayerPreferences Preferences { get; }
    public RunTimer Timer { get; } = new();
    public PlayerButtons BoundButton { get; set; }
    public PlayerButtons LastButtons { get; set; }
    public bool HasButtonSample { get; set; }
    public List<TimerLine> Lines { get; } = new(2);
    public int NextWorldTextAttemptTick { get; set; }
    public int NextRenderTick { get; set; }
    public uint WorldTextViewModelIndex { get; set; }
    public uint ProbeViewModelIndex { get; set; }
    public bool NeedsViewModelRecheck { get; set; }
    public bool LayoutDirty { get; set; } = true;
    public bool Stacked { get; set; }
    public bool HasLayoutSample { get; set; }
    public TimerRenderMode? ActiveRenderMode { get; set; }
    public int ConsecutiveFailures { get; set; }
    public bool SuspendedByErrors { get; set; }

    public bool IsEnabled(TimerHudConfig config) =>
        !SuspendedByErrors && (Preferences.Enabled ?? config.EnabledByDefault);

    public TimerRenderMode ResolveMode(TimerHudConfig config) =>
        TimerRenderModeParser.Parse(config.DefaultRenderMode);

    public Language ResolveLanguage(TimerHudConfig config) =>
        LanguageParser.Parse(Preferences.Language ?? config.DefaultLanguage);

    public MessageCatalog Messages(TimerHudConfig config) => Translations.For(ResolveLanguage(config));

    public void ResetInput()
    {
        LastButtons = 0;
        HasButtonSample = false;
    }

    public void InvalidateRenderCache()
    {
        LayoutDirty = true;

        foreach (var line in Lines)
        {
            line.LastText = null;
            line.LastColor = null;
            line.LastEnabled = null;
        }
    }
}

public sealed class TimerLine
{
    public TimerLine(CPointWorldText entity)
    {
        Entity = entity;
    }

    public CPointWorldText Entity { get; }
    public string? LastText { get; set; }
    public Color? LastColor { get; set; }
    public bool? LastEnabled { get; set; }
}
