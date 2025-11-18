using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed partial class ShakeableSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShakeableComponent, GetVerbsEvent<Verb>>(AddShakeVerb);
        SubscribeLocalEvent<ShakeableComponent, ShakeDoAfterEvent>(OnShakeDoAfter);
    }

    private void AddShakeVerb(EntityUid uid, ShakeableComponent component, GetVerbsEvent<Verb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        if (!CanShake((uid, component), args.User))
            return;

        var shakeVerb = new Verb()
        {
            Text = Loc.GetString(component.ShakeVerbText),
            Act = () => TryStartShake((args.Target, component), args.User)
        };
        args.Verbs.Add(shakeVerb);
    }

    private void OnShakeDoAfter(Entity<ShakeableComponent> entity, ref ShakeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        TryShake((entity, entity.Comp), args.User);
    }

    /// <summary>
    /// Attempts to start the doAfter to shake the entity.
    /// Fails and returns false if the entity cannot be shaken for any reason.
    /// If successful, displays popup messages, plays shake sound, and starts the doAfter.
    /// </summary>
    public bool TryStartShake(Entity<ShakeableComponent?> entity, EntityUid user)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (!CanShake(entity, user))
            return false;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            entity.Comp.ShakeDuration,
            new ShakeDoAfterEvent(),
            eventTarget: entity,
            target: user,
            used: entity)
        {
            NeedHand = true,
            BreakOnDamage = true,
            DistanceThreshold = 1,
            MovementThreshold = 0.01f,
            BreakOnHandChange = entity.Comp.RequireInHand,
        };
        if (entity.Comp.RequireInHand)
            doAfterArgs.BreakOnHandChange = true;

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return false;

        var userName = Identity.Entity(user, EntityManager);
        var shakeableName = Identity.Entity(entity, EntityManager);

        var selfMessage = Loc.GetString(entity.Comp.ShakePopupMessageSelf, ("user", userName), ("shakeable", shakeableName));
        var othersMessage = Loc.GetString(entity.Comp.ShakePopupMessageOthers, ("user", userName), ("shakeable", shakeableName));
        _popup.PopupPredicted(selfMessage, othersMessage, user, user);

        _audio.PlayPredicted(entity.Comp.ShakeSound, entity, user);

        return true;
    }

    /// <summary>
    /// Attempts to shake the entity, skipping the doAfter.
    /// Fails and returns false if the entity cannot be shaken for any reason.
    /// If successful, raises a ShakeEvent on the entity.
    /// </summary>
    public bool TryShake(Entity<ShakeableComponent?> entity, EntityUid? user = null)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (!CanShake(entity, user))
            return false;

        var ev = new ShakeEvent(user);
        RaiseLocalEvent(entity, ref ev);

        return true;
    }


    /// <summary>
    /// Is it possible for the given user to shake the entity?
    /// </summary>
    public bool CanShake(Entity<ShakeableComponent?> entity, EntityUid? user = null)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        // If required to be in hand, fail if the user is not holding this entity
        if (user != null && entity.Comp.RequireInHand && !_hands.IsHolding(user.Value, entity, out _))
            return false;

        var attemptEv = new AttemptShakeEvent();
        RaiseLocalEvent(entity, ref attemptEv);
        if (attemptEv.Cancelled)
            return false;
        return true;
    }
}

/// <summary>
/// Raised when a ShakeableComponent is shaken, after the doAfter completes.
/// </summary>
[ByRefEvent]
public record struct ShakeEvent(EntityUid? Shaker);

/// <summary>
/// Raised when trying to shake a ShakeableComponent. If cancelled, the
/// entity will not be shaken.
/// </summary>
[ByRefEvent]
public record struct AttemptShakeEvent()
{
    public bool Cancelled;
}

[Serializable, NetSerializable]
public sealed partial class ShakeDoAfterEvent : SimpleDoAfterEvent
{
}
