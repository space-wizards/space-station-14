using Content.Server.Eui;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    class SetOutfitCommand : IClientCommand
    {
        public string Command => "setoutfit";

        public string Description => Loc.GetString("Sets the outfit of the specified entity. The entity must have an InventoryComponent");

        public string Help => Loc.GetString("Usage: {0} <entityUid> | {0} <entityUid> <outfitId>", Command);

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length < 1)
            {
                shell.SendText(player, Loc.GetString("Wrong number of arguments."));
                return;
            }

            if (!int.TryParse(args[0], out var entityUid))
            {
                shell.SendText(player, Loc.GetString("EntityUid must be a number."));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            var eUid = new EntityUid(entityUid);

            if (!eUid.IsValid() || !entityManager.EntityExists(eUid))
            {
                shell.SendText(player, Loc.GetString("Invalid entity ID."));
                return;
            }

            var target = entityManager.GetEntity(eUid);

            if (!target.TryGetComponent<InventoryComponent>(out var inventoryComponent))
            {
                shell.SendText(player, Loc.GetString("Target entity does not have an inventory!"));
                return;
            }

            if (args.Length == 1)
            {
                var eui = IoCManager.Resolve<EuiManager>();
                var ui = new SetOutfitEui(target);
                eui.OpenEui(ui, player);
                return;
            }

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            if (!prototypeManager.TryIndex<StartingGearPrototype>(args[1], out var startingGear))
            {
                shell.SendText(player, Loc.GetString("Invalid outfit id"));
                return;
            }

            foreach (var slot in inventoryComponent.Slots)
            {
                inventoryComponent.ForceUnequip(slot);
                var gearStr = startingGear.GetGear(slot, null);
                if (gearStr != "")
                {
                    var equipmentEntity = entityManager.SpawnEntity(gearStr, target.Transform.Coordinates);
                    inventoryComponent.Equip(slot, equipmentEntity.GetComponent<ItemComponent>());
                }
            }

        }
    }
}
