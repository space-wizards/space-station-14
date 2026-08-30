using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed partial class SatiationSpeedModifierSystem :
    BaseSatiationEffectSystem<SatiationSpeedModifierComponent, float>
{
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;

    protected override Dictionary<ProtoId<SatiationTypePrototype>, SatiationThresholds<float>> GetThresholds(
        SatiationSpeedModifierComponent comp) => comp.Satiations;

    protected override float DefaultValue() => 1f;

    protected override void AfterSatiationUpdate(Entity<SatiationSpeedModifierComponent> entity)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(entity.Owner);
    }

    [SubscribeLocalEvent]
    private static void OnRefreshMovementSpeed(
        Entity<SatiationSpeedModifierComponent> entity,
        ref RefreshMovementSpeedModifiersEvent args
    )
    {
        foreach (var (_, thresholds) in entity.Comp.Satiations)
        {
            args.ModifySpeed(thresholds.Current);
        }
    }
}
