using Content.Server.Humanoid;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;

namespace Content.Server.Clothing;

public sealed class ServerClothingSystem : ClothingSystem
{
    [Dependency] private readonly HumanoidSystem _humanoidSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    protected override void OnGotEquipped(EntityUid uid, ClothingComponent component, GotEquippedEvent args)
    {
        base.OnGotEquipped(uid, component, args);
        // why the fuck is humanoid visuals server-only???

        if (args.Slot == "head"
            && _tagSystem.HasTag(args.Equipment, "HidesHair"))
        {
            _humanoidSystem.ToggleHiddenLayer(args.Equipee, HumanoidVisualLayers.Hair);
        }
    }

    protected override void OnGotUnequipped(EntityUid uid, ClothingComponent component, GotUnequippedEvent args)
    {
        base.OnGotUnequipped(uid, component, args);

        // why the fuck is humanoid visuals server-only???

        if (args.Slot == "head"
            && _tagSystem.HasTag(args.Equipment, "HidesHair"))
        {
            _humanoidSystem.ToggleHiddenLayer(args.Equipee, HumanoidVisualLayers.Hair);
        }
    }
}
