using Content.Shared.Chemistry.Hypospray.Events;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class HyposprayBackfireStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HyposprayBackfireStatusEffectComponent, StatusEffectRelayedEvent<SelfBeforeHyposprayInjectsEvent>>(OnSelfBeforeHyposprayInjects);
    }

    private void OnSelfBeforeHyposprayInjects(Entity<HyposprayBackfireStatusEffectComponent> ent, ref StatusEffectRelayedEvent<SelfBeforeHyposprayInjectsEvent> args)
    {
        if (Comp<StatusEffectComponent>(ent).AppliedTo is not { } target)
            return;

        if (args.Args.TargetGettingInjected == args.Args.EntityUsingHypospray)
            return;

        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
        var rand = new System.Random(seed);
        if (!rand.Prob(ent.Comp.Probability))
            return;

        args.Args.TargetGettingInjected = args.Args.EntityUsingHypospray;

        _stun.TryUpdateParalyzeDuration(target, ent.Comp.BackfireStunTime);
        _audio.PlayPvs(ent.Comp.BackfireSound, target);
        _popup.PopupEntity(Loc.GetString(ent.Comp.BackfireMessage), target, target);
    }
}
