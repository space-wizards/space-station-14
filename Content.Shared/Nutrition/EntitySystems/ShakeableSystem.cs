using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
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

    private void OnShakeDoAfter(EntityUid uid, ShakeableComponent shakeable, ShakeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        TryShake((uid, shakeable), args.User);
    }

    public bool TryStartShake(Entity<ShakeableComponent?> entity, EntityUid user)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (!CanShake(entity, user))
            return false;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, entity.Comp.ShakeDuration, new ShakeDoAfterEvent(), entity)
        {
            BreakOnDamage = true,
        };
        if (entity.Comp.RequireInHand)
            doAfterArgs.BreakOnHandChange = true;

        _doAfter.TryStartDoAfter(doAfterArgs);

        var userName = Identity.Entity(user, EntityManager);
        var shakeableName = Identity.Entity(entity, EntityManager);

        _popup.PopupEntity(Loc.GetString(entity.Comp.ShakePopupMessageOthers, ("user", userName), ("shakeable", shakeableName)), user, Filter.PvsExcept(user), true);
        _popup.PopupClient(Loc.GetString(entity.Comp.ShakePopupMessageSelf, ("user", userName), ("shakeable", shakeableName)), user, user);

        _audio.PlayPredicted(entity.Comp.ShakeSound, entity, user);

        return true;
    }

    public bool TryShake(Entity<ShakeableComponent?> entity, EntityUid user)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (!CanShake(entity, user))
            return false;

        var ev = new ShakeEvent(user);
        RaiseLocalEvent(entity, ref ev);

        return true;
    }

    public bool CanShake(Entity<ShakeableComponent?> entity, EntityUid? user = null)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        // If required to be in hand, fail if there's no user or the user is not holding this entity
        if (entity.Comp.RequireInHand && (user == null || !_hands.IsHolding(user.Value, entity, out _)))
            return false;

        var attemptEv = new AttemptShakeEvent();
        RaiseLocalEvent(entity, attemptEv);
        if (attemptEv.Cancelled)
            return false;
        return true;
    }
}

[ByRefEvent]
public record struct ShakeEvent(EntityUid Shaker);

public sealed class AttemptShakeEvent : CancellableEntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class ShakeDoAfterEvent : SimpleDoAfterEvent
{
}
