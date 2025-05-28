using System.Linq;
using Content.Shared.Access.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Doors.Systems;

/// <summary>
/// This handles logic and interactions related to <see cref="TurnstileComponent"/>
/// </summary>
public abstract class SharedTurnstileSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TurnstileComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<TurnstileComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<TurnstileComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<TurnstileComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<TurnstileComponent, BeforePryEvent>(OnBeforePry);
        SubscribeLocalEvent<TurnstileComponent, GetPryTimeModifierEvent>(OnGetPryMod);
        SubscribeLocalEvent<TurnstileComponent, PriedEvent>(OnAfterPry);
    }

    private void OnPreventCollide(Entity<TurnstileComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled || !args.OurFixture.Hard || !args.OtherFixture.Hard)
            return;

        if (ent.Comp.CollideExceptions.ContainsKey(args.OtherEntity))
        {
            args.Cancelled = true;
            return;
        }

        // We need to add this in here too for chain pulls
        if (_pulling.GetPuller(args.OtherEntity) is { } puller
            && ent.Comp.CollideExceptions.TryGetValue(puller, out var pullerMethod))
        {
            var pullerPulled = pullerMethod is EntranceMethod.Pulled or EntranceMethod.ChainPulled;
            var method = pullerPulled ? EntranceMethod.ChainPulled : EntranceMethod.Pulled;
            ent.Comp.CollideExceptions[args.OtherEntity] = method;
            Dirty(ent);
            args.Cancelled = true;
            return;
        }

        // unblockables go through for free.
        if (_entityWhitelist.IsWhitelistFail(ent.Comp.ProcessWhitelist, args.OtherEntity))
        {
            args.Cancelled = true;
            return;
        }

        if (ent.Comp.PriedExceptions.ContainsKey(args.OtherEntity))
        {
            ent.Comp.CollideExceptions[args.OtherEntity] = EntranceMethod.Forced;
            if (_pulling.GetPulling(args.OtherEntity) is { } uid)
                ent.Comp.CollideExceptions[uid] = EntranceMethod.Pulled;

            ent.Comp.PriedExceptions.Remove(args.OtherEntity);
            args.Cancelled = true;
            Dirty(ent);
            return;
        }

        if (CanPassDirection(ent, args.OtherEntity))
        {
            var knownAccessBroken = ent.Comp.ObviouslyAccessBroken || ent.Comp.AccessBroken;

            if (!knownAccessBroken && !_accessReader.IsAllowed(args.OtherEntity, ent))
                return;

            var method = knownAccessBroken ? EntranceMethod.AccessBroken : EntranceMethod.Access;
            ent.Comp.CollideExceptions[args.OtherEntity] = method;
            if (_pulling.GetPulling(args.OtherEntity) is { } uid)
                ent.Comp.CollideExceptions[uid] = EntranceMethod.Pulled;

            args.Cancelled = true;
            Dirty(ent);
        }
        else
        {
            if (_timing.CurTime >= ent.Comp.NextResistTime)
            {
                _popup.PopupClient(Loc.GetString("turnstile-component-popup-resist", ("turnstile", ent.Owner)), ent, args.OtherEntity);
                ent.Comp.NextResistTime = _timing.CurTime + TimeSpan.FromSeconds(0.1);
                Dirty(ent);
            }
        }
    }

    private void OnStartCollide(Entity<TurnstileComponent> ent, ref StartCollideEvent args)
    {
        var knownAccessBroken = ent.Comp.AccessBroken || ent.Comp.ObviouslyAccessBroken;
        if (!ent.Comp.CollideExceptions.TryGetValue(args.OtherEntity, out var method))
        {
            // Will not be allowed through!

            // If entered from the wrong direction, no animation or anything
            if (!CanPassDirection(ent, args.OtherEntity))
                return;

            // If we know that access is broken, don't play a deny sound
            if (knownAccessBroken)
                return;

            // Don't play a deny sound if access is allowed
            if (_accessReader.IsAllowed(args.OtherEntity, ent))
                return;

            // Finally, play a denial sound and animation
            _audio.PlayPredicted(ent.Comp.DenySound, ent, args.OtherEntity);
            PlayAnimation(ent, TurnstileVisualLayers.Indicators, ent.Comp.DenyState);

            return;
        }

        // if they passed through:
        if(!knownAccessBroken)  // it's already spinnin', don't play animation
            PlayAnimation(ent, TurnstileVisualLayers.Spinner, ent.Comp.SpinState);

        if(method == EntranceMethod.Access) // The access reader was used, show the indicator lights
            PlayAnimation(ent, TurnstileVisualLayers.Indicators, ent.Comp.GrantedState);

        // Always play the turn sound!
        _audio.PlayPredicted(ent.Comp.TurnSound, ent, args.OtherEntity);
    }

    private void OnEndCollide(Entity<TurnstileComponent> ent, ref EndCollideEvent args)
    {
        if (args.OurFixture.Hard)
            return;

        ent.Comp.CollideExceptions.Remove(args.OtherEntity);
        Dirty(ent);
    }

    private bool CanPassDirection(Entity<TurnstileComponent> ent, EntityUid other)
    {
        var xform = Transform(ent);
        var otherXform = Transform(other);

        var (pos, rot) = _transform.GetWorldPositionRotation(xform);
        if(ent.Comp.Flipped)
            rot += Angle.FromDegrees(180);
        var otherPos = _transform.GetWorldPosition(otherXform);

        var approachAngle = (pos - otherPos).ToAngle();
        var rotateAngle = rot.ToWorldVec().ToAngle();

        var diff = Math.Abs(approachAngle - rotateAngle);
        diff %= MathHelper.TwoPi;
        if (diff > Math.PI)
            diff = MathHelper.TwoPi - diff;

        return diff < Math.PI / 4;
    }

    protected virtual void PlayAnimation(EntityUid uid, TurnstileVisualLayers layer, string stateId) { }

    private void OnEmagged(Entity<TurnstileComponent> ent, ref GotEmaggedEvent args)
    {
        switch (args.Type)
        {
            case EmagType.Interaction:
                ent.Comp.Flipped = !ent.Comp.Flipped;
                break;
            case EmagType.Access:
                ent.Comp.AccessBroken = true;
                ent.Comp.ObviouslyAccessBroken = true;
                _appearance.SetData(ent, TurnstileVisuals.AccessBrokenSpinning, true);
                break;
            case EmagType.None:
            default:
                return;
        }
        args.Handled = true;
        args.Repeatable = true;
        Dirty(ent);
    }

    public void SetSolenoidBypassed(Entity<TurnstileComponent> ent, bool value)
    {
        ent.Comp.SolenoidBypassed = value;
        Dirty(ent);
    }

    private void OnBeforePry(Entity<TurnstileComponent> ent, ref BeforePryEvent args)
    {
        if (!args.CanPry)
            return;

        if (ent.Comp.SolenoidBypassed || args.Strength >= PryStrength.Powered)
            return;

        args.Message = Loc.GetString("turnstile-component-popup-resist", ("turnstile", ent.Owner));
        args.CanPry = false;
    }

    private void OnGetPryMod(Entity<TurnstileComponent> ent, ref GetPryTimeModifierEvent args)
    {
        if (!ent.Comp.SolenoidBypassed)
            args.PryTimeModifier *= ent.Comp.PoweredPryModifier;

        if (!CanPassDirection(ent, args.User))
            args.PryTimeModifier *= ent.Comp.WrongDirectionPryModifier;
    }

    private void OnAfterPry(Entity<TurnstileComponent> ent, ref PriedEvent args)
    {
        ent.Comp.PriedExceptions[args.User] = _timing.CurTime + ent.Comp.PryExpirationTime;
        _popup.PopupClient(Loc.GetString("turnstile-component-popup-forced", ("turnstile", ent.Owner)), ent.Owner, args.User);
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TurnstileComponent>();
        while (query.MoveNext(out var uid, out var turnstile))
        {
            var curTime = _timing.CurTime;
            var expired = turnstile.PriedExceptions.Where(pair => curTime > pair.Value);
            foreach (var (expiredUid, _) in expired)
            {
                turnstile.PriedExceptions.Remove(expiredUid);
                if(expiredUid == _playerManager.LocalEntity)
                    _popup.PopupClient(Loc.GetString("turnstile-component-popup-force-expired", ("turnstile", uid)), uid, expiredUid);
            }
        }
    }
}
