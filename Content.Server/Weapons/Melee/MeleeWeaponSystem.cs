using System.Linq;
using System.Numerics;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.CombatMode.Disarm;
using Content.Server.Contests;
using Content.Server.Movement.Systems;
using Content.Shared.Actions.Events;
using Content.Shared.Administration.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Speech.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
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
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ContestsSystem _contests = default!;
    [Dependency] private readonly DamageExamineSystem _damageExamine = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly LagCompensationSystem _lag = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MeleeChemicalInjectorComponent, MeleeHitEvent>(OnChemicalInjectorHit);
        SubscribeLocalEvent<MeleeSpeechComponent, MeleeHitEvent>(OnSpeechHit);
        SubscribeLocalEvent<MeleeWeaponComponent, DamageExamineEvent>(OnMeleeExamineDamage);
    }

    private void OnMeleeExamineDamage(EntityUid uid, MeleeWeaponComponent component, ref DamageExamineEvent args)
    {
        if (component.Hidden)
            return;

        var damageSpec = GetDamage(uid, args.User, component);

        if (damageSpec.Total == FixedPoint2.Zero)
            return;

        _damageExamine.AddDamageExamine(args.Message, damageSpec, Loc.GetString("damage-melee"));
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

    protected override bool DoDisarm(EntityUid user, DisarmAttackEvent ev, EntityUid meleeUid, MeleeWeaponComponent component, ICommonSession? session)
    {
        if (!base.DoDisarm(user, ev, meleeUid, component, session))
            return false;

        if (!TryComp<CombatModeComponent>(user, out var combatMode) ||
            combatMode.CanDisarm != true)
        {
            return false;
        }

        var target = GetEntity(ev.Target!.Value);

        if (!TryComp<HandsComponent>(target, out var targetHandsComponent))
        {
            if (!TryComp<StatusEffectsComponent>(target, out var status) || !status.AllowedEffects.Contains("KnockedDown"))
                return false;
        }

        if (!InRange(user, target, component.Range, session))
        {
            return false;
        }

        EntityUid? inTargetHand = null;

        if (targetHandsComponent?.ActiveHand is { IsEmpty: false })
        {
            inTargetHand = targetHandsComponent.ActiveHand.HeldEntity!.Value;
        }

        Interaction.DoContactInteraction(user, target);

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
        _color.RaiseEffect(Color.Red, targets, filter);
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

        RaiseNetworkEvent(new MeleeLungeEvent(GetNetEntity(user), angle, localPos, animation), filter);
    }

    private void OnSpeechHit(EntityUid owner, MeleeSpeechComponent comp, MeleeHitEvent args)
    {
        if (!args.IsHit ||
        !args.HitEntities.Any())
        {
            return;
        }

        if (comp.Battlecry != null)//If the battlecry is set to empty, doesn't speak
        {
            _chat.TrySendInGameICMessage(args.User, comp.Battlecry, InGameICChatType.Speak, true, true, checkRadioPrefix: false);  //Speech that isn't sent to chat or adminlogs
        }

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
