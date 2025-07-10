using Content.Server.Administration.UI;
using Content.Server.Clothing.Systems;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Inventory;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class SetOutfitCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly EuiManager _euiManager = default!;
        [Dependency] private readonly OutfitSystem _outfitSystem = default!;

        public override string Command => "setoutfit";
        public override string Description => Loc.GetString("cmd-setoutfit-desc", ("requiredComponent", nameof(InventoryComponent)));

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!int.TryParse(args[0], out var entInt))
            {
                shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            var nent = new NetEntity(entInt);

            if (!EntityManager.TryGetEntity(nent, out var target))
            {
                shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
                return;
            }

            if (!EntityManager.HasComponent<InventoryComponent>(target))
            {
                shell.WriteLine(Loc.GetString("shell-target-entity-does-not-have-message", ("missing", "inventory")));
                return;
            }

            if (args.Length == 1)
            {
                if (shell.Player is not { } player)
                {
                    shell.WriteError(Loc.GetString("cmd-setoutfit-is-not-player-error"));
                    return;
                }

                var ui = new SetOutfitEui(nent);
                _euiManager.OpenEui(ui, player);
                return;
            }

            if (!_outfitSystem.SetOutfit(target.Value, args[1]))
                shell.WriteLine(Loc.GetString("cmd-setoutfit-invalid-outfit-id-error"));
        }
    }
}
