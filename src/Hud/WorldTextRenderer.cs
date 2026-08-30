using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using TimerHud.Configuration;
using TimerHud.Integration;
using TimerHud.Players;
using TimerHud.Util;

namespace TimerHud.Hud;

public sealed class WorldTextRenderer : ITimerRenderer
{
    private const string EntityClassName = "point_worldtext";
    private const int RetryDelayTicks = 16;
    private const int MainLine = 0;
    private const int PreviousLine = 1;

    private readonly TimerTheme _theme;
    private readonly TimerGeometry _geometry;
    private readonly MovementHudProbe _probe;
    private readonly ILogger _logger;
    private readonly bool _schemaText;
    private readonly bool _inputText;
    private bool _inputTextBroken;

    public WorldTextRenderer(
        TimerHudConfig config,
        TimerTheme theme,
        TimerGeometry geometry,
        MovementHudProbe probe,
        ILogger logger)
    {
        _theme = theme;
        _geometry = geometry;
        _probe = probe;
        _logger = logger;

        var mode = config.TextUpdateMode.Trim().ToLowerInvariant();
        _schemaText = mode is not "input";
        _inputText = mode is not "schema";
    }

    public TimerRenderMode Mode => TimerRenderMode.WorldText;

    public bool RequiresContinuousRefresh => false;

    public bool Render(TimerSession session, TimerView view, int tick)
    {
        if (!EnsureLines(session, tick))
            return false;

        if (session.LayoutDirty && !ApplyLayout(session, tick))
            return false;

        var main = session.Lines[MainLine];
        ApplyEnabled(main, true);
        ApplyText(main, view.MainText);
        ApplyColor(main, view.MainColor);

        var previous = session.Lines[PreviousLine];
        ApplyEnabled(previous, view.HasPrevious);

        if (view.HasPrevious)
        {
            ApplyText(previous, view.PreviousText);
            ApplyColor(previous, view.PreviousColor);
        }

        return true;
    }

    public void Clear(TimerSession session) => DestroyLines(session);

    private bool EnsureLines(TimerSession session, int tick)
    {
        if (session.Lines.Count == 2)
        {
            foreach (var line in session.Lines)
            {
                if (line.Entity.IsValid)
                    continue;

                DestroyLines(session);
                session.NextWorldTextAttemptTick = tick + RetryDelayTicks;
                return false;
            }

            return !session.NeedsViewModelRecheck || Revalidate(session, tick);
        }

        if (tick < session.NextWorldTextAttemptTick)
            return false;

        return Build(session, tick);
    }

    private bool Revalidate(TimerSession session, int tick)
    {
        var controller = session.Controller;
        if (!controller.IsValid || !controller.PawnIsAlive)
            return true;

        var viewModel = ViewModelLocator.Find(controller);
        if (viewModel is null)
            return true;

        session.NeedsViewModelRecheck = false;

        if (viewModel.Index == session.WorldTextViewModelIndex)
            return true;

        foreach (var line in session.Lines)
            line.Entity.AcceptInput("SetParent", viewModel, line.Entity, "!activator");

        session.WorldTextViewModelIndex = viewModel.Index;
        session.LayoutDirty = true;
        return ApplyLayout(session, tick);
    }

    private bool Build(TimerSession session, int tick)
    {
        var controller = session.Controller;
        if (!controller.IsValid || !controller.PawnIsAlive)
        {
            session.NextWorldTextAttemptTick = tick + RetryDelayTicks;
            return false;
        }

        var pawn = controller.PlayerPawn.Value;
        var viewModel = ViewModelLocator.Find(controller);

        if (pawn is null || !pawn.IsValid || viewModel is null)
        {
            session.NextWorldTextAttemptTick = tick + RetryDelayTicks;
            return false;
        }

        var frame = _geometry.Compute(pawn, PlacementFor(session));

        var main = CreateLine(_theme.FontSize, frame.MainUnitsPerPixel, _theme.Idle, viewModel,
            frame.MainPosition, frame.Facing);

        var previous = main is null
            ? null
            : CreateLine(_theme.PreviousFontSize, frame.PreviousUnitsPerPixel, _theme.Previous, viewModel,
                frame.PreviousPosition, frame.Facing);

        if (main is null || previous is null)
        {
            main?.Remove();

            _logger.LogWarning("TimerHUD: could not create {ClassName} for slot {Slot}.", EntityClassName, session.Slot);

            DestroyLines(session);
            session.NextWorldTextAttemptTick = tick + RetryDelayTicks;
            return false;
        }

        session.Lines.Add(new TimerLine(main));
        session.Lines.Add(new TimerLine(previous));
        session.WorldTextViewModelIndex = viewModel.Index;
        session.NeedsViewModelRecheck = false;
        session.LayoutDirty = false;
        return true;
    }

