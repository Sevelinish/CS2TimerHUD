using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace TimerHud.Util;

public static class ViewModelLocator
{
    private const string ViewModelDesignerName = "predicted_viewmodel";

    public static CBaseEntity? Find(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid)
            return null;

        var pawnIndex = pawn.Index;
        var controllerIndex = player.Index;

        foreach (var viewModel in Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(ViewModelDesignerName))
        {
            if (!viewModel.IsValid)
                continue;

            var owner = viewModel.OwnerEntity.Value;
            if (owner is null || !owner.IsValid)
                continue;

            if (owner.Index == pawnIndex || owner.Index == controllerIndex)
                return viewModel;
        }

        return null;
    }
}
