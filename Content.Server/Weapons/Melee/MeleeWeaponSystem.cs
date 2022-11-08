using System.Linq;
using Content.Server.Actions.Events;
using Content.Server.Administration.Components;
using Content.Server.Administration.Logs;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.CombatMode;
using Content.Server.CombatMode.Disarm;
using Content.Server.Contests;
using Content.Server.Damage.Systems;
using Content.Server.Examine;
using Content.Server.Hands.Components;
using Content.Server.Movement.Systems;
using Content.Server.Weapons.Melee.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.StatusEffect;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Weapons.Melee;

public sealed class MeleeWeaponSystem : SharedMeleeWeaponSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly ContestsSystem _contests = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly LagCompensationSystem _lag = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;

    public const float DamagePitchVariation = 0.05f;

    private const int AttackMask = (int) (CollisionGroup.MobMask | CollisionGroup.Opaque);

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
                var markup = _damageable.GetDamageExamine(damageSpec, Loc.GetString("damage-melee"));
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

    protected override void DoLightAttack(EntityUid user, LightAttackEvent ev, MeleeWeaponComponent component, ICommonSession? session)
    {
        base.DoLightAttack(user, ev, component, session);

        // Can't attack yourself
        // Not in LOS.
        if (user == ev.Target ||
            ev.Target == null ||
            Deleted(ev.Target) ||
            // For consistency with wide attacks stuff needs damageable.
            !HasComp<DamageableComponent>(ev.Target) ||
            !TryComp<TransformComponent>(ev.Target, out var targetXform))
        {
            return;
        }

        if (!InRange(user, ev.Target.Value, component.Range, session))
            return;

        var damage = component.Damage * GetModifier(component, true);

        // Sawmill.Debug($"Melee damage is {damage.Total} out of {component.Damage.Total}");

        // Raise event before doing damage so we can cancel damage if the event is handled
        var hitEvent = new MeleeHitEvent(new List<EntityUid> { ev.Target.Value }, user, damage);
        RaiseLocalEvent(component.Owner, hitEvent);

        if (hitEvent.Handled)
            return;

        var targets = new List<EntityUid>(1)
        {
            ev.Target.Value
        };

        _interaction.DoContactInteraction(ev.Weapon, ev.Target);
        _interaction.DoContactInteraction(user, ev.Weapon);

        // If the user is using a long-range weapon, this probably shouldn't be happening? But I'll interpret melee as a
        // somewhat messy scuffle. See also, heavy attacks.
        _interaction.DoContactInteraction(user, ev.Target);

        // For stuff that cares about it being attacked.
        RaiseLocalEvent(ev.Target.Value, new AttackedEvent(component.Owner, user, targetXform.Coordinates));

        var modifiedDamage = DamageSpecifier.ApplyModifierSets(damage + hitEvent.BonusDamage, hitEvent.ModifiersList);
        var damageResult = _damageable.TryChangeDamage(ev.Target, modifiedDamage, origin:user);

        if (damageResult != null && damageResult.Total > FixedPoint2.Zero)
        {
            // If the target has stamina and is taking blunt damage, they should also take stamina damage based on their blunt to stamina factor
            if (damageResult.DamageDict.TryGetValue("Blunt", out var bluntDamage))
            {
                _stamina.TakeStaminaDamage(ev.Target.Value, (bluntDamage * component.BluntStaminaDamageFactor).Float());
            }

            if (component.Owner == user)
            {
                _adminLogger.Add(LogType.MeleeHit,
                    $"{ToPrettyString(user):user} melee attacked {ToPrettyString(ev.Target.Value):target} using their hands and dealt {damageResult.Total:damage} damage");
            }
            else
            {
                _adminLogger.Add(LogType.MeleeHit,
                    $"{ToPrettyString(user):user} melee attacked {ToPrettyString(ev.Target.Value):target} using {ToPrettyString(component.Owner):used} and dealt {damageResult.Total:damage} damage");
            }

            PlayHitSound(ev.Target.Value, GetHighestDamageSound(modifiedDamage, _protoManager), hitEvent.HitSoundOverride, component.HitSound);
        }
        else
        {
            if (hitEvent.HitSoundOverride != null)
            {
                Audio.PlayPvs(hitEvent.HitSoundOverride, component.Owner);
            }
            else
            {
                Audio.PlayPvs(component.NoDamageSound, component.Owner);
            }
        }

        if (damageResult?.Total > FixedPoint2.Zero)
        {
            RaiseNetworkEvent(new DamageEffectEvent(Color.Red, targets), Filter.Pvs(targetXform.Coordinates, entityMan: EntityManager));
        }
    }

    protected override void DoHeavyAttack(EntityUid user, HeavyAttackEvent ev, MeleeWeaponComponent component, ICommonSession? session)
    {
        base.DoHeavyAttack(user, ev, component, session);

        // TODO: This is copy-paste as fuck with DoPreciseAttack
        if (!TryComp<TransformComponent>(user, out var userXform))
        {
            return;
        }

        var targetMap = ev.Coordinates.ToMap(EntityManager);

        if (targetMap.MapId != userXform.MapID)
        {
            return;
        }

        var userPos = userXform.WorldPosition;
        var direction = targetMap.Position - userPos;
        var distance = Math.Min(component.Range, direction.Length);

        // This should really be improved. GetEntitiesInArc uses pos instead of bounding boxes.
        var entities = ArcRayCast(userPos, direction.ToWorldAngle(), component.Angle, distance, userXform.MapID, user);

        if (entities.Count == 0)
            return;

        var targets = new List<EntityUid>();
        var damageQuery = GetEntityQuery<DamageableComponent>();

        foreach (var entity in entities)
        {
            if (entity == user ||
                !damageQuery.HasComponent(entity))
                continue;

            targets.Add(entity);
        }

        var damage = component.Damage * GetModifier(component, false);
        // Sawmill.Debug($"Melee damage is {damage.Total} out of {component.Damage.Total}");

        // Raise event before doing damage so we can cancel damage if the event is handled
        var hitEvent = new MeleeHitEvent(targets, user, damage);
        RaiseLocalEvent(component.Owner, hitEvent);

        if (hitEvent.Handled)
            return;

        _interaction.DoContactInteraction(user, ev.Weapon);

        // For stuff that cares about it being attacked.
        foreach (var target in targets)
        {
            _interaction.DoContactInteraction(ev.Weapon, target);

            // If the user is using a long-range weapon, this probably shouldn't be happening? But I'll interpret melee as a
            // somewhat messy scuffle. See also, light attacks.
            _interaction.DoContactInteraction(user, target);

            RaiseLocalEvent(target, new AttackedEvent(component.Owner, user, Transform(target).Coordinates));
        }

        var modifiedDamage = DamageSpecifier.ApplyModifierSets(damage + hitEvent.BonusDamage, hitEvent.ModifiersList);
        var appliedDamage = new DamageSpecifier();

        foreach (var entity in targets)
        {
            RaiseLocalEvent(entity, new AttackedEvent(component.Owner, user, ev.Coordinates));

            var damageResult = _damageable.TryChangeDamage(entity, modifiedDamage, origin:user);

            if (damageResult != null && damageResult.Total > FixedPoint2.Zero)
            {
                appliedDamage += damageResult;

                if (component.Owner == user)
                {
                    _adminLogger.Add(LogType.MeleeHit,
                        $"{ToPrettyString(user):user} melee attacked {ToPrettyString(entity):target} using their hands and dealt {damageResult.Total:damage} damage");
                }
                else
                {
                    _adminLogger.Add(LogType.MeleeHit,
                        $"{ToPrettyString(user):user} melee attacked {ToPrettyString(entity):target} using {ToPrettyString(component.Owner):used} and dealt {damageResult.Total:damage} damage");
                }
            }
        }

        if (entities.Count != 0)
        {
            if (appliedDamage.Total > FixedPoint2.Zero)
            {
                var target = entities.First();
                PlayHitSound(target, GetHighestDamageSound(modifiedDamage, _protoManager), hitEvent.HitSoundOverride, component.HitSound);
            }
            else
            {
                if (hitEvent.HitSoundOverride != null)
                {
                    Audio.PlayPvs(hitEvent.HitSoundOverride, component.Owner);
                }
                else
                {
                    Audio.PlayPvs(component.NoDamageSound, component.Owner);
                }
            }
        }

        if (appliedDamage.Total > FixedPoint2.Zero)
        {
            RaiseNetworkEvent(new DamageEffectEvent(Color.Red, targets), Filter.Pvs(Transform(targets[0]).Coordinates, entityMan: EntityManager));
        }
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

        _interaction.DoContactInteraction(user, ev.Target);

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
        _adminLogger.Add(LogType.DisarmedAction, $"{ToPrettyString(user):user} used disarm on {ToPrettyString(target):target}");

        var eventArgs = new DisarmedEvent { Target = target, Source = user, PushProbability = 1 - chance };
        RaiseLocalEvent(target, eventArgs);

        RaiseNetworkEvent(new DamageEffectEvent(Color.Aqua, new List<EntityUid>() {target}));
        return true;
    }

    private bool InRange(EntityUid user, EntityUid target, float range, ICommonSession? session)
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

        return _interaction.InRangeUnobstructed(user, target, targetCoordinates, targetLocalAngle, range);
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

    private HashSet<EntityUid> ArcRayCast(Vector2 position, Angle angle, Angle arcWidth, float range, MapId mapId, EntityUid ignore)
    {
        // TODO: This is pretty sucky.
        var widthRad = arcWidth;
        var increments = 1 + 35 * (int) Math.Ceiling(widthRad / (2 * Math.PI));
        var increment = widthRad / increments;
        var baseAngle = angle - widthRad / 2;

        var resSet = new HashSet<EntityUid>();

        for (var i = 0; i < increments; i++)
        {
            var castAngle = new Angle(baseAngle + increment * i);
            var res = _physics.IntersectRay(mapId,
                new CollisionRay(position, castAngle.ToWorldVec(),
                    AttackMask), range, ignore, false).ToList();

            if (res.Count != 0)
            {
                resSet.Add(res[0].HitEntity);
            }
        }

        return resSet;
    }

    public override void DoLunge(EntityUid user, Angle angle, Vector2 localPos, string? animation)
    {
        RaiseNetworkEvent(new MeleeLungeEvent(user, angle, localPos, animation), Filter.Pvs(user, entityManager: EntityManager).RemoveWhereAttachedEntity(e => e == user));
    }

    private void PlayHitSound(EntityUid target, string? type, SoundSpecifier? hitSoundOverride, SoundSpecifier? hitSound)
    {
        var playedSound = false;

        // Play sound based off of highest damage type.
        if (TryComp<MeleeSoundComponent>(target, out var damageSoundComp))
        {
            if (type == null && damageSoundComp.NoDamageSound != null)
            {
                Audio.PlayPvs(damageSoundComp.NoDamageSound, target, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (type != null && damageSoundComp.SoundTypes?.TryGetValue(type, out var damageSoundType) == true)
            {
                Audio.PlayPvs(damageSoundType, target, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (type != null && damageSoundComp.SoundGroups?.TryGetValue(type, out var damageSoundGroup) == true)
            {
                Audio.PlayPvs(damageSoundGroup, target, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
        }

        // Use weapon sounds if the thing being hit doesn't specify its own sounds.
        if (!playedSound)
        {
            if (hitSoundOverride != null)
            {
                Audio.PlayPvs(hitSoundOverride, target, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (hitSound != null)
            {
                Audio.PlayPvs(hitSound, target, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
        }

        // Fallback to generic sounds.
        if (!playedSound)
        {
            switch (type)
            {
                // Unfortunately heat returns caustic group so can't just use the damagegroup in that instance.
                case "Burn":
                case "Heat":
                case "Cold":
                    Audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/welder.ogg"), target, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
                // No damage, fallback to tappies
                case null:
                    Audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/tap.ogg"), target, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
                case "Brute":
                    Audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/smash.ogg"), target, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
            }
        }
    }

    public static string? GetHighestDamageSound(DamageSpecifier modifiedDamage, IPrototypeManager protoManager)
    {
        var groups = modifiedDamage.GetDamagePerGroup(protoManager);

        // Use group if it's exclusive, otherwise fall back to type.
        if (groups.Count == 1)
        {
            return groups.Keys.First();
        }

        var highestDamage = FixedPoint2.Zero;
        string? highestDamageType = null;

        foreach (var (type, damage) in modifiedDamage.DamageDict)
        {
            if (damage <= highestDamage)
                continue;

            highestDamageType = type;
        }

        return highestDamageType;
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
