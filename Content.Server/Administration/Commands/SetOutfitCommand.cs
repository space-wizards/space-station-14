using Content.Server.Administration.UI;
using Content.Server.EUI;
using Content.Server.Preferences.Managers;
using Content.Shared.Administration;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using InventoryComponent = Content.Shared.Inventory.InventoryComponent;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    sealed class SetOutfitCommand : IConsoleCommand
    {
        public string Command => "setoutfit";

        public string Description => Loc.GetString("set-outfit-command-description", ("requiredComponent", nameof(InventoryComponent)));

        public string Help => Loc.GetString("set-outfit-command-help-text", ("command",Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!int.TryParse(args[0], out var entityUid))
            {
                shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            var target = new EntityUid(entityUid);

            if (!target.IsValid() || !entityManager.EntityExists(target))
            {
                shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
                return;
            }

            if (!entityManager.TryGetComponent<InventoryComponent?>(target, out var inventoryComponent))
            {
                shell.WriteLine(Loc.GetString("shell-target-entity-does-not-have-message",("missing", "inventory")));
                return;
            }

            if (args.Length == 1)
            {
                if (shell.Player is not IPlayerSession player)
                {
                    shell.WriteError(Loc.GetString("set-outfit-command-is-not-player-error"));
                    return;
                }

                var eui = IoCManager.Resolve<EuiManager>();
                var ui = new SetOutfitEui(target);
                eui.OpenEui(ui, player);
                return;
            }

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            if (!prototypeManager.TryIndex<StartingGearPrototype>(args[1], out var startingGear))
            {
                shell.WriteLine(Loc.GetString("set-outfit-command-invalid-outfit-id-error"));
                return;
            }

            HumanoidCharacterProfile? profile = null;
            // Check if we are setting the outfit of a player to respect the preferences
            if (entityManager.TryGetComponent<ActorComponent?>(target, out var actorComponent))
            {
                var userId = actorComponent.PlayerSession.UserId;
                var preferencesManager = IoCManager.Resolve<IServerPreferencesManager>();
                var prefs = preferencesManager.GetPreferences(userId);
                profile = prefs.SelectedCharacter as HumanoidCharacterProfile;
            }

            var invSystem = EntitySystem.Get<InventorySystem>();
            if (invSystem.TryGetSlots(target, out var slotDefinitions, inventoryComponent))
            {
                foreach (var slot in slotDefinitions)
                {
                    invSystem.TryUnequip(target, slot.Name, true, true, inventoryComponent);
                    var gearStr = startingGear.GetGear(slot.Name, profile);
                    if (gearStr == string.Empty)
                    {
                        continue;
                    }
                    var equipmentEntity = entityManager.SpawnEntity(gearStr, entityManager.GetComponent<TransformComponent>(target).Coordinates);
                    if (slot.Name == "id" &&
                        entityManager.TryGetComponent<PDAComponent?>(equipmentEntity, out var pdaComponent) &&
                        pdaComponent.ContainedID != null)
                    {
                        pdaComponent.ContainedID.FullName = entityManager.GetComponent<MetaDataComponent>(target).EntityName;
                    }

                    invSystem.TryEquip(target, equipmentEntity, slot.Name, true, inventory: inventoryComponent);
                }
            }
        }
    }
}
