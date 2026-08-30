using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;
using TimerHud.Configuration;
using TimerHud.Hud;
using TimerHud.Input;
using TimerHud.Integration;
using TimerHud.Players;
using TimerHud.Timing;

namespace TimerHud;

[MinimumApiVersion(228)]
public sealed partial class TimerHudPlugin : BasePlugin, IPluginConfig<TimerHudConfig>
{
    private const string ViewModelDesignerName = "predicted_viewmodel";
    private const float PreferenceFlushInterval = 30f;
    private const int FailureLimit = 30;
    private const int SpawnBuildDelayTicks = 32;
    private const int SpawnProbeDelayTicks = 16;
    private const float DefaultTickInterval = 1f / 64f;

    private readonly SessionRegistry _sessions = new();
    private readonly HashSet<uint> _ownEntities = new();
    private readonly Dictionary<uint, uint> _viewModels = new();
    private readonly int[] _chatDispatchTick = Enumerable.Repeat(-1, SessionRegistry.MaxSlots).ToArray();

    private PreferenceStore _preferences = null!;
    private MovementHudProbe _probe = null!;
    private TimerTheme _theme = null!;
    private TimerGeometry _geometry = null!;
    private WorldTextRenderer _worldTextRenderer = null!;
    private CenterHtmlRenderer _centerHtmlRenderer = null!;
    private TimeFormatter _formatter = null!;
    private int _minimumRecordTicks;
    private int _probeDueTick;
    private int _tick;
    private bool _timingResolved;

    public override string ModuleName => "TimerHUD";

    public override string ModuleVersion => "1.0.2";

    public override string ModuleAuthor => "TimerHUD";

    public override string ModuleDescription =>
        "Run timer below the crosshair with a temporary previous value, bound to a single key.";

    public TimerHudConfig Config { get; set; } = new();

    public void OnConfigParsed(TimerHudConfig config)
    {
        config.Normalize();
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        _preferences = new PreferenceStore(Path.Combine(ModuleDirectory, "data", "preferences.json"), Logger);
        _preferences.Load();

        BuildPipeline();

        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
        RegisterListener<Listeners.OnClientDisconnectPost>(OnClientDisconnectPost);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);

