// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Revenant.Components;
using Content.Server.Ghost;
using Content.Shared.Mind;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Ghost;
using Content.Shared.Corvax.TTS;
using Content.Shared.Damage.Components;
using Content.Shared.Mind.Components;
using Content.Shared.DeadSpace.Languages.Components;
using Robust.Shared.Containers;
using Content.Shared.Mobs;
using Content.Shared.Actions; //DS14
using Content.Shared.Polymorph;
using Content.Shared.Revenant.Components; //DS14

namespace Content.Server.Revenant.EntitySystems;

public sealed class RevenantMindCapturedSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!; //DS14

    public override void Initialize()
    {
        SubscribeLocalEvent<RevenantMindCapturedComponent, MindUnvisitedMessage>(OnUnvisited);
        SubscribeLocalEvent<RevenantMindCapturedComponent, MobStateChangedEvent>(OnStateChange);
        SubscribeLocalEvent<RevenantMindCapturedComponent, PolymorphAttemptEvent>(OnPolymorphAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<RevenantMindCapturedComponent>();

        while (enumerator.MoveNext(out var uid, out var comp))
        {
            comp.Accumulator += frameTime;

            if (_mind.TryGetMind(comp.RevenantUid, out var _, out var mind)
            && mind.IsVisitingEntity
            && mind.VisitingEntity != uid)
                EndCapture(uid, comp);

            if (comp.Accumulator < comp.DurationOfCapture)
                continue;

            EndCapture(uid, comp);
        }
    }

    private void OnStateChange(EntityUid uid, RevenantMindCapturedComponent comp, MobStateChangedEvent args)
    {
        if (!_mobState.IsAlive(uid))
            EndCapture(uid, comp);
    }

    private void OnPolymorphAttempt(
        Entity<RevenantMindCapturedComponent> ent,
        ref PolymorphAttemptEvent args)
    {
        args.Cancelled = true;
        EndCapture(ent, ent.Comp, killHost: true);
    }

    private void EndCapture(EntityUid uid, RevenantMindCapturedComponent comp, bool killHost = false)
    {
        if (comp.EndingCapture)
            return;

        comp.EndingCapture = true;

        if (_mobThresholdSystem.TryGetThresholdForState(uid, MobState.Critical, out var crit))
            _mobThresholdSystem.SetMobStateThreshold(uid, comp.CritThreshold, MobState.Critical);

        if (_mobThresholdSystem.TryGetThresholdForState(uid, MobState.Dead, out var dead))
        {
            _mobThresholdSystem.SetMobStateThreshold(uid, comp.DeadThreshold, MobState.Dead);

            if (TryComp<DamageableComponent>(uid, out var damageable) && damageable.TotalDamage > dead.Value)
                _mobState.ChangeMobState(uid, MobState.Dead);
        }

        if (_container.IsEntityInContainer(comp.RevenantUid))
            _container.EmptyContainer(comp.RevenantContainer);

        if (TryComp<TTSComponent>(uid, out var tts))
        {
            if (!string.IsNullOrEmpty(comp.ReturnTTSPrototype))
                tts.VoicePrototypeId = comp.ReturnTTSPrototype;
            else
                tts.VoicePrototypeId = null;
        }

        if (TryComp<LanguageComponent>(uid, out var language))
        {
            language.CantSpeakLanguages = comp.ReturnCantSpeakLanguages;
            language.KnownLanguages = comp.ReturnKnownLanguages;
        }

        //DS14-Start
        if (TryComp<RevenantComponent>(comp.RevenantUid, out var revenantComp))
        {
            _actions.RemoveAction(uid, revenantComp.HackActionEntity);
        }
        //DS14-End

        if (_mind.TryGetMind(comp.RevenantUid, out var mindId, out var mind) && mind.VisitingEntity == uid)
            _mind.UnVisit(mindId, mind);

        if (killHost)
            _mobState.ChangeMobState(uid, MobState.Dead);

        RemCompDeferred(uid, comp);
    }
    private void OnUnvisited(EntityUid uid, RevenantMindCapturedComponent comp, MindUnvisitedMessage args)
    {
        if (TryComp<GhostComponent>(comp.TargetUid, out var ghostComponent))
            _ghost.SetCanReturnToBody((comp.TargetUid, ghostComponent), true);
    }
}
