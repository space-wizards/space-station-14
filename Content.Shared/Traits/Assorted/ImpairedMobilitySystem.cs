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
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<ImpairedMobilityComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<ImpairedMobilityComponent, GetStandUpTimeEvent>(OnGetStandUpTime);
    }

    // Handles movement speed for entities with impaired mobility.
    // Applies a speed penalty, but counteracts it if the entity is holding a non-wielded mobility aid.
    private void OnRefreshMovementSpeed(EntityUid uid, ImpairedMobilityComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (HasMobilityAid(uid))
            return;

        args.ModifySpeed(component.SpeedModifier);
    }

    // Increases the time it takes for entities to stand up from being knocked down.
    private void OnGetStandUpTime(EntityUid uid, ImpairedMobilityComponent component, ref GetStandUpTimeEvent args)
    {
        args.DoAfterTime *= component.StandUpTimeModifier;
    }

    // Checks if the entity is holding any non-wielded mobility aids.
    private bool HasMobilityAid(Entity<HandsComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        foreach (var held in _hands.EnumerateHeld(entity))
        {
            if (!HasComp<MobilityAidComponent>(held))
                continue;

            // Makes sure it's not wielded yet
            if (TryComp<WieldableComponent>(held, out var wieldable) && wieldable.Wielded)
                continue;

            return true;
        }

        return false;
    }
}
