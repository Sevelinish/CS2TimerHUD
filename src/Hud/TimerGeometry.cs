using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using TimerHud.Configuration;
using TimerHud.Integration;
using TimerHud.Util;

namespace TimerHud.Hud;

public readonly record struct TimerFrame(
    Vector MainPosition,
    Vector PreviousPosition,
    QAngle Facing,
    float MainUnitsPerPixel,
    float PreviousUnitsPerPixel);

public sealed class TimerGeometry
{
    private readonly TimerHudConfig _config;
    private readonly TimerTheme _theme;

    public TimerGeometry(TimerHudConfig config, TimerTheme theme)
    {
        _config = config;
        _theme = theme;
    }

    public TimerFrame Compute(CCSPlayerPawn pawn, MovementHudPlacement? stacked)
    {
        var layout = _config.Layout;
        var distance = layout.Distance;
        var scale = stacked is null ? layout.SoloScale : layout.StackedScale;

        var mainUnitsPerPixel = HudScale.WorldUnitsPerPixel(_theme.FontSize, scale);
        var previousUnitsPerPixel = HudScale.WorldUnitsPerPixel(_theme.PreviousFontSize, scale);

        var mainAngle = HudScale.LineAngle(_theme.FontSize, scale, distance);
        var previousAngle = HudScale.LineAngle(_theme.PreviousFontSize, scale, distance);
        var lineGap = layout.RowSpacing * 0.5f * (mainAngle + previousAngle);

        var previousCenter = stacked is { } placement
            ? placement.TopEdgeAngle + layout.StackGap * mainAngle + previousAngle * 0.5f
            : layout.SoloOffsetY * mainAngle - lineGap * 0.5f;

        var mainCenter = previousCenter + lineGap;
        var right = layout.OffsetX * mainAngle * distance;

        var eyeAngles = PawnView.EyeAngles(pawn);
        var eye = ViewGeometry.EyePosition(pawn);

        return new TimerFrame(
            ViewGeometry.ScreenPoint(eye, eyeAngles, distance, right, mainCenter * distance),
            ViewGeometry.ScreenPoint(eye, eyeAngles, distance, right, previousCenter * distance),
            ViewGeometry.FacingAngles(eyeAngles),
            mainUnitsPerPixel,
            previousUnitsPerPixel);
    }
}
