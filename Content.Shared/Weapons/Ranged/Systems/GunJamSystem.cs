using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Ranged.Systems;

/// <summary>
/// Handles per-shot jam chance for guns with <see cref="GunJamComponent"/>.
/// A jammed gun cannot fire until the player racks the slide (Z / Use In Hand).
/// </summary>
public sealed class GunJamSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunJamComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<GunJamComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<GunJamComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnAttemptShoot(Entity<GunJamComponent> ent, ref AttemptShootEvent args)
    {
        if (!ent.Comp.IsJammed)
            return;

        args.Cancelled = true;
        _popup.PopupEntity(Loc.GetString("gun-jam-blocked"), ent, args.User, PopupType.SmallCaution);
    }

    private void OnGunShot(Entity<GunJamComponent> ent, ref GunShotEvent args)
    {
        if (ent.Comp.IsJammed)
            return;

        if (!SharedRandomExtensions.PredictedProb(_timing, ent.Comp.JamChance, GetNetEntity(ent)))
            return;

        ent.Comp.IsJammed = true;
        Dirty(ent, ent.Comp);
        _popup.PopupEntity(Loc.GetString("gun-jam-jammed"), ent, args.User, PopupType.SmallCaution);
    }

    private void OnUseInHand(Entity<GunJamComponent> ent, ref UseInHandEvent args)
    {
        if (!ent.Comp.IsJammed)
            return;

        ent.Comp.IsJammed = false;
        Dirty(ent, ent.Comp);
        _popup.PopupEntity(Loc.GetString("gun-jam-cleared"), ent, args.User);
    }
}
