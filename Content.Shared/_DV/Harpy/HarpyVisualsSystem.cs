using Content.Shared.Inventory.Events;
using Content.Shared.Tag;
using Content.Shared.Humanoid;
using Content.Shared._NF.Clothing.Components; // Frontier

namespace Content.Shared._DV.Harpy;

public sealed class HarpyVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HarpySingerComponent, DidEquipEvent>(OnDidEquipEvent);
        SubscribeLocalEvent<HarpySingerComponent, DidUnequipEvent>(OnDidUnequipEvent);
    }

    private void OnDidEquipEvent(EntityUid uid, HarpySingerComponent component, DidEquipEvent args)
    {
        if (args.Slot == "outerClothing" && HasComp<HarpyHideWingsComponent>(args.Equipment))
        {
            _humanoidSystem.SetLayerVisibility(uid, HumanoidVisualLayers.RArmExtension, false);
            _humanoidSystem.SetLayerVisibility(uid, HumanoidVisualLayers.Tail, false);
        }
    }

    private void OnDidUnequipEvent(EntityUid uid, HarpySingerComponent component, DidUnequipEvent args)
    {
        if (args.Slot == "outerClothing" && HasComp<HarpyHideWingsComponent>(args.Equipment))
        {
            _humanoidSystem.SetLayerVisibility(uid, HumanoidVisualLayers.RArmExtension, true);
            _humanoidSystem.SetLayerVisibility(uid, HumanoidVisualLayers.Tail, true);
        }
    }
}
