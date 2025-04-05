using Content.Shared.Access.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Animations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Doors.Systems;

/// <summary>
/// This handles logic and interactions related to <see cref="TurnstileComponent"/>
/// </summary>
public sealed class TurnstileSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedAnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TurnstileComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<TurnstileComponent, EndCollideEvent>(OnEndCollide);
    }

    private void OnPreventCollide(Entity<TurnstileComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled || !args.OurFixture.Hard)
            return;

        if (ent.Comp.CollideExceptions.Contains(args.OtherEntity))
        {
            // We need to add this in here too for chain pulls
            if (_pulling.GetPulling(args.OtherEntity) is { } uid &&
                ent.Comp.CollideExceptions.Add(uid))
            {
                Dirty(ent);
            }

            args.Cancelled = true;
            return;
        }

        // unblockables go through for free.
        if (_entityWhitelist.IsWhitelistFail(ent.Comp.ProcessWhitelist, args.OtherEntity))
        {
            args.Cancelled = true;
            return;
        }

        if (_timing.CurTime < ent.Comp.NextPassTime)
            return;

        if (!_accessReader.IsAllowed(args.OtherEntity, ent))
        {
            _audio.PlayPredicted(ent.Comp.DenySound, ent, args.OtherEntity);
            if (_net.IsClient)
                _animationPlayer.Flick(ent, ent.Comp.DenyState, TurnstileVisualLayers.Base);
            return;
        }

        var xform = Transform(ent);
        var otherXform = Transform(args.OtherEntity);

        var moveDir = (xform.LocalPosition - otherXform.LocalPosition).GetDir();
        var faceDir = xform.LocalRotation.GetDir();

        if (moveDir == faceDir)
        {
            ent.Comp.CollideExceptions.Add(args.OtherEntity);
            if (_pulling.GetPulling(args.OtherEntity) is { } uid)
                ent.Comp.CollideExceptions.Add(uid);

            args.Cancelled = true;
            ent.Comp.NextPassTime = _timing.CurTime + ent.Comp.PassDelay;
            if (_net.IsClient)
                _animationPlayer.Flick(ent, ent.Comp.SpinState, TurnstileVisualLayers.Base);
            _audio.PlayPredicted(ent.Comp.TurnSound, ent, args.OtherEntity);
            Dirty(ent);
        }
        else
        {
            if (_timing.CurTime >= ent.Comp.NextResistTime)
            {
                _popup.PopupPredicted(Loc.GetString("turnstile-component-popup-resist", ("turnstile", ent.Owner)), ent, args.OtherEntity);
                ent.Comp.NextResistTime = _timing.CurTime + TimeSpan.FromSeconds(0.5);
                Dirty(ent);
            }
        }
    }

    private void OnEndCollide(Entity<TurnstileComponent> ent, ref EndCollideEvent args)
    {
        if (!args.OurFixture.Hard)
        {
            ent.Comp.CollideExceptions.Remove(args.OtherEntity);
            Dirty(ent);
        }
    }
}
