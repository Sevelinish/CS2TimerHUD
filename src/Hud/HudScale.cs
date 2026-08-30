namespace TimerHud.Hud;

public static class HudScale
{
    public const float BaseWorldUnitsPerPixel = 0.25f / 1050f;

    public static float WorldUnitsPerPixel(float fontSize, float scale) =>
        BaseWorldUnitsPerPixel * fontSize * scale;

    public static float LineHeight(float fontSize, float scale) =>
        fontSize * WorldUnitsPerPixel(fontSize, scale);

    public static float LineAngle(float fontSize, float scale, float distance) =>
        distance > 0f ? LineHeight(fontSize, scale) / distance : 0f;
}
