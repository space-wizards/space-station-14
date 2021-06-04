using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Items;
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

namespace Content.Server.GameObjects.EntitySystems
{
    public sealed class MeleeWeaponSystem : EntitySystem
    {
        [Dependency] private IGameTiming _gameTiming = default!;
        [Dependency] private IEntityManager _entityManager = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MeleeWeaponComponent, HandSelectedEvent>(OnHandSelected);
            SubscribeLocalEvent<MeleeWeaponComponent, NormalAttackEvent>(OnNormalAttack);
            SubscribeLocalEvent<MeleeWeaponComponent, WideAttackEvent>(OnWideAttack);
            SubscribeLocalEvent<ItemCooldownComponent, RefreshItemCooldownEvent>(OnCooldownRefreshed);
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

            RaiseLocalEvent(uid, new RefreshItemCooldownEvent(comp.LastAttackTime, comp.CooldownEnd));
        }

        private void OnNormalAttack(EntityUid uid, MeleeWeaponComponent comp, NormalAttackEvent args)
        {
            var curTime = _gameTiming.CurTime;
            if (!_entityManager.TryGetEntity(uid, out var owner))
            {
                args.Succeeded = false;
                return;
            }

            if (curTime < comp.CooldownEnd || !args.Target.IsValid())
            {
                args.Succeeded = false;
                return;
            }

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
                args.Succeeded = false;
                return;
            }

            if (target.TryGetComponent(out IDamageableComponent? damageComponent))
            {
                damageComponent.ChangeDamage(comp.DamageType, comp.Damage, false, owner);
            }

            RaiseLocalEvent(uid, new MeleeHitEvent(new List<IEntity>() { target }), false);

            var targets = new[] { target };

            SendAnimation(comp.ClickArc, angle, args.User, owner, targets, comp.ClickAttackEffect, false);

            comp.LastAttackTime = curTime;
            comp.CooldownEnd = comp.LastAttackTime + TimeSpan.FromSeconds(comp.CooldownTime);

            RaiseLocalEvent(uid, new RefreshItemCooldownEvent(comp.LastAttackTime, comp.CooldownEnd), false);
            args.Succeeded = true;
            return;
        }

        private void OnWideAttack(EntityUid uid, MeleeWeaponComponent comp, WideAttackEvent args)
        {
            var curTime = _gameTiming.CurTime;

            if (curTime < comp.CooldownEnd)
            {
                args.Succeeded = false;
                return;
            }

            if (!_entityManager.TryGetEntity(uid, out var owner))
            {
                args.Succeeded = false;
                return;
            }

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

            RaiseLocalEvent(uid, new MeleeHitEvent(hitEntities), false);

            SendAnimation(comp.Arc, angle, args.User, owner, hitEntities);

            comp.LastAttackTime = curTime;
            comp.CooldownEnd = comp.LastAttackTime + TimeSpan.FromSeconds(comp.ArcCooldownTime);

            RaiseLocalEvent(uid, new RefreshItemCooldownEvent(comp.LastAttackTime, comp.CooldownEnd), false);
            args.Succeeded = true;
            return;
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

        private void OnCooldownRefreshed(EntityUid uid, ItemCooldownComponent comp, RefreshItemCooldownEvent args)
        {
            comp.CooldownStart = args.LastAttackTime;
            comp.CooldownEnd = args.CooldownEnd;
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
        public readonly List<IEntity> HitEntities;

        public MeleeHitEvent(List<IEntity> hitEntities)
        {
            HitEntities = hitEntities;
        }
    }

    public class RefreshItemCooldownEvent : EntityEventArgs
    {
        public TimeSpan LastAttackTime;
        public TimeSpan CooldownEnd;

        public RefreshItemCooldownEvent(TimeSpan lastAttackTime, TimeSpan cooldownEnd)
        {
            LastAttackTime = lastAttackTime;
            CooldownEnd = cooldownEnd;
        }
    }
}
