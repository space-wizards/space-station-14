using System.Linq;
using Content.Server.Actions.Events;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.CombatMode.Disarm;
using Content.Server.Contests;
using Content.Server.Examine;
using Content.Server.Movement.Systems;
using Content.Shared.Administration.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Weapons.Melee;

public sealed class MeleeWeaponSystem : SharedMeleeWeaponSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly ContestsSystem _contests = default!;
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly LagCompensationSystem _lag = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;
    [Dependency] private readonly TagSystem _tag = default!;

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

        var getDamage = new MeleeHitEvent(new List<EntityUid>(), args.User, uid, component.Damage);
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
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/smite.svg.192dpi.png")),
        };

        args.Verbs.Add(verb);
    }

    protected override bool ArcRaySuccessful(EntityUid targetUid, Vector2 position, Angle angle, Angle arcWidth, float range, MapId mapId,
        EntityUid ignore, ICommonSession? session)
    {
        // Originally the client didn't predict damage effects so you'd intuit some level of how far
        // in the future you'd need to predict, but then there was a lot of complaining like "why would you add artifical delay" as if ping is a choice.
        // Now damage effects are predicted but for wide attacks it differs significantly from client and server so your game could be lying to you on hits.
        // This isn't fair in the slightest because it makes ping a huge advantage and this would be a hidden system.
        // Now the client tells us what they hit and we validate if it's plausible.

        // Even if the client is sending entities they shouldn't be able to hit:
        // A) Wide-damage is split anyway
        // B) We run the same validation we do for click attacks.

        // Could also check the arc though future effort + if they're aimbotting it's not really going to make a difference.

        // (This runs lagcomp internally and is what clickattacks use)
        if (!Interaction.InRangeUnobstructed(ignore, targetUid, range + 0.1f))
            return false;

        // TODO: Check arc though due to the aforementioned aimbot + damage split comments it's less important.
        return true;
    }

    private DamageSpecifier? GetDamage(MeleeWeaponComponent component)
    {
        return component.Damage.Total > FixedPoint2.Zero ? component.Damage : null;
    }

    protected override void Popup(string message, EntityUid? uid, EntityUid? user)
    {
        if (uid == null)
            return;

        if (user == null)
            PopupSystem.PopupEntity(message, uid.Value);
        else
            PopupSystem.PopupEntity(message, uid.Value, Filter.PvsExcept(user.Value, entityManager: EntityManager), true);
    }

    protected override bool DoDisarm(EntityUid user, DisarmAttackEvent ev, EntityUid meleeUid, MeleeWeaponComponent component, ICommonSession? session)
    {
        if (!base.DoDisarm(user, ev, meleeUid, component, session))
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

        var filterOther = Filter.PvsExcept(user, entityManager: EntityManager);
        var msgPrefix = "disarm-action-";

        if (inTargetHand == null)
            msgPrefix = "disarm-action-shove-";

        var msgOther = Loc.GetString(
                msgPrefix + "popup-message-other-clients",
                ("performerName", Identity.Entity(user, EntityManager)),
                ("targetName", Identity.Entity(target, EntityManager)));

       var msgUser = Loc.GetString(msgPrefix + "popup-message-cursor", ("targetName", Identity.Entity(target, EntityManager)));

        PopupSystem.PopupEntity(msgOther, user, filterOther, true);
        PopupSystem.PopupEntity(msgUser, target, user);

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

    private float CalculateDisarmChance(EntityUid disarmer, EntityUid disarmed, EntityUid? inTargetHand, CombatModeComponent disarmerComp)
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

    public override void DoLunge(EntityUid user, Angle angle, Vector2 localPos, string? animation, bool predicted = true)
    {
        Filter filter;

        if (predicted)
        {
            filter = Filter.PvsExcept(user, entityManager: EntityManager);
        }
        else
        {
            filter = Filter.Pvs(user, entityManager: EntityManager);
        }

        RaiseNetworkEvent(new MeleeLungeEvent(user, angle, localPos, animation), filter);
    }

    private void OnChemicalInjectorHit(EntityUid owner, MeleeChemicalInjectorComponent comp, MeleeHitEvent args)
    {
        if (!args.IsHit ||
            !args.HitEntities.Any() ||
            !_solutions.TryGetSolution(owner, comp.Solution, out var solutionContainer))
        {
            return;
        }

        var hitBloodstreams = new List<(EntityUid Entity, BloodstreamComponent Component)>();
        var bloodQuery = GetEntityQuery<BloodstreamComponent>();

        foreach (var entity in args.HitEntities)
        {
            if (Deleted(entity))
                continue;

            // prevent deathnettles injecting through hardsuits
            if (!comp.PierceArmor && _inventory.TryGetSlotEntity(entity, "outerClothing", out var suit) && _tag.HasTag(suit.Value, "Hardsuit"))
            {
                PopupSystem.PopupEntity(Loc.GetString("melee-inject-failed-hardsuit", ("weapon", owner)), args.User, args.User, PopupType.SmallCaution);
                continue;
            }

            if (bloodQuery.TryGetComponent(entity, out var bloodstream))
                hitBloodstreams.Add((entity, bloodstream));
        }

        if (!hitBloodstreams.Any())
            return;

        var removedSolution = solutionContainer.SplitSolution(comp.TransferAmount * hitBloodstreams.Count);
        var removedVol = removedSolution.Volume;
        var solutionToInject = removedSolution.SplitSolution(removedVol * comp.TransferEfficiency);
        var volPerBloodstream = solutionToInject.Volume * (1 / hitBloodstreams.Count);

        foreach (var (ent, bloodstream) in hitBloodstreams)
        {
            var individualInjection = solutionToInject.SplitSolution(volPerBloodstream);
            _bloodstream.TryAddToChemicals(ent, individualInjection, bloodstream);
        }
    }
}