        AddCommandListener("say", OnSayCommand, HookMode.Pre);
        AddCommandListener("say_team", OnSayCommand, HookMode.Pre);

        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);

        AddTimer(PreferenceFlushInterval, () => _preferences.Flush(), TimerFlags.REPEAT);
        AddTimer(Config.MovementHud.FileRefreshInterval, RefreshIntegrationFiles, TimerFlags.REPEAT);
        AddTimer(Config.MovementHud.EntityProbeInterval, ProbeIntegration, TimerFlags.REPEAT);

        if (hotReload)
            AdoptConnectedPlayers();

        Logger.LogInformation(
            "TimerHUD {Version} loaded. Commands: {Commands}. MovementHUD detection: {Detection}, config found: {Found}.",
            ModuleVersion, RegisteredCommands, _probe.Detection.ToConfigValue(), _probe.ConfigLoaded);
    }

    public override void Unload(bool hotReload)
    {
        foreach (var session in _sessions.Snapshot())
            SafeClearHud(session);

        _sessions.Clear();
        _preferences.Flush(force: true);
    }

    private void BuildPipeline()
    {
        _theme = new TimerTheme(Config.Theme);
        _probe = new MovementHudProbe(Config, ModuleDirectory, Logger);
        _geometry = new TimerGeometry(Config, _theme);
        _worldTextRenderer = new WorldTextRenderer(Config, _theme, _geometry, _probe, Logger);
        _centerHtmlRenderer = new CenterHtmlRenderer(_theme);

        ApplyTiming(DefaultTickInterval);
        RefreshIntegrationFiles();
    }

    private void ApplyTiming(float tickInterval)
    {
        _formatter = new TimeFormatter(tickInterval, Config.TimePrecision, Config.AlwaysShowHours);
        _minimumRecordTicks = _formatter.TicksFromSeconds(Config.MinimumRecordSeconds);
    }

    private void ResolveTiming()
    {
        if (_timingResolved)
            return;

        _timingResolved = true;

        try
        {
            var interval = Server.TickInterval;

            if (interval > 0f && MathF.Abs(interval - DefaultTickInterval) > 0.0001f)
                ApplyTiming(interval);
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception,
                "TimerHUD: could not read the server tick interval, {Interval} is used.", DefaultTickInterval);
        }
    }

    private ITimerRenderer RendererFor(TimerRenderMode mode) =>
        mode == TimerRenderMode.CenterHtml ? _centerHtmlRenderer : _worldTextRenderer;

    private void OnClientPutInServer(int playerSlot)
    {
        var controller = Utilities.GetPlayerFromSlot(playerSlot);
        if (controller is null || !controller.IsValid || controller.IsBot || controller.IsHLTV)
            return;

        _sessions.Add(new TimerSession(controller, _preferences.GetOrCreate(controller.SteamID), Config));
    }

    private void OnClientDisconnectPost(int playerSlot)
    {
        var session = _sessions.Remove(playerSlot);
        if (session is null)
            return;

        session.Lines.Clear();
        _preferences.Flush();
    }

    private void AdoptConnectedPlayers()
    {
        foreach (var controller in Utilities.GetPlayers())
        {
            if (!controller.IsValid || controller.IsBot || controller.IsHLTV)
                continue;

            if (_sessions.Get(controller.Slot) is null)
                _sessions.Add(new TimerSession(controller, _preferences.GetOrCreate(controller.SteamID), Config));
        }
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (@event.Userid is { } player && _sessions.Get(player.Slot) is { } session)
        {
            session.NeedsViewModelRecheck = true;
            session.NextWorldTextAttemptTick = _tick + SpawnBuildDelayTicks;
            session.ResetInput();
        }

        if (_probeDueTick == 0)
            _probeDueTick = _tick + SpawnProbeDelayTicks;

        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (@event.Userid is { } player && _sessions.Get(player.Slot) is { } session)
        {
            SafeClearHud(session);
            session.ResetInput();
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        var enumerator = _sessions.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var session = enumerator.Current;
            session.NeedsViewModelRecheck = true;
            session.NextWorldTextAttemptTick = _tick + SpawnBuildDelayTicks;
        }

        if (_probeDueTick == 0)
            _probeDueTick = _tick + SpawnProbeDelayTicks;

        return HookResult.Continue;
    }

    private void OnMapStart(string mapName)
    {
        _tick = Server.TickCount;
        _viewModels.Clear();

        foreach (var session in _sessions.Snapshot())
        {
            session.Lines.Clear();
            session.WorldTextViewModelIndex = 0;
            session.ProbeViewModelIndex = 0;
            session.ActiveRenderMode = null;
            session.NextWorldTextAttemptTick = 0;
            session.NextRenderTick = 0;
            session.ResetInput();
            session.InvalidateRenderCache();

            if (Config.ResetOnMapChange)
                session.Timer.Reset(keepPrevious: false);
            else
                session.Timer.Rebase(_tick);
        }
    }

    private void OnMapEnd()
    {
        _viewModels.Clear();

        foreach (var session in _sessions.Snapshot())
        {
            session.Lines.Clear();
            session.WorldTextViewModelIndex = 0;
            session.ProbeViewModelIndex = 0;
            session.ActiveRenderMode = null;
            session.InvalidateRenderCache();
        }

        _preferences.Flush();
    }

    private void RefreshIntegrationFiles()
    {
        try
        {
            _probe.RefreshFiles();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "TimerHUD: MovementHUD file refresh failed.");
        }
    }

    private void ProbeIntegration()
    {
        if (_probe.Detection != MovementHudDetection.Entities)
            return;

        _ownEntities.Clear();

        var sessions = _sessions.GetEnumerator();
        while (sessions.MoveNext())
        {
            foreach (var line in sessions.Current.Lines)
            {
                if (line.Entity.IsValid)
                    _ownEntities.Add(line.Entity.Index);
            }
        }

        _probe.ScanEntities(_ownEntities);
        RefreshViewModelIndices();
    }

    private void RefreshViewModelIndices()
    {
        _viewModels.Clear();

        foreach (var viewModel in Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(ViewModelDesignerName))
        {
            if (!viewModel.IsValid)
                continue;

            var owner = viewModel.OwnerEntity.Value;
            if (owner is null || !owner.IsValid)
                continue;

            _viewModels[owner.Index] = viewModel.Index;
        }

        var sessions = _sessions.GetEnumerator();
        while (sessions.MoveNext())
        {
            var session = sessions.Current;
            var controller = session.Controller;

            if (!controller.IsValid)
                continue;

            var pawn = controller.PlayerPawn.Value;
            if (pawn is null || !pawn.IsValid)
            {
                session.ProbeViewModelIndex = 0;
                continue;
            }

            session.ProbeViewModelIndex = _viewModels.TryGetValue(pawn.Index, out var index)
                ? index
                : _viewModels.TryGetValue(controller.Index, out var byController) ? byController : 0u;
        }
    }

    private void OnTick()
    {
        ResolveTiming();

        _tick = Server.TickCount;

        if (_probeDueTick != 0 && _tick >= _probeDueTick)
        {
            _probeDueTick = 0;
            ProbeIntegration();
        }

        if (!Config.Enabled)
            return;

        var enumerator = _sessions.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var session = enumerator.Current;

            try
            {
                UpdateSession(session);
            }
            catch (Exception exception)
            {
                HandleSessionFailure(session, exception);
            }
        }
    }

    private void UpdateSession(TimerSession session)
    {
        var controller = session.Controller;

        if (!controller.IsValid)
        {
            _sessions.Remove(session.Slot);
            return;
        }

        if (!session.IsEnabled(Config))
        {
            session.ResetInput();

            if (session.ActiveRenderMode is not null || session.Lines.Count > 0)
                SafeClearHud(session);

            return;
        }

        var pawn = controller.PawnIsAlive ? controller.PlayerPawn.Value : null;

        if (pawn is null || !pawn.IsValid)
        {
            session.ResetInput();

            if (session.ActiveRenderMode is not null || session.Lines.Count > 0)
                SafeClearHud(session);

            return;
        }

        PollInput(session, pawn);
        UpdateStacking(session);

        var mode = session.ResolveMode(Config);
        var renderer = RendererFor(mode);

        if (_tick < session.NextRenderTick && !session.LayoutDirty && session.ActiveRenderMode == mode)
            return;

        session.NextRenderTick = _tick + Config.UpdateInterval;

        var view = BuildView(session);

        if (renderer.Render(session, view, _tick))
        {
            if (mode == TimerRenderMode.WorldText && session.ActiveRenderMode == TimerRenderMode.CenterHtml)
                _centerHtmlRenderer.Clear(session);

            session.ActiveRenderMode = mode;
            session.ConsecutiveFailures = 0;
            return;
        }

        if (mode != TimerRenderMode.WorldText || !Config.FallbackToCenterHtml)
            return;

        if (_centerHtmlRenderer.Render(session, view, _tick))
            session.ActiveRenderMode = TimerRenderMode.CenterHtml;
    }

    private void PollInput(TimerSession session, CCSPlayerPawn pawn)
    {
        var bound = session.BoundButton;

        if (bound == 0)
        {
            session.ResetInput();
            return;
        }

        var held = ButtonReader.Read(pawn);

        if (!session.HasButtonSample)
        {
            session.LastButtons = held;
            session.HasButtonSample = true;
            return;
        }

        var pressed = held & ~session.LastButtons;
        session.LastButtons = held;

        if ((pressed & bound) == 0)
            return;

        var action = session.Timer.Advance(_tick, _minimumRecordTicks);
        session.NextRenderTick = 0;

        if (Config.AnnounceActions)
            Announce(session, action);
    }

    private void UpdateStacking(TimerSession session)
    {
        var stacked = _probe.IsStacked(session.SteamId, session.ProbeViewModelIndex);

        if (session.HasLayoutSample && stacked == session.Stacked)
            return;

        session.Stacked = stacked;
        session.HasLayoutSample = true;
        session.LayoutDirty = true;
    }

    private TimerView BuildView(TimerSession session)
    {
        var timer = session.Timer;
        var main = _formatter.Format(timer.ElapsedTicks(_tick));

        var previous = timer.PreviousTicks is { } previousTicks
            ? _theme.PreviousLabel + _formatter.Format(previousTicks)
            : string.Empty;

        return new TimerView(main, _theme.ColorFor(timer.State), previous, _theme.Previous);
    }

    private void Announce(TimerSession session, TimerAction action)
    {
        if (!session.Controller.IsValid)
            return;

        var messages = session.Messages(Config);

        var text = action switch
        {
            TimerAction.Started => messages.RunStarted,
            TimerAction.Paused => Format(messages.RunPaused, _formatter.Format(session.Timer.ElapsedTicks(_tick))),
            _ => Format(messages.RunOverwritten,
                _formatter.Format(session.Timer.PreviousTicks ?? 0)),
        };

        session.Controller.PrintToChat(Decorate(text));
    }

    private void ClearHud(TimerSession session)
    {
        _worldTextRenderer.Clear(session);

        if (session.ActiveRenderMode == TimerRenderMode.CenterHtml)
            _centerHtmlRenderer.Clear(session);

        session.ActiveRenderMode = null;
        session.InvalidateRenderCache();
    }

    private void SafeClearHud(TimerSession session)
    {
        try
        {
            ClearHud(session);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "TimerHUD: failed to clear the HUD for slot {Slot}.", session.Slot);
            session.Lines.Clear();
            session.ActiveRenderMode = null;
        }
    }

    private void HandleSessionFailure(TimerSession session, Exception exception)
    {
        session.ConsecutiveFailures++;

        if (session.ConsecutiveFailures <= 3)
            Logger.LogError(exception, "TimerHUD: render error for slot {Slot}.", session.Slot);

        if (session.ConsecutiveFailures < FailureLimit)
            return;

        Logger.LogError(
            "TimerHUD: the HUD of slot {Slot} was suspended after {Count} consecutive errors. " +
            "Personal settings are untouched and !timerhud brings it back.",
            session.Slot, session.ConsecutiveFailures);

        session.SuspendedByErrors = true;
        SafeClearHud(session);
    }
}
