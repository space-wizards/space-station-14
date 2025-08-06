using Content.Shared.Movement.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Wieldable.Components;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Handles <see cref="ImpairedMobilityComponent"/>
/// </summary>
public sealed class ImpairedMobilitySystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ImpairedMobilityComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<ImpairedMobilityComponent, GetStandUpTimeEvent>(OnGetStandUpTime);
    }

    /// Handles movement speed for entities with impaired mobility.
    /// Applies a speed penalty, but counteracts it if the entity is holding a non-wielded mobility aid.
    private void OnRefreshMovementSpeed(EntityUid uid, ImpairedMobilityComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.SpeedModifier);

        if (HasMobilityAid(uid))
        {
            var counterMultiplier = 1.0f / component.SpeedModifier;
            args.ModifySpeed(counterMultiplier);
        }
    }

    /// Increases the time it takes for entities to stand up from being knocked down.
    private void OnGetStandUpTime(EntityUid uid, ImpairedMobilityComponent component, ref GetStandUpTimeEvent args)
    {
        args.DoAfterTime = TimeSpan.FromTicks((long)(args.DoAfterTime.Ticks * component.StandUpTimeModifier));
    }

    /// Checks if the entity is holding any non-wielded mobility aids.
    /// Wielded mobility aids do NOT provide speed restoration. You can't use a mobility aid if you're swinging it around dingus.
    private bool HasMobilityAid(EntityUid uid)
    {
        if (!TryComp<HandsComponent>(uid, out var hands))
            return false;

        var handsSystem = EntityManager.System<SharedHandsSystem>();
        foreach (var held in handsSystem.EnumerateHeld((uid, hands)))
        {
            if (HasComp<MobilityAidComponent>(held))
            {
                if (TryComp<WieldableComponent>(held, out var wieldable) && wieldable.Wielded)
                    continue;

                return true;
            }
        }

        return false;
    }
}
