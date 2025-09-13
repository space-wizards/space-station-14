using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._Starlight.Inventory;

public sealed class SwitchableEquipModeSystem : EntitySystem
{
    [Dependency] private readonly SharedStorageSystem _sharedStorage = default!;

    private readonly Dictionary<EquipMode, string> _equipModeLocaleMapping = new() {
        { EquipMode.Remove, "equipmode-mode-remove" },
        { EquipMode.Open,   "equipmode-mode-open"   }
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SwitchableEquipModeComponent, InventoryUseSlotEvent>(OnInventoryUseSlot);
        SubscribeLocalEvent<SwitchableEquipModeComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
    }

    private void OnInventoryUseSlot(Entity<SwitchableEquipModeComponent> ent, ref InventoryUseSlotEvent args)
    {
        var comp = ent.Comp;

        switch (comp.Mode)
        {
            case EquipMode.Remove:
                return; // pass to next

            case EquipMode.Open:
                _sharedStorage.OpenStorageUI(args.Target, args.Actor);
                args.Handled = true;
                break;
        }
    }

    private void OnGetVerbs(EntityUid uid, SwitchableEquipModeComponent switchModeComp, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract) return;
        if (!TryComp<StorageComponent>(uid, out var storageComp)) return; // not a bag

        var modeCount = Enum.GetNames<EquipMode>().Length;
        EquipMode nextMode = (EquipMode)(((int)switchModeComp.Mode + 1) % modeCount);

        InteractionVerb switchVerb = new()
        {
            Text = Loc.GetString("equipmode-switch", ("type", Loc.GetString(new(_equipModeLocaleMapping[nextMode])))),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
            Act = () => switchModeComp.Mode = nextMode
        };
        args.Verbs.Add(switchVerb);
    }
}