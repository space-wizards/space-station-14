using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// This system implements movement speed modifiers based on satiations.
/// </summary>
public sealed partial class SatiationSpeedModifierSystem : BaseSatiationEffectSystem<SatiationSpeedModifierComponent>
{
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SatiationSpeedModifierComponent, RefreshMovementSpeedModifiersEvent>(
            OnRefreshMovementSpeed);
    }

    /// <summary>
    /// Forwards threshold changes to movement speed modifier refresh events.
    /// </summary>
    protected override void OnThresholdChanged(
        Entity<SatiationSpeedModifierComponent, SatiationComponent> entity,
        ref SatiationThresholdChangedEvent args
    )
    {
        if (!entity.Comp1.Satiations.ContainsKey(args.Satiation))
            return;

        _movementSpeedModifier.RefreshMovementSpeedModifiers(entity);
    }

    /// <summary>
    /// Applies a speed modifier based on the current satiation level.
    /// </summary>
    private void OnRefreshMovementSpeed(
        Entity<SatiationSpeedModifierComponent> entity,
        ref RefreshMovementSpeedModifiersEvent args
    )
    {
        if (!SatiationQuery.TryComp(entity, out var comp))
            return;
        var satiation = new Entity<SatiationComponent>(entity, comp);

        foreach (var (satiationType, thresholds) in entity.Comp.Satiations)
        {
            if (!SatiationSystem.TryGetValueByThreshold(
                    satiation,
                    satiationType,
                    thresholds,
                    out var res
                ))
                continue;

            args.ModifySpeed(res);
        }
    }
}
