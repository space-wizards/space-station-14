using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Item;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Mobs.Systems;

namespace Content.Shared._Impstation.MobStateClothingVisuals;

public abstract class SharedMobStateClothingVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _itemSys = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateClothingVisualsComponent, InventoryRelayedEvent<MobStateChangedEvent>>(OnMobStateChanged);
        SubscribeLocalEvent<MobStateClothingVisualsComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals);
    }

    private void OnMobStateChanged(Entity<MobStateClothingVisualsComponent> ent, ref InventoryRelayedEvent<MobStateChangedEvent> args)
    {
        _itemSys.VisualsChanged(ent); // update clothing visuals

        var ev = new ClothingMobStateChangedEvent();
        RaiseLocalEvent(ent, ev);
    }

    private void OnGetEquipmentVisuals(Entity<MobStateClothingVisualsComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (!TryComp(ent, out ClothingComponent? clothingComp))
            return;

        ent.Comp.ClothingPrefix ??= clothingComp.EquippedPrefix; // if the clothing prefix is null, set it to the clothing component's equipped prefix

        var prefix = _mobState.IsIncapacitated(args.Equipee) ? ent.Comp.IncapacitatedPrefix : ent.Comp.ClothingPrefix;
        _clothing.SetEquippedPrefix(ent, prefix, clothingComp); // change the sprite based on the 
    }
}
