using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class GunBackfireStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunBackfireStunEvent>(OnGunBackfireStun);
        SubscribeLocalEvent<GunBackfireStatusEffectComponent, StatusEffectRelayedEvent<SelfBeforeGunShotEvent>>(OnSelfBeforeGunShot);
    }

    private void OnGunBackfireStun(GunBackfireStunEvent args)
    {
        _stun.TryUpdateParalyzeDuration(args.Target, args.Duration);
    }

    private void OnSelfBeforeGunShot(Entity<GunBackfireStatusEffectComponent> ent, ref StatusEffectRelayedEvent<SelfBeforeGunShotEvent> args)
    {
        if (Comp<StatusEffectComponent>(ent).AppliedTo is not { } target)
            return;

        if (args.Args.Cancelled)
            return;

        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(args.Args.Gun).Id });
        var rand = new System.Random(seed);
        if (!rand.Prob(ent.Comp.Probability))
            return;

        QueueLocalEvent(new GunBackfireStunEvent(target, ent.Comp.BackfireStunTime));
        _audio.PlayPvs(ent.Comp.BackfireSound, target);
        _popup.PopupEntity(Loc.GetString(ent.Comp.BackfireMessage), target, target);

        args.Args.Cancel();
    }

}

public sealed class GunBackfireStunEvent(EntityUid uid, TimeSpan duration) : EntityEventArgs
{
    public readonly EntityUid Target = uid;
    public readonly TimeSpan Duration = duration;
}
