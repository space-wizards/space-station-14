using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Console;

namespace Content.Server.Nutrition;

[AdminCommand(AdminFlags.Debug)]
public sealed class Hungry : LocalizedEntityCommands
{
    public override string Command => "hungry";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteError(Loc.GetString("cmd-nutrition-error-player"));
            return;
        }

        if (player.AttachedEntity is not {Valid: true} playerEntity)
        {
            shell.WriteError(Loc.GetString("cmd-nutrition-error-entity"));
            return;
        }

        if (!EntityManager.TryGetComponent(playerEntity, out HungerComponent? hunger))
        {
            shell.WriteError(Loc.GetString("cmd-nutrition-error-component", ("comp", nameof(HungerComponent))));
            return;
        }

        var hungryThreshold = hunger.Thresholds[HungerThreshold.Starving];
        EntityManager.System<HungerSystem>().SetHunger(playerEntity, hungryThreshold, hunger);
    }
}
