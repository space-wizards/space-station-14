using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew;
using Content.Shared.Verbs;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.Wounds;

public sealed class CprSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly WoundableSystem _woundable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CprTargetComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<CprTargetComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CprTargetComponent, CprDoAfterEvent>(OnCprDoAfter);
    }

    private void TryStartCpr(Entity<CprTargetComponent> ent, EntityUid user)
    {
        _popup.PopupPredicted(
            Loc.GetString(ent.Comp.UserPopup, ("target", Identity.Entity(ent, EntityManager))),
            Loc.GetString(ent.Comp.OtherPopup, ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(ent, EntityManager))),
            ent,
            user
        );

        var args =
            new DoAfterArgs(EntityManager, user, ent.Comp.DoAfterDuration, new CprDoAfterEvent(), ent, target: ent, used: ent)
            {
                NeedHand = true,
                BreakOnDamage = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = true,
            };

        _doAfter.TryStartDoAfter(args);
    }

    private void OnCprDoAfter(Entity<CprTargetComponent> ent, ref CprDoAfterEvent args)
    {
        _statusEffects.TryAddStatusEffectDuration(ent, ent.Comp.Effect, ent.Comp.EffectDuration);

        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
        var rand = new System.Random(seed);

        if (rand.Prob(ent.Comp.WoundProbability) && TryComp<WoundableComponent>(ent, out var woundable))
        {
            if (_woundable.TryWound((ent, woundable), ent.Comp.Wound, unique: true, refreshDamage: true))
            {
                _popup.PopupClient(
                    Loc.GetString(ent.Comp.WoundPopup, ("target", Identity.Entity(ent, EntityManager))),
                    ent.Owner,
                    args.User,
                    PopupType.MediumCaution
                );
            }
        }

        args.Repeat = TryComp<HeartrateComponent>(ent, out var heartrate) && !heartrate.Running;
    }

    private void OnGetVerbs(Entity<CprTargetComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Owner == args.User)
            return;

        if (!TryComp<HeartrateComponent>(ent, out var heartrate) || heartrate.Running)
            return;

        var @event = args;
        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () =>
            {
                TryStartCpr(ent, @event.User);
            },
            Text = Loc.GetString("verb-perform-cpr"),
        });
    }

    private void OnExamined(Entity<CprTargetComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp<HeartrateComponent>(ent, out var heartrate) || heartrate.Running)
            return;

        if (_mobState.IsDead(ent))
            return;

        args.PushMarkup(Loc.GetString("cpr-target-needs-cpr", ("target", Identity.Entity(ent, EntityManager))));
    }
}
