using Content.Shared.Movement.Systems;
using Content.Shared.Stunnable;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// System that applies movement speed penalties and standing time modifications for the Impaired Mobility trait.
/// Speed reduction is nullified when holding an item with the MobilityAidComponent, handled by the MobilityAidSystem.
/// </summary>
public sealed class ImpairedMobilitySystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ImpairedMobilityComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<ImpairedMobilityComponent, GetStandUpTimeEvent>(OnGetStandUpTime);
    }

    private void OnRefreshMovementSpeed(EntityUid uid, ImpairedMobilityComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        // Applies the movement penalty for the trait
        // Mobility aids (any item with the MobilityAidComponent) counter this penalty via MobilityAidSystem
        args.ModifySpeed(component.SpeedModifier);
    }

    private void OnGetStandUpTime(EntityUid uid, ImpairedMobilityComponent component, ref GetStandUpTimeEvent args)
    {
        // Apply the standing time penalty for impaired mobility
        // This multiplies whatever time other systems have calculated
        args.DoAfterTime = TimeSpan.FromTicks((long)(args.DoAfterTime.Ticks * component.StandUpTimeModifier));
    }
}
