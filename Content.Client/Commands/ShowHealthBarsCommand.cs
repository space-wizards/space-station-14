using Content.Shared.Damage.Prototypes;
using Content.Shared.Overlays;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Client.Commands;

public sealed class ShowHealthBarsCommand : LocalizedEntityCommands
{
    public override string Command => "showhealthbars";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        if (player.AttachedEntity is not { } playerEntity)
        {
            shell.WriteError(Loc.GetString("shell-must-be-attached-to-entity"));
            return;
        }

        if (!EntityManager.HasComponent<ShowHealthBarsComponent>(playerEntity))
        {
            var showHealthBarsComponent = new ShowHealthBarsComponent
            {
                DamageContainers = args.Select(arg => new ProtoId<DamageContainerPrototype>(arg)).ToList(),
                HealthStatusIcon = null,
                NetSyncEnabled = false,
            };

            EntityManager.AddComponent(playerEntity, showHealthBarsComponent, true);

            shell.WriteLine(Loc.GetString("cmd-showhealthbars-notify-enabled", ("args", string.Join(", ", args))));
            return;
        }

        EntityManager.RemoveComponentDeferred<ShowHealthBarsComponent>(playerEntity);
        shell.WriteLine(Loc.GetString("cmd-showhealthbars-notify-disabled"));
    }
}
