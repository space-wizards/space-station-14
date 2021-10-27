using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Body.Circulatory;
using Content.Server.Chemistry.Components;
using Content.Server.Cooldown;
using Content.Server.Weapon.Melee.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
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

        private void OnClickAttack(EntityUid uid, MeleeWeaponComponent comp, ClickAttackEvent args)
        {
            args.Handled = true;
            var curTime = _gameTiming.CurTime;

            if (curTime < comp.CooldownEnd || !args.Target.IsValid())
                return;

            var owner = EntityManager.GetEntity(uid);
            var target = args.TargetEntity;

            var location = args.User.Transform.Coordinates;
            var diff = args.ClickLocation.ToMapPos(owner.EntityManager) - location.ToMapPos(owner.EntityManager);
            var angle = Angle.FromWorldVec(diff);

            if (target != null)
            {
                // Raise event before doing damage so we can cancel damage if the event is handled
                var hitEvent = new MeleeHitEvent(new List<IEntity>() { target }, args.User);
                RaiseLocalEvent(uid, hitEvent, false);

                if (!hitEvent.Handled)
                {
                    var targets = new[] { target };
                    SendAnimation(comp.ClickArc, angle, args.User, owner, targets, comp.ClickAttackEffect, false);

                    RaiseLocalEvent(target.Uid, new AttackedEvent(args.Used, args.User, args.ClickLocation));

                    _damageableSystem.TryChangeDamage(target.Uid,
                        DamageSpecifier.ApplyModifierSets(comp.Damage, hitEvent.ModifiersList));
                    SoundSystem.Play(Filter.Pvs(owner), comp.HitSound.GetSound(), target);
                }
            }
            else
            {
                SoundSystem.Play(Filter.Pvs(owner), comp.MissSound.GetSound(), args.User);
                return;
            }

            comp.LastAttackTime = curTime;
            comp.CooldownEnd = comp.LastAttackTime + TimeSpan.FromSeconds(comp.CooldownTime);

            RaiseLocalEvent(uid, new RefreshItemCooldownEvent(comp.LastAttackTime, comp.CooldownEnd), false);
        }

        private void OnWideAttack(EntityUid uid, MeleeWeaponComponent comp, WideAttackEvent args)
        {
            args.Handled = true;
            var curTime = _gameTiming.CurTime;

            if (curTime < comp.CooldownEnd)
            {
                return;
            }

            var owner = EntityManager.GetEntity(uid);

            var location = args.User.Transform.Coordinates;
            var diff = args.ClickLocation.ToMapPos(owner.EntityManager) - location.ToMapPos(owner.EntityManager);
            var angle = Angle.FromWorldVec(diff);

            // This should really be improved. GetEntitiesInArc uses pos instead of bounding boxes.
            var entities = ArcRayCast(args.User.Transform.WorldPosition, angle, comp.ArcWidth, comp.Range, owner.Transform.MapID, args.User);

            var hitEntities = new List<IEntity>();
            foreach (var entity in entities)
            {
                if (entity.IsInContainer() || entity == args.User)
                    continue;

                if (EntityManager.HasComponent<DamageableComponent>(entity.Uid))
                {
                    hitEntities.Add(entity);
                }
            }

            // Raise event before doing damage so we can cancel damage if handled
            var hitEvent = new MeleeHitEvent(hitEntities, args.User);
            RaiseLocalEvent(uid, hitEvent, false);
            SendAnimation(comp.Arc, angle, args.User, owner, hitEntities);

            if (!hitEvent.Handled)
            {
                if (entities.Count != 0)
                {
                    SoundSystem.Play(Filter.Pvs(owner), comp.HitSound.GetSound(), entities.First().Transform.Coordinates);
                }
                else
                {
                    SoundSystem.Play(Filter.Pvs(owner), comp.MissSound.GetSound(), args.User.Transform.Coordinates);
                }

                foreach (var entity in hitEntities)
                {
                    RaiseLocalEvent(entity.Uid, new AttackedEvent(args.Used, args.User, args.ClickLocation));

                    _damageableSystem.TryChangeDamage(entity.Uid,
                            DamageSpecifier.ApplyModifierSets(comp.Damage, hitEvent.ModifiersList));
                }
            }

            comp.LastAttackTime = curTime;
            comp.CooldownEnd = comp.LastAttackTime + TimeSpan.FromSeconds(comp.ArcCooldownTime);

            RaiseLocalEvent(uid, new RefreshItemCooldownEvent(comp.LastAttackTime, comp.CooldownEnd), false);
        }

        /// <summary>
        ///     Used for melee weapons that want some behavior on AfterInteract,
        ///     but also want the cooldown (stun batons, flashes)
        /// </summary>
        private void OnAfterInteract(EntityUid uid, MeleeWeaponComponent comp, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            var curTime = _gameTiming.CurTime;

            if (curTime < comp.CooldownEnd)
            {
                return;
            }

            var owner = EntityManager.GetEntity(uid);

            if (args.Target == null)
                return;

            var location = args.User.Transform.Coordinates;
            var diff = args.ClickLocation.ToMapPos(owner.EntityManager) - location.ToMapPos(owner.EntityManager);
            var angle = Angle.FromWorldVec(diff);

            var hitEvent = new MeleeInteractEvent(args.Target, args.User);
            RaiseLocalEvent(uid, hitEvent, false);

            if (!hitEvent.CanInteract) return;
            SendAnimation(comp.ClickArc, angle, args.User, owner, new List<IEntity>() { args.Target }, comp.ClickAttackEffect, false);

            comp.LastAttackTime = curTime;
            comp.CooldownEnd = comp.LastAttackTime + TimeSpan.FromSeconds(comp.CooldownTime);

            RaiseLocalEvent(uid, new RefreshItemCooldownEvent(comp.LastAttackTime, comp.CooldownEnd), false);
        }

        private HashSet<IEntity> ArcRayCast(Vector2 position, Angle angle, float arcWidth, float range, MapId mapId, IEntity ignore)
        {
            var widthRad = Angle.FromDegrees(arcWidth);
            var increments = 1 + 35 * (int) Math.Ceiling(widthRad / (2 * Math.PI));
            var increment = widthRad / increments;
            var baseAngle = angle - widthRad / 2;

            var resSet = new HashSet<IEntity>();

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

        private void OnChemicalInjectorHit(EntityUid uid, MeleeChemicalInjectorComponent comp, MeleeHitEvent args)
        {
            IEntity owner = EntityManager.GetEntity(uid);
            if (!_solutionsSystem.TryGetInjectableSolution(owner.Uid, out var solutionContainer))
                return;

            var hitBloodstreams = new List<BloodstreamComponent>();
            foreach (var entity in args.HitEntities)
            {
                if (entity.Deleted)
                    continue;

                if (entity.TryGetComponent<BloodstreamComponent>(out var bloodstream))
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
                bloodstream.TryTransferSolution(individualInjection);
            }
        }

        public void SendAnimation(string arc, Angle angle, IEntity attacker, IEntity source, IEnumerable<IEntity> hits, bool textureEffect = false, bool arcFollowAttacker = true)
        {
            RaiseNetworkEvent(new MeleeWeaponSystemMessages.PlayMeleeWeaponAnimationMessage(arc, angle, attacker.Uid, source.Uid,
                hits.Select(e => e.Uid).ToList(), textureEffect, arcFollowAttacker), Filter.Pvs(source, 1f));
        }

        public void SendLunge(Angle angle, IEntity source)
        {
            RaiseNetworkEvent(new MeleeWeaponSystemMessages.PlayLungeAnimationMessage(angle, source.Uid), Filter.Pvs(source, 1f));
        }
    }

    /// <summary>
    ///     Raised directed on the melee weapon entity used to attack something in combat mode,
    ///     whether through a click attack or wide attack.
    /// </summary>
    public class MeleeHitEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Modifier sets to apply to the hit event when it's all said and done.
        ///     This should be modified by adding a new entry to the list.
        /// </summary>
        public List<DamageModifierSet> ModifiersList = new();

        /// <summary>
        ///     A flat amount of damage to add. Same reason as above with Multiplier.
        /// </summary>
        public int FlatDamage = 0;

        /// <summary>
        ///     A list containing every hit entity. Can be zero.
        /// </summary>
        public IEnumerable<IEntity> HitEntities { get; }

        /// <summary>
        /// The user who attacked with the melee wepaon.
        /// </summary>
        public IEntity User { get; }

        public MeleeHitEvent(List<IEntity> hitEntities, IEntity user)
        {
            HitEntities = hitEntities;
            User = user;
        }
    }

    /// <summary>
    ///     Raised directed on the melee weapon entity used to attack something in combat mode,
    ///     whether through a click attack or wide attack.
    /// </summary>
    public class MeleeInteractEvent : EntityEventArgs
    {
        /// <summary>
        ///     The entity interacted with.
        /// </summary>
        public IEntity Entity { get; }

        /// <summary>
        ///     The user who interacted using the melee weapon.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Modified by the event handler to specify whether they could successfully interact with the entity.
        ///     Used to know whether to send the hit animation or not.
        /// </summary>
        public bool CanInteract { get; set; } = false;

        public MeleeInteractEvent(IEntity entity, IEntity user)
        {
            Entity = entity;
            User = user;
        }
    }
}
