using Content.Server.Humanoid;
using Content.Shared.Clothing.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;

namespace Content.Server.Clothing;

public sealed class ServerClothingSystem : EntitySystem
{
    [Dependency] private readonly HumanoidSystem _humanoidSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SharedClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SharedClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(EntityUid uid, SharedClothingComponent component, GotEquippedEvent args)
    {
        if (args.Slot == "head"
            && _tagSystem.HasTag(args.Equipment, "HidesHair"))
        {
            _humanoidSystem.SetLayersVisibility(args.Equipee,
                HumanoidVisualLayersExtension.Sublayers(HumanoidVisualLayers.Head), false);
        }
    }

    private void OnGotUnequipped(EntityUid uid, SharedClothingComponent component, GotUnequippedEvent args)
    {
        if (args.Slot == "head"
            && _tagSystem.HasTag(args.Equipment, "HidesHair"))
        {
            _humanoidSystem.SetLayersVisibility(args.Equipee,
                HumanoidVisualLayersExtension.Sublayers(HumanoidVisualLayers.Head), true);
        }
    }
}
