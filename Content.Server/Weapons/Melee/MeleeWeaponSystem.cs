using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.CombatMode.Disarm;
using Content.Server.Movement.Systems;
using Content.Shared.Actions.Events;
using Content.Shared.Administration.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Speech.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;

namespace Content.Server.Weapons.Melee;

public sealed class MeleeWeaponSystem : SharedMeleeWeaponSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageExamineSystem _damageExamine = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly LagCompensationSystem _lag = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
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

        if (damageSpec.Empty)
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

        if (_mobState.IsIncapacitated(target))
        {
            return false;
        }

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
            // Yknow something tells me this comment is hilariously out of date...
            // Don't play a sound as the swing is already predicted.
            // Also don't play popups because most disarms will miss.
            return false;
        }

        AdminLogger.Add(LogType.DisarmedAction, $"{ToPrettyString(user):user} used disarm on {ToPrettyString(target):target}");

        var eventArgs = new DisarmedEvent { Target = target, Source = user, PushProbability = 1 - chance };
        RaiseLocalEvent(target, eventArgs);

        if (!eventArgs.Handled)
        {
            return false;
        }

        Audio.PlayPvs(combatMode.DisarmSuccessSound, user, AudioParams.Default.WithVariation(0.025f).WithVolume(5f));

        var targetEnt = Identity.Entity(target, EntityManager);
        var userEnt = Identity.Entity(user, EntityManager);

        var msgOther = Loc.GetString(
                eventArgs.PopupPrefix + "popup-message-other-clients",
                ("performerName", userEnt),
                ("targetName", targetEnt));

        var msgUser = Loc.GetString(eventArgs.PopupPrefix + "popup-message-cursor", ("targetName", targetEnt));

        var filterOther = Filter.PvsExcept(user, entityManager: EntityManager);

        PopupSystem.PopupEntity(msgOther, user, filterOther, true);
        PopupSystem.PopupEntity(msgUser, target, user);


        if (eventArgs.IsStunned)
        {

            PopupSystem.PopupEntity(Loc.GetString("stunned-component-disarm-success-others", ("source", userEnt), ("target", targetEnt)), targetEnt, Filter.PvsExcept(user), true, PopupType.LargeCaution);
            PopupSystem.PopupCursor(Loc.GetString("stunned-component-disarm-success", ("target", targetEnt)), user, PopupType.Large);

            AdminLogger.Add(LogType.DisarmedKnockdown, LogImpact.Medium, $"{ToPrettyString(user):user} knocked down {ToPrettyString(target):target}");
        }

        return true;
    }

    protected override bool InRange(EntityUid user, EntityUid target, float range, ICommonSession? session)
    {
        EntityCoordinates targetCoordinates;
        Angle targetLocalAngle;

        if (session is { } pSession)
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

        var chance = disarmerComp.BaseDisarmFailChance;

        if (inTargetHand != null && TryComp<DisarmMalusComponent>(inTargetHand, out var malus))
        {
            chance += malus.Malus;
        }

        return Math.Clamp(chance, 0f, 1f);
    }

    public override void DoLunge(EntityUid user, EntityUid weapon, Angle angle, Vector2 localPos, string? animation, bool predicted = true)
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

        RaiseNetworkEvent(new MeleeLungeEvent(GetNetEntity(user), GetNetEntity(weapon), angle, localPos, animation), filter);
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

    private void OnChemicalInjectorHit(Entity<MeleeChemicalInjectorComponent> entity, ref MeleeHitEvent args)
    {
        if (!args.IsHit ||
            !args.HitEntities.Any() ||
            !_solutions.TryGetSolution(entity.Owner, entity.Comp.Solution, out var solutionContainer))
        {
            return;
        }

        var hitBloodstreams = new List<(EntityUid Entity, BloodstreamComponent Component)>();
        var bloodQuery = GetEntityQuery<BloodstreamComponent>();

        foreach (var hit in args.HitEntities)
        {
            if (Deleted(hit))
                continue;

            // prevent deathnettles injecting through hardsuits
            if (!entity.Comp.PierceArmor && _inventory.TryGetSlotEntity(hit, "outerClothing", out var suit) && _tag.HasTag(suit.Value, "Hardsuit"))
            {
                PopupSystem.PopupEntity(Loc.GetString("melee-inject-failed-hardsuit", ("weapon", entity.Owner)), args.User, args.User, PopupType.SmallCaution);
                continue;
            }

            if (bloodQuery.TryGetComponent(hit, out var bloodstream))
                hitBloodstreams.Add((hit, bloodstream));
        }

        if (!hitBloodstreams.Any())
            return;

        var removedSolution = _solutions.SplitSolution(solutionContainer.Value, entity.Comp.TransferAmount * hitBloodstreams.Count);
        var removedVol = removedSolution.Volume;
        var solutionToInject = removedSolution.SplitSolution(removedVol * entity.Comp.TransferEfficiency);
        var volPerBloodstream = solutionToInject.Volume * (1 / hitBloodstreams.Count);

        foreach (var (ent, bloodstream) in hitBloodstreams)
        {
            var individualInjection = solutionToInject.SplitSolution(volPerBloodstream);
            _bloodstream.TryAddToChemicals(ent, individualInjection, bloodstream);
        }
    }
}
