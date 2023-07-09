using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Inventory;
using Content.Shared.Rejuvenate;
using JetBrains.Annotations;

namespace Content.Shared.Eye.Blinding.Systems;

public sealed class BlindableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlindableComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnRejuvenate(EntityUid uid, BlindableComponent component, RejuvenateEvent args)
    {
        AdjustEyeDamage(uid, -component.EyeDamage, component);
    }

    [PublicAPI]
    public void UpdateIsBlind(EntityUid uid, BlindableComponent? blindable = null)
    {
        if (!Resolve(uid, ref blindable, false))
            return;

        var old = blindable.IsBlind;

        // Don't bother raising an event if the eye is too damaged.
        if (blindable.EyeDamage >= BlindableComponent.MaxDamage)
        {
            blindable.IsBlind = true;
        }
        else
        {
            var ev = new CanSeeAttemptEvent();
            RaiseLocalEvent(uid, ev);
            blindable.IsBlind = ev.Blind;
        }

        if (old == blindable.IsBlind)
            return;

        var changeEv = new BlindnessChangedEvent(blindable.IsBlind);
        RaiseLocalEvent(uid, ref changeEv);
        Dirty(blindable);
    }

    public void AdjustEyeDamage(EntityUid uid, int amount, BlindableComponent? blindable = null)
    {
        if (!Resolve(uid, ref blindable, false) || amount == 0)
            return;

        blindable.EyeDamage += amount;
        blindable.EyeDamage = Math.Clamp(blindable.EyeDamage, 0, BlindableComponent.MaxDamage);
        Dirty(blindable);
        UpdateIsBlind(uid, blindable);

        var ev = new EyeDamageChangedEvent(blindable.EyeDamage);
        RaiseLocalEvent(uid, ref ev);
    }
}

/// <summary>
///     This event is raised when an entity's blindness changes
/// </summary>
[ByRefEvent]
public record struct BlindnessChangedEvent(bool Blind);

/// <summary>
///     This event is raised when an entity's eye damage changes
/// </summary>
[ByRefEvent]
public record struct  EyeDamageChangedEvent(int Damage);

/// <summary>
///     Raised directed at an entity to see whether the entity is currently blind or not.
/// </summary>
public sealed class CanSeeAttemptEvent : CancellableEntityEventArgs, IInventoryRelayEvent
{
    public bool Blind => Cancelled;
    public SlotFlags TargetSlots => SlotFlags.EYES | SlotFlags.MASK | SlotFlags.HEAD;
}

public sealed class GetEyeProtectionEvent : EntityEventArgs, IInventoryRelayEvent
{
    /// <summary>
    ///     Time to subtract from any temporary blindness sources.
    /// </summary>
    public TimeSpan Protection;

    public SlotFlags TargetSlots => SlotFlags.EYES | SlotFlags.MASK | SlotFlags.HEAD;
}
