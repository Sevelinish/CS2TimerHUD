using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace TimerHud.Input;

public static class ButtonReader
{
    public static PlayerButtons Read(CCSPlayerPawn pawn)
    {
        if (!pawn.IsValid)
            return 0;

        var services = pawn.MovementServices;
        if (services is null || services.Handle == IntPtr.Zero)
            return 0;

        var states = services.Buttons.ButtonStates;
        return states.Length == 0 ? 0 : (PlayerButtons)states[0];
    }
}
