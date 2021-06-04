using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.GameObjects.EntitySystems.Weapon.Melee
{
    public sealed class MeleeWeaponSystem : EntitySystem
    {
        [Dependency] private IGameTiming _gameTiming = default!;
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

        private void OnClickAttack(EntityUid uid, MeleeWeaponComponent comp, ClickAttackEvent args)
        {
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
                SoundSystem.Play(Filter.Pvs(owner), comp.HitSound, target);
            }
            else
            {
                SoundSystem.Play(Filter.Pvs(owner), comp.MissSound, args.User);
                return;
            }

            if (target.TryGetComponent(out IDamageableComponent? damageComponent))
            {
                damageComponent.ChangeDamage(comp.DamageType, comp.Damage, false, owner);
            }

            RaiseLocalEvent(uid, new MeleeHitEvent(new List<IEntity>() { target }, args.User), false);

            var targets = new[] { target };

            SendAnimation(comp.ClickArc, angle, args.User, owner, targets, comp.ClickAttackEffect, false);

            comp.LastAttackTime = curTime;
            comp.CooldownEnd = comp.LastAttackTime + TimeSpan.FromSeconds(comp.CooldownTime);

            RaiseLocalEvent(uid, new RefreshItemCooldownEvent(comp.LastAttackTime, comp.CooldownEnd), false);
        }

        private void OnWideAttack(EntityUid uid, MeleeWeaponComponent comp, WideAttackEvent args)
        {
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

            if (entities.Count != 0)
            {
                SoundSystem.Play(Filter.Pvs(owner), comp.HitSound, entities.First().Transform.Coordinates);
            }
            else
            {
                SoundSystem.Play(Filter.Pvs(owner), comp.MissSound, args.User.Transform.Coordinates);
            }

            var hitEntities = new List<IEntity>();
            foreach (var entity in entities)
            {
                if (!entity.Transform.IsMapTransform || entity == args.User)
                    continue;

                if (entity.TryGetComponent(out IDamageableComponent? damageComponent))
                {
                    damageComponent.ChangeDamage(comp.DamageType, comp.Damage, false, owner);
                    hitEntities.Add(entity);
                }
            }

            RaiseLocalEvent(uid, new MeleeHitEvent(hitEntities, args.User), false);
            SendAnimation(comp.Arc, angle, args.User, owner, hitEntities);

            comp.LastAttackTime = curTime;
            comp.CooldownEnd = comp.LastAttackTime + TimeSpan.FromSeconds(comp.ArcCooldownTime);

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
                var res = EntitySystem.Get<SharedBroadPhaseSystem>().IntersectRay(mapId,
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
            if (!ComponentManager.TryGetComponent<SolutionContainerComponent>(uid, out var solutionContainer))
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

            var removedSolution = solutionContainer.Solution.SplitSolution(comp.TransferAmount * hitBloodstreams.Count);
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

    public class MeleeHitEvent : EntityEventArgs
    {
        public IEnumerable<IEntity> HitEntities { get; }
        public IEntity User { get; }

        public MeleeHitEvent(List<IEntity> hitEntities, IEntity user)
        {
            HitEntities = hitEntities;
            User = user;
        }
    }
}
