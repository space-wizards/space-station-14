using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Cooldown;
using Content.Server.Damage.Components;
using Content.Server.Damage.Systems;
using Content.Server.Weapon.Melee.Components;
using Content.Shared.Damage;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Physics;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Weapon.Melee
{
    public sealed class MeleeWeaponSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly StaminaSystem _staminaSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;

        public const float DamagePitchVariation = 0.15f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MeleeWeaponComponent, HandSelectedEvent>(OnHandSelected);
            SubscribeLocalEvent<MeleeWeaponComponent, ClickAttackEvent>(OnClickAttack);
            SubscribeLocalEvent<MeleeWeaponComponent, WideAttackEvent>(OnWideAttack);
            SubscribeLocalEvent<MeleeChemicalInjectorComponent, MeleeHitEvent>(OnChemicalInjectorHit);
        }

        private void OnHandSelected(EntityUid uid, MeleeWeaponComponent comp, HandSelectedEvent args)
        {
            var curTime = _gameTiming.CurTime;
            var cool = TimeSpan.FromSeconds(comp.CooldownTime * 0.5f);

            if (curTime < comp.CooldownEnd)
            {
                if (comp.CooldownEnd - curTime < cool)
                {
                    comp.LastAttackTime = curTime;
                    comp.CooldownEnd += cool;
                }
                else
                    return;
            }
            else
            {
                comp.LastAttackTime = curTime;
                comp.CooldownEnd = curTime + cool;
            }

            RaiseLocalEvent(uid, new RefreshItemCooldownEvent(comp.LastAttackTime, comp.CooldownEnd), false);
        }

        private void OnClickAttack(EntityUid owner, MeleeWeaponComponent comp, ClickAttackEvent args)
        {
            args.Handled = true;
            var curTime = _gameTiming.CurTime;

            if (curTime < comp.CooldownEnd ||
                args.Target == null ||
                args.Target == owner ||
                args.User == args.Target)
                return;

            var location = Transform(args.User).Coordinates;
            var diff = args.ClickLocation.ToMapPos(EntityManager) - location.ToMapPos(EntityManager);
            var angle = Angle.FromWorldVec(diff);

            if (args.Target is {Valid: true} target)
            {
                // Raise event before doing damage so we can cancel damage if the event is handled
                var hitEvent = new MeleeHitEvent(new List<EntityUid>() { target }, args.User, comp.Damage);
                RaiseLocalEvent(owner, hitEvent, false);

                if (!hitEvent.Handled)
                {
                    var targets = new[] { target };
                    SendAnimation(comp.ClickArc, angle, args.User, owner, targets, comp.ClickAttackEffect, false);

                    RaiseLocalEvent(target, new AttackedEvent(args.Used, args.User, args.ClickLocation), true);

                    var modifiedDamage = DamageSpecifier.ApplyModifierSets(comp.Damage + hitEvent.BonusDamage, hitEvent.ModifiersList);
                    var damageResult = _damageableSystem.TryChangeDamage(target, modifiedDamage);

                    if (damageResult != null && damageResult.Total > FixedPoint2.Zero)
                    {
                        FixedPoint2 bluntDamage;
                        // If the target has stamina and is taking blunt damage, they should also take stamina damage based on their blunt to stamina factor
                        if (damageResult.DamageDict.TryGetValue("Blunt", out bluntDamage))
                        {
                            _staminaSystem.TakeStaminaDamage(target, (bluntDamage * comp.BluntStaminaDamageFactor).Float());
                        }

                        if (args.Used == args.User)
                            _adminLogger.Add(LogType.MeleeHit,
                                $"{ToPrettyString(args.User):user} melee attacked {ToPrettyString(args.Target.Value):target} using their hands and dealt {damageResult.Total:damage} damage");
                        else
                            _adminLogger.Add(LogType.MeleeHit,
                                $"{ToPrettyString(args.User):user} melee attacked {ToPrettyString(args.Target.Value):target} using {ToPrettyString(args.Used):used} and dealt {damageResult.Total:damage} damage");

                        PlayHitSound(target, GetHighestDamageSound(modifiedDamage, _protoManager), hitEvent.HitSoundOverride, comp.HitSound);
                    }
                    else
                    {
                        SoundSystem.Play((hitEvent.HitSoundOverride != null)
                            ? hitEvent.HitSoundOverride.GetSound()
                            : comp.NoDamageSound.GetSound(), Filter.Pvs(owner, entityManager: EntityManager), owner);
                    }
                }
            }
            else
            {
                SoundSystem.Play(comp.MissSound.GetSound(), Filter.Pvs(owner, entityManager: EntityManager), owner);
            }

            comp.LastAttackTime = curTime;
            SetAttackCooldown(owner, comp.LastAttackTime + TimeSpan.FromSeconds(comp.CooldownTime), comp);

            RaiseLocalEvent(owner, new RefreshItemCooldownEvent(comp.LastAttackTime, comp.CooldownEnd));
        }

        /// <summary>
        /// Set the melee weapon cooldown's end to the specified value. Will use the maximum of the existing cooldown or the new one.
        /// </summary>
        public void SetAttackCooldown(EntityUid uid, TimeSpan endTime, MeleeWeaponComponent? component = null)
        {
            // Some other system may want to artificially inflate melee weapon CD.
            if (!Resolve(uid, ref component) || component.CooldownEnd > endTime) return;

            component.CooldownEnd = endTime;
            RaiseLocalEvent(uid, new RefreshItemCooldownEvent(component.LastAttackTime, component.CooldownEnd));
        }

        private void OnWideAttack(EntityUid owner, MeleeWeaponComponent comp, WideAttackEvent args)
        {
            if (string.IsNullOrEmpty(comp.Arc)) return;

            args.Handled = true;
            var curTime = _gameTiming.CurTime;

            if (curTime < comp.CooldownEnd)
            {
                return;
            }

            var location = EntityManager.GetComponent<TransformComponent>(args.User).Coordinates;
            var diff = args.ClickLocation.ToMapPos(EntityManager) - location.ToMapPos(EntityManager);
            var angle = Angle.FromWorldVec(diff);

            // This should really be improved. GetEntitiesInArc uses pos instead of bounding boxes.
            var entities = ArcRayCast(EntityManager.GetComponent<TransformComponent>(args.User).WorldPosition, angle, comp.ArcWidth, comp.Range, EntityManager.GetComponent<TransformComponent>(owner).MapID, args.User);

            var hitEntities = new List<EntityUid>();
            foreach (var entity in entities)
            {
                if (entity.IsInContainer() || entity == args.User)
                    continue;

                if (EntityManager.HasComponent<DamageableComponent>(entity))
                {
                    hitEntities.Add(entity);
                }
            }

            // Raise event before doing damage so we can cancel damage if handled
            var hitEvent = new MeleeHitEvent(hitEntities, args.User, comp.Damage);
            RaiseLocalEvent(owner, hitEvent, false);
            SendAnimation(comp.Arc, angle, args.User, owner, hitEntities);

            if (!hitEvent.Handled)
            {
                var modifiedDamage = DamageSpecifier.ApplyModifierSets(comp.Damage + hitEvent.BonusDamage, hitEvent.ModifiersList);
                var appliedDamage = new DamageSpecifier();

                foreach (var entity in hitEntities)
                {
                    RaiseLocalEvent(entity, new AttackedEvent(args.Used, args.User, args.ClickLocation), true);

                    var damageResult = _damageableSystem.TryChangeDamage(entity, modifiedDamage);

                    if (damageResult != null && damageResult.Total > FixedPoint2.Zero)
                    {
                        appliedDamage += damageResult;

                        if (args.Used == args.User)
                            _adminLogger.Add(LogType.MeleeHit,
                                $"{ToPrettyString(args.User):user} melee attacked {ToPrettyString(entity):target} using their hands and dealt {damageResult.Total:damage} damage");
                        else
                            _adminLogger.Add(LogType.MeleeHit,
                                $"{ToPrettyString(args.User):user} melee attacked {ToPrettyString(entity):target} using {ToPrettyString(args.Used):used} and dealt {damageResult.Total:damage} damage");
                    }
                }

                if (entities.Count != 0)
                {
                    if (appliedDamage.Total > FixedPoint2.Zero)
                    {
                        var target = entities.First();
                        PlayHitSound(target, GetHighestDamageSound(modifiedDamage, _protoManager), hitEvent.HitSoundOverride, comp.HitSound);
                    }
                    else
                    {
                        SoundSystem.Play((hitEvent.HitSoundOverride != null)
                            ? hitEvent.HitSoundOverride.GetSound()
                            : comp.NoDamageSound.GetSound(), Filter.Pvs(owner, entityManager: EntityManager), owner);
                    }
                }
                else
                {
                    SoundSystem.Play(comp.MissSound.GetSound(), Filter.Pvs(owner, entityManager: EntityManager), owner);
                }
            }

            comp.LastAttackTime = curTime;
            comp.CooldownEnd = comp.LastAttackTime + TimeSpan.FromSeconds(comp.ArcCooldownTime);
            RaiseLocalEvent(owner, new RefreshItemCooldownEvent(comp.LastAttackTime, comp.CooldownEnd));
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
                if (damage <= highestDamage) continue;
                highestDamageType = type;
            }

            return highestDamageType;
        }

        private void PlayHitSound(EntityUid target, string? type, SoundSpecifier? hitSoundOverride, SoundSpecifier? hitSound)
        {
            var playedSound = false;

            // Play sound based off of highest damage type.
            if (TryComp<MeleeSoundComponent>(target, out var damageSoundComp))
            {
                if (type == null && damageSoundComp.NoDamageSound != null)
                {
                    SoundSystem.Play(damageSoundComp.NoDamageSound.GetSound(), Filter.Pvs(target, entityManager: EntityManager), target, AudioHelpers.WithVariation(DamagePitchVariation));
                    playedSound = true;
                }
                else if (type != null && damageSoundComp.SoundTypes?.TryGetValue(type, out var damageSoundType) == true)
                {
                    SoundSystem.Play(damageSoundType.GetSound(), Filter.Pvs(target, entityManager: EntityManager), target, AudioHelpers.WithVariation(DamagePitchVariation));
                    playedSound = true;
                }
                else if (type != null && damageSoundComp.SoundGroups?.TryGetValue(type, out var damageSoundGroup) == true)
                {
                    SoundSystem.Play(damageSoundGroup.GetSound(), Filter.Pvs(target, entityManager: EntityManager), target, AudioHelpers.WithVariation(DamagePitchVariation));
                    playedSound = true;
                }
            }

            // Use weapon sounds if the thing being hit doesn't specify its own sounds.
            if (!playedSound)
            {
                if (hitSoundOverride != null)
                {
                    SoundSystem.Play(hitSoundOverride.GetSound(), Filter.Pvs(target, entityManager: EntityManager), target, AudioHelpers.WithVariation(DamagePitchVariation));
                    playedSound = true;
                }
                else if (hitSound != null)
                {
                    SoundSystem.Play(hitSound.GetSound(), Filter.Pvs(target, entityManager: EntityManager), target);
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
                        SoundSystem.Play("/Audio/Items/welder.ogg", Filter.Pvs(target, entityManager: EntityManager), target);
                        break;
                    // No damage, fallback to tappies
                    case null:
                        SoundSystem.Play("/Audio/Weapons/tap.ogg", Filter.Pvs(target, entityManager: EntityManager), target);
                        break;
                    case "Brute":
                        SoundSystem.Play("/Audio/Weapons/smash.ogg", Filter.Pvs(target, entityManager: EntityManager), target);
                        break;
                }
            }
        }

        private HashSet<EntityUid> ArcRayCast(Vector2 position, Angle angle, float arcWidth, float range, MapId mapId, EntityUid ignore)
        {
            var widthRad = Angle.FromDegrees(arcWidth);
            var increments = 1 + 35 * (int) Math.Ceiling(widthRad / (2 * Math.PI));
            var increment = widthRad / increments;
            var baseAngle = angle - widthRad / 2;

            var resSet = new HashSet<EntityUid>();

            for (var i = 0; i < increments; i++)
            {
                var castAngle = new Angle(baseAngle + increment * i);
                var res = Get<SharedPhysicsSystem>().IntersectRay(mapId,
                    new CollisionRay(position, castAngle.ToWorldVec(),
                        (int) (CollisionGroup.MobMask | CollisionGroup.Opaque)), range, ignore).ToList();

                if (res.Count != 0)
                {
                    resSet.Add(res[0].HitEntity);
                }
            }

            return resSet;
        }

        private void OnChemicalInjectorHit(EntityUid owner, MeleeChemicalInjectorComponent comp, MeleeHitEvent args)
        {
            if (!_solutionsSystem.TryGetInjectableSolution(owner, out var solutionContainer))
                return;

            var hitBloodstreams = new List<BloodstreamComponent>();
            foreach (var entity in args.HitEntities)
            {
                if (Deleted(entity))
                    continue;

                if (EntityManager.TryGetComponent<BloodstreamComponent?>(entity, out var bloodstream))
                    hitBloodstreams.Add(bloodstream);
            }

            if (hitBloodstreams.Count < 1)
                return;

            var removedSolution = solutionContainer.SplitSolution(comp.TransferAmount * hitBloodstreams.Count);
            var removedVol = removedSolution.TotalVolume;
            var solutionToInject = removedSolution.SplitSolution(removedVol * comp.TransferEfficiency);
            var volPerBloodstream = solutionToInject.TotalVolume * (1 / hitBloodstreams.Count);

            foreach (var bloodstream in hitBloodstreams)
            {
                var individualInjection = solutionToInject.SplitSolution(volPerBloodstream);
                _bloodstreamSystem.TryAddToChemicals((bloodstream).Owner, individualInjection, bloodstream);
            }
        }

        public void SendAnimation(string arc, Angle angle, EntityUid attacker, EntityUid source, IEnumerable<EntityUid> hits, bool textureEffect = false, bool arcFollowAttacker = true)
        {
            RaiseNetworkEvent(new MeleeWeaponSystemMessages.PlayMeleeWeaponAnimationMessage(arc, angle, attacker, source,
                hits.Select(e => e).ToList(), textureEffect, arcFollowAttacker), Filter.Pvs(source, 1f));
        }

        public void SendLunge(Angle angle, EntityUid source)
        {
            RaiseNetworkEvent(new MeleeWeaponSystemMessages.PlayLungeAnimationMessage(angle, source), Filter.Pvs(source, 1f));
        }
    }

    /// <summary>
    ///     Raised directed on the melee weapon entity used to attack something in combat mode,
    ///     whether through a click attack or wide attack.
    /// </summary>
    public sealed class MeleeHitEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     The base amount of damage dealt by the melee hit.
        /// </summary>
        public readonly DamageSpecifier BaseDamage = new();

        /// <summary>
        ///     Modifier sets to apply to the hit event when it's all said and done.
        ///     This should be modified by adding a new entry to the list.
        /// </summary>
        public List<DamageModifierSet> ModifiersList = new();

        /// <summary>
        ///     Damage to add to the default melee weapon damage. Applied before modifiers.
        /// </summary>
        /// <remarks>
        ///     This might be required as damage modifier sets cannot add a new damage type to a DamageSpecifier.
        /// </remarks>
        public DamageSpecifier BonusDamage = new();

        /// <summary>
        ///     A list containing every hit entity. Can be zero.
        /// </summary>
        public IEnumerable<EntityUid> HitEntities { get; }

        /// <summary>
        ///     Used to define a new hit sound in case you want to override the default GenericHit.
        ///     Also gets a pitch modifier added to it.
        /// </summary>
        public SoundSpecifier? HitSoundOverride {get; set;}

        /// <summary>
        /// The user who attacked with the melee weapon.
        /// </summary>
        public EntityUid User { get; }

        public MeleeHitEvent(List<EntityUid> hitEntities, EntityUid user, DamageSpecifier baseDamage)
        {
            HitEntities = hitEntities;
            User = user;
            BaseDamage = baseDamage;
        }
    }
}
