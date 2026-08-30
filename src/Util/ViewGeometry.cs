using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace TimerHud.Util;

public static class ViewGeometry
{
    private const float DegreesToRadians = MathF.PI / 180f;
    private const float FallbackEyeHeight = 64f;

    public static void AngleVectors(QAngle angles, out Vector forward, out Vector right, out Vector up)
    {
        var pitch = angles.X * DegreesToRadians;
        var yaw = angles.Y * DegreesToRadians;
        var roll = angles.Z * DegreesToRadians;

        float sp = MathF.Sin(pitch), cp = MathF.Cos(pitch);
        float sy = MathF.Sin(yaw), cy = MathF.Cos(yaw);
        float sr = MathF.Sin(roll), cr = MathF.Cos(roll);

        forward = new Vector(cp * cy, cp * sy, -sp);

        right = new Vector(
            -sr * sp * cy + cr * sy,
            -sr * sp * sy - cr * cy,
            -sr * cp);

        up = new Vector(
            cr * sp * cy + sr * sy,
            cr * sp * sy - sr * cy,
            cr * cp);
    }

    public static Vector EyePosition(CCSPlayerPawn pawn)
    {
        var origin = pawn.AbsOrigin;
        if (origin is null)
            return new Vector(0f, 0f, 0f);

        var eyeHeight = pawn.CameraServices?.OldPlayerViewOffsetZ ?? FallbackEyeHeight;
        if (eyeHeight <= 0f)
            eyeHeight = FallbackEyeHeight;

        return new Vector(origin.X, origin.Y, origin.Z + eyeHeight);
    }

    public static Vector ScreenPoint(
        Vector eye,
        QAngle eyeAngles,
        float distance,
        float offsetRight,
        float offsetUp)
    {
        AngleVectors(eyeAngles, out var forward, out var right, out var up);

        return new Vector(
            eye.X + forward.X * distance + right.X * offsetRight + up.X * offsetUp,
            eye.Y + forward.Y * distance + right.Y * offsetRight + up.Y * offsetUp,
            eye.Z + forward.Z * distance + right.Z * offsetRight + up.Z * offsetUp);
    }

    public static QAngle FacingAngles(QAngle eyeAngles) =>
        new(0f, eyeAngles.Y + 270f, 90f - eyeAngles.X);
}
