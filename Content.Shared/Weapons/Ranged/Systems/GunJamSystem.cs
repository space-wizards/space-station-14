using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Ranged.Systems;

// Handles per-shot jam chance for guns with GunJamDefectComponent.
// A jammed gun cannot fire until the player racks the slide (Z / Use In Hand).
public sealed class GunJamSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunJamDefectComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<GunJamDefectComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<GunJamDefectComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnAttemptShoot(Entity<GunJamDefectComponent> ent, ref AttemptShootEvent args)
    {
        if (!ent.Comp.IsJammed)
            return;

        args.Cancelled = true;

        // Rate-limit the popup to avoid flooding the screen when the player holds the trigger.
        if (_timing.CurTime < ent.Comp.NextPopupTime)
            return;

        ent.Comp.NextPopupTime = _timing.CurTime + ent.Comp.PopupCooldown;
        _popup.PopupEntity(Loc.GetString("gun-jam-blocked"), ent, args.User, PopupType.SmallCaution);
    }

    private void OnGunShot(Entity<GunJamDefectComponent> ent, ref GunShotEvent args)
    {
        if (ent.Comp.IsJammed)
            return;

        if (!SharedRandomExtensions.PredictedProb(_timing, ent.Comp.JamChance, GetNetEntity(ent)))
            return;

        ent.Comp.IsJammed = true;
        Dirty(ent, ent.Comp);

        _audio.PlayPredicted(ent.Comp.SoundJamRack, ent.Owner, args.User);
        _popup.PopupEntity(Loc.GetString("gun-jam-jammed"), ent, args.User, PopupType.SmallCaution);
    }

    private void OnUseInHand(Entity<GunJamDefectComponent> ent, ref UseInHandEvent args)
    {
        if (!ent.Comp.IsJammed)
            return;

        ent.Comp.IsJammed = false;
        Dirty(ent, ent.Comp);

        if (_timing.CurTime < ent.Comp.NextPopupTime)
            return;

        ent.Comp.NextPopupTime = _timing.CurTime + ent.Comp.PopupCooldown;
        _popup.PopupEntity(Loc.GetString("gun-jam-cleared"), ent, args.User);
    }
}
