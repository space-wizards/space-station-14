using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Inventory;
using Content.Shared.Rejuvenate;
using JetBrains.Annotations;

namespace Content.Shared.Eye.Blinding.Systems;

public sealed class BlindableSystem : EntitySystem
{
    [Dependency] private readonly BlurryVisionSystem _blurriness = default!;
    [Dependency] private readonly EyeClosingSystem _eyelids = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlindableComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<BlindableComponent, EyeDamageChangedEvent>(OnDamageChanged);
    }

    private void OnRejuvenate(Entity<BlindableComponent> ent, ref RejuvenateEvent args)
    {
        AdjustEyeDamage((ent.Owner, ent.Comp), -ent.Comp.EyeDamage);
    }

    private void OnDamageChanged(Entity<BlindableComponent> ent, ref EyeDamageChangedEvent args)
    {
        _blurriness.UpdateBlurMagnitude((ent.Owner, ent.Comp));
        _eyelids.UpdateEyesClosable((ent.Owner, ent.Comp));
    }

    [PublicAPI]
    public void UpdateIsBlind(Entity<BlindableComponent?> blindable)
    {
        if (!Resolve(blindable, ref blindable.Comp, false))
            return;

        var old = blindable.Comp.IsBlind;

        // Don't bother raising an event if the eye is too damaged.
        if (blindable.Comp.EyeDamage >= BlindableComponent.MaxDamage)
        {
            blindable.Comp.IsBlind = true;
        }
        else
        {
            var ev = new CanSeeAttemptEvent();
            RaiseLocalEvent(blindable.Owner, ev);
            blindable.Comp.IsBlind = ev.Blind;
        }

        if (old == blindable.Comp.IsBlind)
            return;

        var changeEv = new BlindnessChangedEvent(blindable.Comp.IsBlind);
        RaiseLocalEvent(blindable.Owner, ref changeEv);
        Dirty(blindable);
    }

    public void AdjustEyeDamage(Entity<BlindableComponent?> blindable, int amount)
    {
        if (!Resolve(blindable, ref blindable.Comp, false) || amount == 0)
            return;

        blindable.Comp.EyeDamage += amount;
        blindable.Comp.EyeDamage = Math.Clamp(blindable.Comp.EyeDamage, 0, BlindableComponent.MaxDamage);
        Dirty(blindable);
        UpdateIsBlind(blindable);

        var ev = new EyeDamageChangedEvent(blindable.Comp.EyeDamage);
        RaiseLocalEvent(blindable.Owner, ref ev);
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
public record struct EyeDamageChangedEvent(int Damage);

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
