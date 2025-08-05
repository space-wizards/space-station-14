using Content.Shared.Hands;
using Content.Shared.Movement.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Handles MobilityAidComponent functionality. Counteracts the ImpairedMobilityComponent speed penalty when ANY mobility aid is held in-hand.
/// </summary>
public sealed class MobilityAidSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MobilityAidComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<MobilityAidComponent, GotUnequippedHandEvent>(OnGotUnequippedHand);
        SubscribeLocalEvent<ImpairedMobilityComponent, RefreshMovementSpeedModifiersEvent>(OnImpairedMobilityRefreshMovementSpeed);
    }

    private void OnGotEquippedHand(Entity<MobilityAidComponent> ent, ref GotEquippedHandEvent args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnGotUnequippedHand(Entity<MobilityAidComponent> ent, ref GotUnequippedHandEvent args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnImpairedMobilityRefreshMovementSpeed(EntityUid uid, ImpairedMobilityComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        // Check if the entity is holding any mobility aid
        if (HasMobilityAid(uid))
        {
            var counterMultiplier = 1.0f / component.SpeedModifier;
            args.ModifySpeed(counterMultiplier);
        }
    }

    /// <summary>
    /// Checks if the entity is currently holding any items with MobilityAidComponent
    /// Returns true if holding 1+ mobility aids, false if holding 0 prevents the effect from speed stacking when holding multiple aids.
    /// </summary>
    private bool HasMobilityAid(EntityUid uid)
    {
        if (!TryComp<HandsComponent>(uid, out var hands))
            return false;

        foreach (var held in _handsSystem.EnumerateHeld((uid, hands)))
        {
            if (HasComp<MobilityAidComponent>(held))
                return true;
        }

        return false;
    }
}
