using Content.Shared.Hands;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Lube;

public sealed partial class LubedSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private NameModifierSystem _nameMod = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<LubedComponent> ent, ref ComponentInit args)
    {
        _nameMod.RefreshNameModifiers(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnHandPickUp(Entity<LubedComponent> ent, ref BeforeGettingEquippedHandEvent args)
    {
        PerformLubedEffect(ent, args.User, out var cancelHandPickup);
        args.Cancelled = cancelHandPickup;
    }

    [SubscribeLocalEvent]
    private void OnRefreshNameModifiers(Entity<LubedComponent> ent, ref RefreshNameModifiersEvent args)
    {
        if (ent.Comp.SlipsLeft > 0) // The component is removed deferred, so it might still exist when we refresh.
            args.AddModifier("lubed-name-prefix");
    }

    [SubscribeLocalEvent]
    private void OnGluedEffectAttemptEvent(Entity<LubedImmuneComponent> entity, ref LubedEffectAttemptEvent args)
    {
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnGluedEffectAttemptEvent(Entity<LubedImmuneComponent> entity, ref InventoryRelayedEvent<LubedEffectAttemptEvent> args)
    {
        OnGluedEffectAttemptEvent(entity, ref args.Args);
    }

    public void PerformLubedEffect(Entity<LubedComponent> ent, EntityUid user, out bool cancelHandPickup)
    {
        cancelHandPickup = false;

        var attemptEv = new LubedEffectAttemptEvent();
        RaiseLocalEvent(user, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        // Throwing is not predicted yet, so we don't want to predict setting the coordinates either, or it will look weird.
        if (_net.IsServer)
        {
            cancelHandPickup = true;
            _transform.SetCoordinates(ent, Transform(user).Coordinates);
            _transform.AttachToGridOrMap(ent);
            _throwing.TryThrow(ent, _random.NextVector2(), baseThrowSpeed: ent.Comp.SlipStrength, user: user);
            _popup.PopupEntity(Loc.GetString("lube-slip", ("target", Identity.Entity(ent, EntityManager))), user, user, PopupType.MediumCaution);
        }

        ent.Comp.SlipsLeft--;
        Dirty(ent);
        if (ent.Comp.SlipsLeft <= 0)
        {
            RemCompDeferred<LubedComponent>(ent);
            _nameMod.RefreshNameModifiers(ent.Owner);
        }
    }
}

/// <summary>
/// Raised on an entity to determine if it will be affected by a lubed item or not.
/// </summary>
[ByRefEvent]
public record struct LubedEffectAttemptEvent() : IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.GLOVES;

    /// <summary>
    /// If true, prevents the effect from being applied.
    /// </summary>
    public bool Cancelled;
}
