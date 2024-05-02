using Content.Server.Administration.UI;
using Content.Server.EUI;
using Content.Server.Hands.Systems;
using Content.Server.Preferences.Managers;
using Content.Shared.Access.Components;
using Content.Shared.Administration;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Clothing;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Server.Station.Systems;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class SetOutfitCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "setoutfit";

        public string Description => Loc.GetString("set-outfit-command-description", ("requiredComponent", nameof(InventoryComponent)));

        public string Help => Loc.GetString("set-outfit-command-help-text", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
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

            if (!_entities.TryGetEntity(nent, out var target))
            {
                shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
                return;
            }

            if (!_entities.HasComponent<InventoryComponent>(target))
            {
                shell.WriteLine(Loc.GetString("shell-target-entity-does-not-have-message", ("missing", "inventory")));
                return;
            }

            if (args.Length == 1)
            {
                if (shell.Player is not { } player)
                {
                    shell.WriteError(Loc.GetString("set-outfit-command-is-not-player-error"));
                    return;
                }

                var eui = IoCManager.Resolve<EuiManager>();
                var ui = new SetOutfitEui(nent);
                eui.OpenEui(ui, player);
                return;
            }

            if (!SetOutfit(target.Value, args[1], _entities))
                shell.WriteLine(Loc.GetString("set-outfit-command-invalid-outfit-id-error"));
        }

        public static bool SetOutfit(EntityUid target, string gear, IEntityManager entityManager, Action<EntityUid, EntityUid>? onEquipped = null)
        {
            if (!entityManager.TryGetComponent(target, out InventoryComponent? inventoryComponent))
                return false;

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            if (!prototypeManager.TryIndex<StartingGearPrototype>(gear, out var startingGear))
                return false;

            HumanoidCharacterProfile? profile = null;
            // Check if we are setting the outfit of a player to respect the preferences
            if (entityManager.TryGetComponent(target, out ActorComponent? actorComponent))
            {
                var userId = actorComponent.PlayerSession.UserId;
                var preferencesManager = IoCManager.Resolve<IServerPreferencesManager>();
                var prefs = preferencesManager.GetPreferences(userId);
                profile = prefs.SelectedCharacter as HumanoidCharacterProfile;
            }

            var invSystem = entityManager.System<InventorySystem>();

            if (!invSystem.TryGetSlots(target, out var slots))
                return false;

            foreach (var slot in slots)
                invSystem.TryUnequip(target, slot.Name, true, true, false, inventoryComponent);

            if (startingGear.Loadout != string.Empty)
            {
                var jobLoadout = LoadoutSystem.GetJobPrototype(startingGear.Loadout);

                if (prototypeManager.TryIndex(jobLoadout, out RoleLoadoutPrototype? roleProto))
                {
                    RoleLoadout? loadout = null;
                    profile?.Loadouts.TryGetValue(jobLoadout, out loadout);

                    // Set to default if not present
                    if (loadout == null)
                    {
                        loadout = new RoleLoadout(jobLoadout);
                        loadout.SetDefault(prototypeManager);
                    }

                    // Order loadout selections by the order they appear on the prototype.
                    foreach (var group in loadout.SelectedLoadouts.OrderBy(x => roleProto.Groups.FindIndex(e => e == x.Key)))
                    {
                        foreach (var items in group.Value)
                        {
                            if (!prototypeManager.TryIndex(items.Prototype, out var loadoutProto))
                                continue;

                            if (!prototypeManager.TryIndex(loadoutProto.Equipment, out var loadoutGear))
                                continue;

                            foreach (var slot in slots)
                            {
                                var gearStr = loadoutGear.GetGear(slot.Name);
                                if (gearStr == string.Empty)
                                    continue;

                                SpawnAndEquip(target, gearStr, slot, entityManager, invSystem, inventoryComponent, onEquipped);
                            }
                        }
                    }
                }
            }

            foreach (var slot in slots)
            {
                var gearStr = startingGear.GetGear(slot.Name);
                if (gearStr == string.Empty)
                    continue;

                SpawnAndEquip(target, gearStr, slot, entityManager, invSystem, inventoryComponent, onEquipped);
            }

            if (entityManager.TryGetComponent(target, out HandsComponent? handsComponent))
            {
                var handsSystem = entityManager.System<HandsSystem>();
                var coords = entityManager.GetComponent<TransformComponent>(target).Coordinates;
                foreach (var prototype in startingGear.Inhand)
                {
                    var inhandEntity = entityManager.SpawnEntity(prototype, coords);
                    handsSystem.TryPickup(target, inhandEntity, checkActionBlocker: false, handsComp: handsComponent);
                }
            }

            return true;
        }

        public static void SpawnAndEquip(EntityUid target, string gearStr, SlotDefinition slot, IEntityManager entityManager, InventorySystem invSystem, InventoryComponent inventoryComponent, Action<EntityUid, EntityUid>? onEquipped = null)
        {
            var equipmentEntity = entityManager.SpawnEntity(gearStr, entityManager.GetComponent<TransformComponent>(target).Coordinates);
            if (slot.Name == "id" &&
                entityManager.TryGetComponent(equipmentEntity, out PdaComponent? pdaComponent) &&
                entityManager.TryGetComponent<IdCardComponent>(pdaComponent.ContainedId, out var id))
            {
                id.FullName = entityManager.GetComponent<MetaDataComponent>(target).EntityName;
            }

            invSystem.TryEquip(target, equipmentEntity, slot.Name, silent: true, force: true, inventory: inventoryComponent);

            onEquipped?.Invoke(target, equipmentEntity);
        }
    }
}
