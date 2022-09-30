using Content.Server.Humanoid;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;
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
        if (args.Slot == "head" && component.HidesHair)
        {
            _humanoidSystem.SetLayersVisibility(args.Equipee,
                HumanoidVisualLayersExtension.Sublayers(HumanoidVisualLayers.Head), false);
        }
    }

    private void OnGotUnequipped(EntityUid uid, SharedClothingComponent component, GotUnequippedEvent args)
    {
        if (args.Slot == "head" && component.HidesHair)
        {
            _humanoidSystem.SetLayersVisibility(args.Equipee,
                HumanoidVisualLayersExtension.Sublayers(HumanoidVisualLayers.Head), true);
        }
    }

    protected override void OnHidesHairToggled(EntityUid uid, SharedClothingComponent clothing)
    {
        if (!_container.TryGetContainingContainer(uid, out var container))
            return;

        var slot = container.ID;
        if (slot != "head")
            return;

        var layers = HumanoidVisualLayersExtension.Sublayers(HumanoidVisualLayers.Head);
        _humanoidSystem.SetLayersVisibility(container.Owner, layers, !clothing.HidesHair);
    }
}
