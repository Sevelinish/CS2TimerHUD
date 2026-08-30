using System.Reflection;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace TimerHud.Util;

public static class PawnView
{
    private static readonly PropertyInfo? EyeAnglesProperty =
        typeof(CCSPlayerPawn).GetProperty("EyeAngles", BindingFlags.Public | BindingFlags.Instance);

    public static QAngle EyeAngles(CCSPlayerPawn pawn) =>
        EyeAnglesProperty?.GetValue(pawn) as QAngle ?? new QAngle(0f, 0f, 0f);
}