    private bool ApplyLayout(TimerSession session, int tick)
    {
        var controller = session.Controller;
        var pawn = controller.IsValid ? controller.PlayerPawn.Value : null;

        if (pawn is null || !pawn.IsValid)
            return false;

        var frame = _geometry.Compute(pawn, PlacementFor(session));

        ApplyLinePlacement(session.Lines[MainLine], frame.MainPosition, frame.Facing, frame.MainUnitsPerPixel);
        ApplyLinePlacement(session.Lines[PreviousLine], frame.PreviousPosition, frame.Facing,
            frame.PreviousUnitsPerPixel);

        session.LayoutDirty = false;
        return true;
    }

    private MovementHudPlacement? PlacementFor(TimerSession session) =>
        session.Stacked ? _probe.Placement(session.SteamId) : null;

    private static void ApplyLinePlacement(TimerLine line, Vector position, QAngle facing, float unitsPerPixel)
    {
        var entity = line.Entity;
        if (!entity.IsValid)
            return;

        entity.WorldUnitsPerPx = unitsPerPixel;
        Utilities.SetStateChanged(entity, "CPointWorldText", "m_flWorldUnitsPerPx");
        entity.Teleport(position, facing, null);
    }

    private CPointWorldText? CreateLine(
        float fontSize,
        float unitsPerPixel,
        Color color,
        CBaseEntity viewModel,
        Vector position,
        QAngle facing)
    {
        var entity = Utilities.CreateEntityByName<CPointWorldText>(EntityClassName);
        if (entity is null || !entity.IsValid)
            return null;

        entity.MessageText = " ";
        entity.Enabled = true;
        entity.FontName = _theme.FontName;
        entity.FontSize = fontSize;
        entity.Color = color;
        entity.WorldUnitsPerPx = unitsPerPixel;

        entity.Fullbright = true;
        entity.DepthOffset = 0f;

        entity.DrawBackground = _theme.DrawBackground;
        entity.BackgroundBorderWidth = _theme.BackgroundPaddingX;
        entity.BackgroundBorderHeight = _theme.BackgroundPaddingY;

        entity.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
        entity.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;
        entity.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;

        entity.DispatchSpawn();

        entity.AcceptInput("SetParent", viewModel, entity, "!activator");
        entity.Teleport(position, facing, null);

        return entity;
    }

    private void ApplyText(TimerLine line, string text)
    {
        if (line.LastText == text)
            return;

        var entity = line.Entity;

        if (_inputText && !_inputTextBroken)
        {
            try
            {
                entity.AcceptInput("SetMessage", null, null, text);
            }
            catch (Exception exception)
            {
                _inputTextBroken = true;
                _logger.LogWarning(exception,
                    "TimerHUD: the SetMessage input is unavailable, the schema path is used instead.");
            }
        }

        if (_schemaText || _inputTextBroken)
        {
            entity.MessageText = text;
            Utilities.SetStateChanged(entity, "CPointWorldText", "m_messageText");
        }

        line.LastText = text;
    }

    private static void ApplyColor(TimerLine line, Color color)
    {
        if (line.LastColor is { } last && last.ToArgb() == color.ToArgb())
            return;

        line.Entity.Color = color;
        Utilities.SetStateChanged(line.Entity, "CPointWorldText", "m_Color");
        line.LastColor = color;
    }

    private static void ApplyEnabled(TimerLine line, bool enabled)
    {
        if (line.LastEnabled == enabled)
            return;

        line.Entity.Enabled = enabled;
        Utilities.SetStateChanged(line.Entity, "CPointWorldText", "m_bEnabled");
        line.LastEnabled = enabled;
    }

    private static void DestroyLines(TimerSession session)
    {
        if (session.Lines.Count == 0)
        {
            session.WorldTextViewModelIndex = 0;
            return;
        }

        foreach (var line in session.Lines)
        {
            if (line.Entity.IsValid)
                line.Entity.Remove();
        }

        session.Lines.Clear();
        session.WorldTextViewModelIndex = 0;
        session.LayoutDirty = true;
    }
}
