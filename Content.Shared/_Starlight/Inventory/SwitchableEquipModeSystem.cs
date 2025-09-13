using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Robust.Shared.Audio.Systems;
using Content.Shared.Inventory.Events;

namespace Content.Shared._Starlight.Inventory;

public sealed class SwitchableEquipModeSystem : EntitySystem
{
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private readonly Dictionary<EquipMode, string> _equipModeLocaleMapping = new() {
        { EquipMode.Remove, "equipmode-mode-remove" },
        { EquipMode.Open,   "equipmode-mode-open"   }
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SwitchableEquipModeComponent, InventoryUseSlotEvent>(OnInventoryUseSlot);
        SubscribeLocalEvent<SwitchableEquipModeComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
        SubscribeLocalEvent<SwitchableEquipModeComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnInventoryUseSlot(EntityUid uid, SwitchableEquipModeComponent switchModeComp, ref InventoryUseSlotEvent args)
    {
        if (!TryComp<StorageComponent>(uid, out var storageComp)) return;

        switch (switchModeComp.Mode)
        {
            case EquipMode.Remove:
                return; // pass to next

            case EquipMode.Open:
                args.Handled = true;
                if (_ui.IsUiOpen(uid, StorageComponent.StorageUiKey.Key, args.Actor)) return; // stop sound spam

                _storage.OpenStorageUI(args.Target, args.Actor);
                _audio.PlayPredicted(storageComp.StorageOpenSound, uid, args.Actor);

                break;
        }
    }

    private void OnGetVerbs(EntityUid uid, SwitchableEquipModeComponent switchModeComp, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess) return;
        if (!TryComp<StorageComponent>(uid, out var storageComp)) return;
        if (!_inventory.TryGetContainingSlot(uid, out var slot)) return;

        var modeCount = Enum.GetNames<EquipMode>().Length;
        EquipMode nextMode = (EquipMode)(((int)switchModeComp.Mode + 1) % modeCount);

        ActivationVerb switchVerb = new()
        {
            Text = Loc.GetString("equipmode-switch", ("type", Loc.GetString(new(_equipModeLocaleMapping[nextMode])))),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
            Priority = -1,
            Act = () => switchModeComp.Mode = nextMode
        };
        args.Verbs.Add(switchVerb);

        // misc verbs

        EntityUid user = args.User;

        ActivationVerb unequipVerb = new()
        {
            Text = Loc.GetString("equipmode-remove"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
            Priority = 0,
            Act = () =>
            {
                _inventory.TryUnequip(user, slot.Name, false, true);
                _hands.TryPickup(user, uid);
            }
        };

        switch (switchModeComp.Mode)
        {
            // we want an unequip option if the default click option isn't to remove
            case EquipMode.Open:
                args.Verbs.Add(unequipVerb);
                break;

            default:
                break;
        };
    }

    private void OnUnequipped(Entity<SwitchableEquipModeComponent> ent, ref GotUnequippedEvent _) => ent.Comp.Mode = EquipMode.Remove; // revert to default behaviour
}