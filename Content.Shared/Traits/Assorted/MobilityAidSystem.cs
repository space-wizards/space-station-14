using Content.Shared.Hands;
using Content.Shared.Movement.Systems;
using Robust.Shared.Containers;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Handles MobilityAidComponent functionality. Counteracts the ImpairedMobilityComponent speed penalty when the mobility aid is held in-hand.
/// </summary>
public sealed class MobilityAidSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MobilityAidComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<MobilityAidComponent, GotUnequippedHandEvent>(OnGotUnequippedHand);
        SubscribeLocalEvent<MobilityAidComponent, HeldRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMovementSpeedModifiers);
    }

    private void OnGotEquippedHand(Entity<MobilityAidComponent> ent, ref GotEquippedHandEvent args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnGotUnequippedHand(Entity<MobilityAidComponent> ent, ref GotUnequippedHandEvent args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnRefreshMovementSpeedModifiers(EntityUid uid, MobilityAidComponent component, HeldRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        // Checks if entity is in a container (such as a player's hand)
        if (!_containerSystem.TryGetContainingContainer(uid, out var container) ||
            container.Owner == EntityUid.Invalid)
            return;

        var holder = container.Owner;

        // Check if the entity has the ImpairedMobilityComponent
        if (TryComp<ImpairedMobilityComponent>(holder, out var impaired))
        {
            // Calculate the exact multiplier needed to counteract the impaired mobility penalty according to the SpeedModifier
            var counterMultiplier = 1.0f / impaired.SpeedModifier;
            args.Args.ModifySpeed(counterMultiplier);
        }
    }
}
