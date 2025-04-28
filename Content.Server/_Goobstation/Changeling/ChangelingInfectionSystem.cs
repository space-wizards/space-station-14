using Robust.Shared.Timing;
using Robust.Server.GameObjects;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Antag;
using Content.Shared.Changeling;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Server.Body.Systems;
using Content.Shared.Store.Components;
using Robust.Shared.Random;
using Content.Shared.Bed.Sleep;
using Content.Shared.Popups;
using Content.Shared.Jittering;
using Content.Shared.Stunnable;
using Content.Server.Medical;
using Content.Shared.Tag;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;

namespace Content.Server.Changeling;

public sealed partial class ChangelingInfectionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedJitteringSystem _jitterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    [Dependency] private readonly VomitSystem _vomit = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingInfectionImplantComponent, ImplantImplantedEvent>(OnImplanterInjected);
    }

    private void OnImplanterInjected(EntityUid uid, ChangelingInfectionImplantComponent comp, ImplantImplantedEvent ev)
    {
        if (!_tag.HasTag(ev.Implant, "ChangelingInfectionImplant") || ev.Implanted == null)
            return;

        if (!EntityManager.TryGetComponent(ev.Implanted.Value, out AbsorbableComponent? absorbable))
        {
            _popupSystem.PopupEntity(Loc.GetString("changeling-convert-implant-fail"), ev.Implanted.Value, ev.Implanted.Value, PopupType.MediumCaution);
            return;
        }

        EnsureComp<ChangelingInfectionComponent>(ev.Implanted.Value);

        _popupSystem.PopupEntity(Loc.GetString("changeling-convert-implant"), ev.Implanted.Value, ev.Implanted.Value, PopupType.LargeCaution);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        foreach (var comp in EntityManager.EntityQuery<ChangelingInfectionComponent>())
        {
            var uid = comp.Owner;

            if (!EntityManager.TryGetComponent(uid, out AbsorbableComponent? absorbable))
            {
                EntityManager.RemoveComponent<ChangelingInfectionComponent>(uid);
                return;
            }

            if (!comp.NeedsInitialization)
            {
                comp.FirstSymptoms = _timing.CurTime + TimeSpan.FromSeconds(comp.FirstSymptomsDelay);

                comp.KnockedOut = _timing.CurTime + TimeSpan.FromSeconds(comp.KnockedOutDelay);

                comp.FullyInfected = _timing.CurTime + TimeSpan.FromSeconds(comp.FullyInfectedDelay);
            }

            if (_timing.CurTime > comp.FirstSymptoms)
            {
                comp.CurrentState = ChangelingInfectionComponent.InfectionState.FirstSymptoms;
                comp.FirstSymptoms = _timing.CurTime + TimeSpan.FromHours(24f); // Don't fire again
            }
            else if (_timing.CurTime > comp.KnockedOut)
            {
                comp.CurrentState = ChangelingInfectionComponent.InfectionState.KnockedOut;
                comp.KnockedOut = _timing.CurTime + TimeSpan.FromHours(24f); // Hacky solution 2: Electric Boogaloo
            }
            else if (_timing.CurTime > comp.FullyInfected)
            {
                comp.CurrentState = ChangelingInfectionComponent.InfectionState.FullyInfected;
                comp.FullyInfected = _timing.CurTime + TimeSpan.FromHours(24f); // Ehhhhh nobody's gonna see this the component is getting removed in a tick anyway!
            }

            if (_timing.CurTime < comp.EffectsTimer)
                continue;

            comp.EffectsTimer = _timing.CurTime + TimeSpan.FromSeconds(comp.EffectsTimerDelay);

            if (comp.NeedsInitialization)
                DoEffects(uid, comp);

            comp.NeedsInitialization = true; // First tick over, setup's complete, we can do the stuff now

        }
    }
    public void DoEffects(EntityUid uid, ChangelingInfectionComponent comp)
    {
        // Switch statement to determine which stage of infection we're in

        switch (comp.CurrentState)
        {
            case ChangelingInfectionComponent.InfectionState.FirstSymptoms:
                if (_random.Prob(comp.ScarySymptomChance))
                {
                    var funnyNumber = _random.Next(0, 4);
                    switch (funnyNumber)
                    {
                        case 1:
                            _popupSystem.PopupEntity(Loc.GetString("changeling-convert-warning-throwup"), uid, uid, PopupType.Medium);
                            _vomit.Vomit(uid);
                            break;
                        case 2:
                            _popupSystem.PopupEntity(Loc.GetString("changeling-convert-warning-collapse"), uid, uid, PopupType.Medium);
                            _stun.TryParalyze(uid, TimeSpan.FromSeconds(5f), true);
                            break;
                        case 3:
                            _popupSystem.PopupEntity(Loc.GetString("changeling-convert-warning-shake"), uid, uid, PopupType.Medium);
                            _jitterSystem.DoJitter(uid, TimeSpan.FromSeconds(5f), false, 10.0f, 4.0f);
                            break;
                    }
                    break;
                }

                _popupSystem.PopupEntity(Loc.GetString(_random.Pick(comp.SymptomMessages)), uid, uid);


                break;
            case ChangelingInfectionComponent.InfectionState.KnockedOut:
                // Add forced knocked out component
                if (!EntityManager.HasComponent<ForcedSleepingComponent>(uid))
                {
                    EntityManager.AddComponent<ForcedSleepingComponent>(uid);
                    _popupSystem.PopupEntity(Loc.GetString("changeling-convert-eeped"), uid, uid, PopupType.LargeCaution);
                    break;
                }
                if (_random.Prob(comp.ScarySymptomChance))
                {
                    _jitterSystem.DoJitter(uid, TimeSpan.FromSeconds(5f), false, 10.0f, 4.0f);
                    _popupSystem.PopupEntity(Loc.GetString("changeling-convert-eeped-shake"), uid, uid, PopupType.Medium);
                    break;
                }
                _popupSystem.PopupEntity(Loc.GetString(_random.Pick(comp.EepyMessages)), uid, uid);
                break;
            case ChangelingInfectionComponent.InfectionState.FullyInfected:
                // This will totally have no adverse effects whatsoever!
                if (!HasComp<MindContainerComponent>(uid) || !TryComp<ActorComponent>(uid, out var targetActor))
                    return;
                _antag.ForceMakeAntag<ChangelingRuleComponent>(targetActor.PlayerSession, "Changeling");

                EntityManager.RemoveComponent<ChangelingInfectionComponent>(uid);

                _popupSystem.PopupEntity(Loc.GetString("changeling-convert-skillissue"), uid, uid);
                if (EntityManager.HasComponent<ForcedSleepingComponent>(uid))
                    EntityManager.RemoveComponent<ForcedSleepingComponent>(uid);

                break;
            case ChangelingInfectionComponent.InfectionState.None:
                break;
        }
    }
}

