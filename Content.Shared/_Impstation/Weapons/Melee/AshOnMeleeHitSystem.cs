using Content.Shared._EE.Supermatter.Components;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Impstation.Weapons.Melee;

public sealed class AshOnMeleeHitSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AshOnMeleeHitComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<AshOnMeleeHitComponent, ThrowDoHitEvent>(OnThrowHit);
    }

    private void OnMeleeHit(Entity<AshOnMeleeHitComponent> ent, ref MeleeHitEvent args)
    {
        if (args.Handled || args.HitEntities.Count < 1)
            return;

        var ashed = 0;

        foreach (var target in args.HitEntities)
        {
            Ash(ent, target);
            ashed++;
        }

        if (ashed == 0)
            return;

        _audio.PlayPvs(ent.Comp.Sound, Transform(ent).Coordinates);

        if (ent.Comp.SingleUse)
            EntityManager.QueueDeleteEntity(ent);
    }

    private void OnThrowHit(Entity<AshOnMeleeHitComponent> ent, ref ThrowDoHitEvent args)
    {
        if (args.Handled || HasComp<SupermatterImmuneComponent>(args.Target))
            return;

        Ash(ent, args.Target);
        _audio.PlayPvs(ent.Comp.Sound, Transform(ent).Coordinates);

        if (ent.Comp.SingleUse)
            EntityManager.QueueDeleteEntity(ent);
    }

    private void Ash(Entity<AshOnMeleeHitComponent> ent, EntityUid target)
    {
        var coords = Transform(target).Coordinates;

        _popup.PopupCoordinates(Loc.GetString(ent.Comp.Popup, ("entity", ent.Owner), ("target", target)), coords, PopupType.LargeCaution);

        EntityManager.SpawnEntity(ent.Comp.AshPrototype, coords);
        EntityManager.QueueDeleteEntity(target);
    }
}
