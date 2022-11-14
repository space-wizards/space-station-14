using System.Linq;
using Content.Server.Actions.Events;
using Content.Server.Administration.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.CombatMode;
using Content.Server.CombatMode.Disarm;
using Content.Server.Contests;
using Content.Server.Examine;
using Content.Server.Hands.Components;
using Content.Server.Movement.Systems;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.StatusEffect;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Random;

namespace Content.Server.Weapons.Melee;

public sealed class MeleeWeaponSystem : SharedMeleeWeaponSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly ContestsSystem _contests = default!;
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly LagCompensationSystem _lag = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MeleeChemicalInjectorComponent, MeleeHitEvent>(OnChemicalInjectorHit);
        SubscribeLocalEvent<MeleeWeaponComponent, GetVerbsEvent<ExamineVerb>>(OnMeleeExaminableVerb);
    }

    private void OnMeleeExaminableVerb(EntityUid uid, MeleeWeaponComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || component.HideFromExamine)
            return;

        var getDamage = new MeleeHitEvent(new List<EntityUid>(), args.User, component.Damage);
        getDamage.IsHit = false;
        RaiseLocalEvent(uid, getDamage);

        var damageSpec = GetDamage(component);

        if (damageSpec == null)
            damageSpec = new DamageSpecifier();

        damageSpec += getDamage.BonusDamage;

        if (damageSpec.Total == FixedPoint2.Zero)
            return;

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = Damageable.GetDamageExamine(damageSpec, Loc.GetString("damage-melee"));
                _examine.SendExamineTooltip(args.User, uid, markup, false, false);
            },
            Text = Loc.GetString("damage-examinable-verb-text"),
            Message = Loc.GetString("damage-examinable-verb-message"),
            Category = VerbCategory.Examine,
            IconTexture = "/Textures/Interface/VerbIcons/smite.svg.192dpi.png"
        };

        args.Verbs.Add(verb);
    }

    private DamageSpecifier? GetDamage(MeleeWeaponComponent component)
    {
        return component.Damage.Total > FixedPoint2.Zero ? component.Damage : null;
    }

    protected override void Popup(string message, EntityUid? uid, EntityUid? user)
    {
        if (uid == null)
            return;

        PopupSystem.PopupEntity(message, uid.Value, Filter.Pvs(uid.Value, entityManager: EntityManager).RemoveWhereAttachedEntity(e => e == user));
    }

    protected override bool DoDisarm(EntityUid user, DisarmAttackEvent ev, MeleeWeaponComponent component, ICommonSession? session)
    {
        if (!base.DoDisarm(user, ev, component, session))
            return false;

        if (!TryComp<CombatModeComponent>(user, out var combatMode) ||
            combatMode.CanDisarm != true)
        {
            return false;
        }

        var target = ev.Target!.Value;

        if (!TryComp<HandsComponent>(ev.Target.Value, out var targetHandsComponent))
        {
            if (!TryComp<StatusEffectsComponent>(ev.Target!.Value, out var status) || !status.AllowedEffects.Contains("KnockedDown"))
                return false;
        }

        if (!InRange(user, ev.Target.Value, component.Range, session))
        {
            return false;
        }

        EntityUid? inTargetHand = null;

        if (targetHandsComponent?.ActiveHand is { IsEmpty: false })
        {
            inTargetHand = targetHandsComponent.ActiveHand.HeldEntity!.Value;
        }

        Interaction.DoContactInteraction(user, ev.Target);

        var attemptEvent = new DisarmAttemptEvent(target, user, inTargetHand);

        if (inTargetHand != null)
        {
            RaiseLocalEvent(inTargetHand.Value, attemptEvent);
        }

        RaiseLocalEvent(target, attemptEvent);

        if (attemptEvent.Cancelled)
            return false;

        var chance = CalculateDisarmChance(user, target, inTargetHand, combatMode);

        if (_random.Prob(chance))
        {
            // Don't play a sound as the swing is already predicted.
            // Also don't play popups because most disarms will miss.
            return false;
        }

        var filterOther = Filter.Pvs(user, entityManager: EntityManager).RemoveWhereAttachedEntity(e => e == user);
        var msgPrefix = "disarm-action-";

        if (inTargetHand == null)
            msgPrefix = "disarm-action-shove-";

        var msgOther = Loc.GetString(
                msgPrefix + "popup-message-other-clients",
                ("performerName", Identity.Entity(user, EntityManager)),
                ("targetName", Identity.Entity(target, EntityManager)));

       var msgUser = Loc.GetString(msgPrefix + "popup-message-cursor", ("targetName", Identity.Entity(target, EntityManager)));

        PopupSystem.PopupEntity(msgOther, user, filterOther);
        PopupSystem.PopupEntity(msgUser, target, Filter.Entities(user));

        Audio.PlayPvs(combatMode.DisarmSuccessSound, user, AudioParams.Default.WithVariation(0.025f).WithVolume(5f));
        AdminLogger.Add(LogType.DisarmedAction, $"{ToPrettyString(user):user} used disarm on {ToPrettyString(target):target}");

        var eventArgs = new DisarmedEvent { Target = target, Source = user, PushProbability = 1 - chance };
        RaiseLocalEvent(target, eventArgs);

        RaiseNetworkEvent(new DamageEffectEvent(Color.Aqua, new List<EntityUid>() {target}));
        return true;
    }

    protected override bool InRange(EntityUid user, EntityUid target, float range, ICommonSession? session)
    {
        EntityCoordinates targetCoordinates;
        Angle targetLocalAngle;

        if (session is IPlayerSession pSession)
        {
            (targetCoordinates, targetLocalAngle) = _lag.GetCoordinatesAngle(target, pSession);
        }
        else
        {
            var xform = Transform(target);
            targetCoordinates = xform.Coordinates;
            targetLocalAngle = xform.LocalRotation;
        }

        return Interaction.InRangeUnobstructed(user, target, targetCoordinates, targetLocalAngle, range);
    }

    protected override void DoDamageEffect(List<EntityUid> targets, EntityUid? user, TransformComponent targetXform)
    {
        var filter = Filter.Pvs(targetXform.Coordinates, entityMan: EntityManager).RemoveWhereAttachedEntity(o => o == user);
        RaiseNetworkEvent(new DamageEffectEvent(Color.Red, targets), filter);
    }

    private float CalculateDisarmChance(EntityUid disarmer, EntityUid disarmed, EntityUid? inTargetHand, SharedCombatModeComponent disarmerComp)
    {
        if (HasComp<DisarmProneComponent>(disarmer))
            return 1.0f;

        if (HasComp<DisarmProneComponent>(disarmed))
            return 0.0f;

        var contestResults = 1 - _contests.OverallStrengthContest(disarmer, disarmed);

        float chance = (disarmerComp.BaseDisarmFailChance + contestResults);

        if (inTargetHand != null && TryComp<DisarmMalusComponent>(inTargetHand, out var malus))
        {
            chance += malus.Malus;
        }

        return Math.Clamp(chance, 0f, 1f);
    }

    public override void DoLunge(EntityUid user, Angle angle, Vector2 localPos, string? animation)
    {
        RaiseNetworkEvent(new MeleeLungeEvent(user, angle, localPos, animation), Filter.Pvs(user, entityManager: EntityManager).RemoveWhereAttachedEntity(e => e == user));
    }

    private void OnChemicalInjectorHit(EntityUid owner, MeleeChemicalInjectorComponent comp, MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        if (!_solutions.TryGetSolution(owner, comp.Solution, out var solutionContainer))
            return;

        var hitBloodstreams = new List<BloodstreamComponent>();
        var bloodQuery = GetEntityQuery<BloodstreamComponent>();

        foreach (var entity in args.HitEntities)
        {
            if (Deleted(entity))
                continue;

            if (bloodQuery.TryGetComponent(entity, out var bloodstream))
                hitBloodstreams.Add(bloodstream);
        }

        if (!hitBloodstreams.Any())
            return;

        var removedSolution = solutionContainer.SplitSolution(comp.TransferAmount * hitBloodstreams.Count);
        var removedVol = removedSolution.TotalVolume;
        var solutionToInject = removedSolution.SplitSolution(removedVol * comp.TransferEfficiency);
        var volPerBloodstream = solutionToInject.TotalVolume * (1 / hitBloodstreams.Count);

        foreach (var bloodstream in hitBloodstreams)
        {
            var individualInjection = solutionToInject.SplitSolution(volPerBloodstream);
            _bloodstream.TryAddToChemicals((bloodstream).Owner, individualInjection, bloodstream);
        }
    }
}
