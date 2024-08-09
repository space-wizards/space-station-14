using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Weapons.Melee.Balloon;

/// <summary>
/// This handles popping ballons when attacked with <see cref="BalloonPopperComponent"/>
/// </summary>
public sealed class BalloonPopperSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BalloonPopperComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<BalloonPopperComponent, ThrowDoHitEvent>(OnThrowHit);
    }

    private void OnMeleeHit(EntityUid uid, BalloonPopperComponent component, MeleeHitEvent args)
    {
        foreach (var entity in args.HitEntities)
        {
            foreach (var held in _hands.EnumerateHeld(entity))
            {
                if (_tag.HasTag(held, component.BalloonTag))
                    PopBallooon(uid, held, component);
            }

            if (_tag.HasTag(entity, component.BalloonTag))
                PopBallooon(uid, entity, component);
        }
    }

    private void OnThrowHit(EntityUid uid, BalloonPopperComponent component, ThrowDoHitEvent args)
    {
        foreach (var held in _hands.EnumerateHeld(args.Target))
        {
            if (_tag.HasTag(held, component.BalloonTag))
                PopBallooon(uid, held, component);
        }
    }

    /// <summary>
    /// Pops a target balloon, making a popup and playing a sound.
    /// </summary>
    public void PopBallooon(EntityUid popper, EntityUid balloon, BalloonPopperComponent? component = null)
    {
        if (!Resolve(popper, ref component))
            return;

        _audio.PlayPvs(component.PopSound, balloon);
        _popup.PopupCoordinates(Loc.GetString("melee-balloon-pop",
            ("balloon", Identity.Entity(balloon, EntityManager))), Transform(balloon).Coordinates, PopupType.Large);
        QueueDel(balloon);
    }
}
