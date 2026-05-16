using Content.Shared.Chemistry.Events;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed partial class HyposprayBackfireStatusEffectSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HyposprayBackfireStatusEffectComponent, StatusEffectRelayedEvent<SelfBeforeInjectEvent>>(OnSelfBeforeInjects);
    }

    private void OnSelfBeforeInjects(Entity<HyposprayBackfireStatusEffectComponent> ent, ref StatusEffectRelayedEvent<SelfBeforeInjectEvent> args)
    {
        if (Comp<StatusEffectComponent>(ent).AppliedTo is not { } target)
            return;

        if (args.Args.TargetGettingInjected == args.Args.EntityUsingInjector)
            return;

        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);
        if (!rand.Prob(ent.Comp.Probability))
            return;

        args.Args.TargetGettingInjected = args.Args.EntityUsingInjector;

        _stun.TryUpdateParalyzeDuration(target, ent.Comp.BackfireStunTime);
        _audio.PlayPvs(ent.Comp.BackfireSound, target);
        _popup.PopupEntity(Loc.GetString(ent.Comp.BackfireMessage), target, target);
    }
}
