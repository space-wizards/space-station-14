using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Cooldown;
using Content.Server.Weapon.Melee.Components;
using Content.Shared.Damage;
using Content.Shared.Sound;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Weapon.Melee
{
    public sealed class MeleeWeaponSystem : EntitySystem
    {
        [Dependency] private IGameTiming _gameTiming = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private SolutionContainerSystem _solutionsSystem = default!;
        [Dependency] private readonly AdminLogSystem _logSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MeleeWeaponComponent, HandSelectedEvent>(OnHandSelected);
            SubscribeLocalEvent<MeleeWeaponComponent, ClickAttackEvent>(OnClickAttack);
            SubscribeLocalEvent<MeleeWeaponComponent, WideAttackEvent>(OnWideAttack);
            SubscribeLocalEvent<MeleeWeaponComponent, AfterInteractEvent>(OnAfterInteract);
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

            if (curTime < comp.CooldownEnd || args.Target == null)
                return;

            var location = EntityManager.GetComponent<TransformComponent>(args.User).Coordinates;
            var diff = args.ClickLocation.ToMapPos(EntityManager) - location.ToMapPos(EntityManager);
            var angle = Angle.FromWorldVec(diff);

            if (args.Target is {Valid: true} target)
            {
                // Raise event before doing damage so we can cancel damage if the event is handled
                var hitEvent = new MeleeHitEvent(new List<EntityUid>() { target }, args.User);
                RaiseLocalEvent(owner, hitEvent, false);

                if (!hitEvent.Handled)
                {
                    var targets = new[] { target };
                    SendAnimation(comp.ClickArc, angle, args.User, owner, targets, comp.ClickAttackEffect, false);

                    RaiseLocalEvent(target, new AttackedEvent(args.Used, args.User, args.ClickLocation));

                    var modifiedDamage = DamageSpecifier.ApplyModifierSets(comp.Damage + hitEvent.BonusDamage, hitEvent.ModifiersList);
                    var damageResult = _damageableSystem.TryChangeDamage(target, modifiedDamage);

                    if (damageResult != null)
                    {
                        if (args.Used == args.User)
                            _logSystem.Add(LogType.MeleeHit,
                                $"{ToPrettyString(args.User):user} melee attacked {ToPrettyString(args.Target.Value):target} using their hands and dealt {damageResult.Total:damage} damage");
                        else
                            _logSystem.Add(LogType.MeleeHit,
                                $"{ToPrettyString(args.User):user} melee attacked {ToPrettyString(args.Target.Value):target} using {ToPrettyString(args.Used):used} and dealt {damageResult.Total:damage} damage");
                    }

                    if (hitEvent.HitSoundOverride != null)
                    {
                        SoundSystem.Play(Filter.Pvs(owner), hitEvent.HitSoundOverride.GetSound(), target, AudioHelpers.WithVariation(0.25f));
                    }
                    else
                    {
                        SoundSystem.Play(Filter.Pvs(owner), comp.HitSound.GetSound(), target);
                    }
                }
            }
            else
            {
                SoundSystem.Play(Filter.Pvs(owner), comp.MissSound.GetSound(), args.User);
                return;
            }

            comp.LastAttackTime = curTime;
            comp.CooldownEnd = comp.LastAttackTime + TimeSpan.FromSeconds(comp.CooldownTime);

            RaiseLocalEvent(owner, new RefreshItemCooldownEvent(comp.LastAttackTime, comp.CooldownEnd), false);
        }

        private void OnWideAttack(EntityUid owner, MeleeWeaponComponent comp, WideAttackEvent args)
        {
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
            var hitEvent = new MeleeHitEvent(hitEntities, args.User);
            RaiseLocalEvent(owner, hitEvent, false);
            SendAnimation(comp.Arc, angle, args.User, owner, hitEntities);

            if (!hitEvent.Handled)
            {
                if (entities.Count != 0)
                {
                    if (hitEvent.HitSoundOverride != null)
                    {
                        SoundSystem.Play(Filter.Pvs(owner), hitEvent.HitSoundOverride.GetSound(), Transform(entities.First()).Coordinates);
                    }
                    else
                    {
                        SoundSystem.Play(Filter.Pvs(owner), comp.HitSound.GetSound(), Transform(entities.First()).Coordinates);
                    }
                }
                else
                {
                    SoundSystem.Play(Filter.Pvs(owner), comp.MissSound.GetSound(), Transform(args.User).Coordinates);
                }

                var modifiedDamage = DamageSpecifier.ApplyModifierSets(comp.Damage + hitEvent.BonusDamage, hitEvent.ModifiersList);

                foreach (var entity in hitEntities)
                {
                    RaiseLocalEvent(entity, new AttackedEvent(args.Used, args.User, args.ClickLocation));

                    var damageResult = _damageableSystem.TryChangeDamage(entity, modifiedDamage);

                    if (damageResult != null)
                    {
                        if (args.Used == args.User)
                            _logSystem.Add(LogType.MeleeHit,
                                $"{ToPrettyString(args.User):user} melee attacked {ToPrettyString(entity):target} using their hands and dealt {damageResult.Total:damage} damage");
                        else
                            _logSystem.Add(LogType.MeleeHit,
                                $"{ToPrettyString(args.User):user} melee attacked {ToPrettyString(entity):target} using {ToPrettyString(args.Used):used} and dealt {damageResult.Total:damage} damage");
                    }
                }
            }

            comp.LastAttackTime = curTime;
            comp.CooldownEnd = comp.LastAttackTime + TimeSpan.FromSeconds(comp.ArcCooldownTime);

            RaiseLocalEvent(owner, new RefreshItemCooldownEvent(comp.LastAttackTime, comp.CooldownEnd), false);
        }

        /// <summary>
        ///     Used for melee weapons that want some behavior on AfterInteract,
        ///     but also want the cooldown (stun batons, flashes)
        /// </summary>
        private void OnAfterInteract(EntityUid owner, MeleeWeaponComponent comp, AfterInteractEvent args)
        {
            if (args.Handled || !args.CanReach)
                return;

            var curTime = _gameTiming.CurTime;

            if (curTime < comp.CooldownEnd)
            {
                return;
            }

            if (!args.Target.HasValue)
                return;

            var location = EntityManager.GetComponent<TransformComponent>(args.User).Coordinates;
            var diff = args.ClickLocation.ToMapPos(EntityManager) - location.ToMapPos(EntityManager);
            var angle = Angle.FromWorldVec(diff);

            var hitEvent = new MeleeInteractEvent(args.Target.Value, args.User);
            RaiseLocalEvent(owner, hitEvent, false);

            if (!hitEvent.CanInteract) return;
            SendAnimation(comp.ClickArc, angle, args.User, owner, new List<EntityUid>() { args.Target.Value }, comp.ClickAttackEffect, false);

            comp.LastAttackTime = curTime;
            comp.CooldownEnd = comp.LastAttackTime + TimeSpan.FromSeconds(comp.CooldownTime);

            RaiseLocalEvent(owner, new RefreshItemCooldownEvent(comp.LastAttackTime, comp.CooldownEnd), false);
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
                        (int) (CollisionGroup.Impassable | CollisionGroup.MobImpassable)), range, ignore).ToList();

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

        public MeleeHitEvent(List<EntityUid> hitEntities, EntityUid user)
        {
            HitEntities = hitEntities;
            User = user;
        }
    }

    /// <summary>
    ///     Raised directed on the melee weapon entity used to attack something in combat mode,
    ///     whether through a click attack or wide attack.
    /// </summary>
    public sealed class MeleeInteractEvent : EntityEventArgs
    {
        /// <summary>
        ///     The entity interacted with.
        /// </summary>
        public EntityUid Entity { get; }

        /// <summary>
        ///     The user who interacted using the melee weapon.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     Modified by the event handler to specify whether they could successfully interact with the entity.
        ///     Used to know whether to send the hit animation or not.
        /// </summary>
        public bool CanInteract { get; set; } = false;

        public MeleeInteractEvent(EntityUid entity, EntityUid user)
        {
            Entity = entity;
            User = user;
        }
    }
}
