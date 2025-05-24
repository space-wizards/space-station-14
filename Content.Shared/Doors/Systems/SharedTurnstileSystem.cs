using Content.Shared.Access.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Doors.Systems;

/// <summary>
/// This handles logic and interactions related to <see cref="TurnstileComponent"/>
/// </summary>
public abstract partial class SharedTurnstileSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TurnstileComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<TurnstileComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<TurnstileComponent, EndCollideEvent>(OnEndCollide);
    }

    private void OnPreventCollide(Entity<TurnstileComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled || !args.OurFixture.Hard || !args.OtherFixture.Hard)
            return;

        if (ent.Comp.CollideExceptions.Contains(args.OtherEntity))
        {
            args.Cancelled = true;
            return;
        }

        // We need to add this in here too for chain pulls
        if (_pulling.GetPuller(args.OtherEntity) is { } puller && ent.Comp.CollideExceptions.Contains(puller))
        {
            ent.Comp.CollideExceptions.Add(args.OtherEntity);
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

        if (CanPassDirection(ent, args.OtherEntity))
        {
            if (!_accessReader.IsAllowed(args.OtherEntity, ent))
                return;

            ent.Comp.CollideExceptions.Add(args.OtherEntity);
            if (_pulling.GetPulling(args.OtherEntity) is { } uid)
                ent.Comp.CollideExceptions.Add(uid);

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
        if (!ent.Comp.CollideExceptions.Contains(args.OtherEntity))
        {
            if (CanPassDirection(ent, args.OtherEntity))
            {
                if (!_accessReader.IsAllowed(args.OtherEntity, ent))
                {
                    _audio.PlayPredicted(ent.Comp.DenySound, ent, args.OtherEntity);
                    PlayAnimation(ent, ent.Comp.DenyState);
                }
            }

            return;
        }
        // if they passed through:
        PlayAnimation(ent, ent.Comp.SpinState);
        _audio.PlayPredicted(ent.Comp.TurnSound, ent, args.OtherEntity);
    }

    private void OnEndCollide(Entity<TurnstileComponent> ent, ref EndCollideEvent args)
    {
        if (!args.OurFixture.Hard)
        {
            ent.Comp.CollideExceptions.Remove(args.OtherEntity);
            Dirty(ent);
        }
    }

    protected bool CanPassDirection(Entity<TurnstileComponent> ent, EntityUid other)
    {
        var xform = Transform(ent);
        var otherXform = Transform(other);

        var (pos, rot) = _transform.GetWorldPositionRotation(xform);
        var otherPos = _transform.GetWorldPosition(otherXform);

        var approachAngle = (pos - otherPos).ToAngle();
        var rotateAngle = rot.ToWorldVec().ToAngle();

        var diff = Math.Abs(approachAngle - rotateAngle);
        diff %= MathHelper.TwoPi;
        if (diff > Math.PI)
            diff = MathHelper.TwoPi - diff;

        return diff < Math.PI / 4;
    }

    protected virtual void PlayAnimation(EntityUid uid, string stateId)
    {

    }
}
