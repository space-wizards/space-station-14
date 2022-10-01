using Content.Server.Humanoid;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;

namespace Content.Server.Clothing;

public sealed class ClothingSystem : SharedClothingSystem
{
    [Dependency] private readonly HumanoidSystem _humanoidSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SharedClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SharedClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(EntityUid uid, SharedClothingComponent component, GotEquippedEvent args)
    {
        if (!component.HidesHair)
            return;

        UpdateHumanoidHair(uid, args.Equipee, args.Slot, false, component);
    }

    private void OnGotUnequipped(EntityUid uid, SharedClothingComponent component, GotUnequippedEvent args)
    {
        if (!component.HidesHair)
            return;

        UpdateHumanoidHair(uid, args.Equipee, args.Slot, true, component);
    }

    protected override void OnHidesHairToggled(EntityUid uid, SharedClothingComponent clothing)
    {
        base.OnHidesHairToggled(uid, clothing);
        
        if (!_container.TryGetContainingContainer(uid, out var container))
            return;

        var slot = container.ID;
        var hairVisible = !clothing.HidesHair;
        UpdateHumanoidHair(uid, container.Owner, slot, hairVisible, clothing);
    }

    private void UpdateHumanoidHair(EntityUid clothingUid, EntityUid humanoidUid, string slot, bool hairVisible,
        SharedClothingComponent? clothing = null, HumanoidComponent? humanoid = null)
    {
        if (slot != "head")
            return;
        if (!Resolve(clothingUid, ref clothing) || !Resolve(humanoidUid, ref humanoid, false))
            return;

        var hairLayers = HumanoidVisualLayersExtension.Sublayers(HumanoidVisualLayers.Head);
        _humanoidSystem.SetLayersVisibility(humanoidUid, hairLayers, hairVisible, humanoid: humanoid);
    }
}
