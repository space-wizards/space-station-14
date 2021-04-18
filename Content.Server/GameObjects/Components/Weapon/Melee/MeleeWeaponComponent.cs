using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class MeleeWeaponComponent : Component, IAttack, IHandSelected
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override string Name => "MeleeWeapon";
        private TimeSpan _lastAttackTime;
        private TimeSpan _cooldownEnd;

        [DataField("hitSound")]
        private string _hitSound = "/Audio/Weapons/genhit1.ogg";

        [DataField("missSound")]
        private string _missSound = "/Audio/Weapons/punchmiss.ogg";

        [DataField("arcCooldownTime")]
        public float ArcCooldownTime { get; private set; } = 1f;

        [DataField("cooldownTime")]
        public float CooldownTime { get; private set; } = 1f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("clickArc")]
        public string ClickArc { get; set; } = "punch";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("arc")]
        public string Arc { get; set; } = "default";

        [ViewVariables(VVAccess.ReadWrite)] [DataField("arcwidth")] public float ArcWidth { get; set; } = 90;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("range")]
        public float Range { get; set; } = 1;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("damage")]
        public int Damage { get; set; } = 5;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("damageType")]
        public DamageType DamageType { get; set; } = DamageType.Blunt;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("clickAttackEffect")] public bool ClickAttackEffect { get; set; } = true;

        protected virtual bool OnHitEntities(IReadOnlyList<IEntity> entities, AttackEventArgs eventArgs)
        {
            return true;
        }

        bool IAttack.WideAttack(AttackEventArgs eventArgs)
        {
            if (!eventArgs.WideAttack) return true;

            var curTime = _gameTiming.CurTime;

            if (curTime < _cooldownEnd)
                return true;

            var location = eventArgs.User.Transform.Coordinates;
            var diff = eventArgs.ClickLocation.ToMapPos(Owner.EntityManager) - location.ToMapPos(Owner.EntityManager);
            var angle = Angle.FromWorldVec(diff);

            // This should really be improved. GetEntitiesInArc uses pos instead of bounding boxes.
            var entities = ArcRayCast(eventArgs.User.Transform.WorldPosition, angle, eventArgs.User);

            if (entities.Count != 0)
            {
                SoundSystem.Play(Filter.Pvs(Owner), _hitSound, entities.First());
            }
            else
            {
                SoundSystem.Play(Filter.Pvs(Owner), _missSound, eventArgs.User);
            }

            var hitEntities = new List<IEntity>();
            foreach (var entity in entities)
            {
                if (!entity.Transform.IsMapTransform || entity == eventArgs.User)
                    continue;

                if (entity.TryGetComponent(out IDamageableComponent? damageComponent))
                {
                    damageComponent.ChangeDamage(DamageType, Damage, false, Owner);
                    hitEntities.Add(entity);
                }
            }
            SendMessage(new MeleeHitMessage(hitEntities));

            if (!OnHitEntities(hitEntities, eventArgs)) return false;

            if (Arc != null)
            {
                var sys = EntitySystem.Get<MeleeWeaponSystem>();
                sys.SendAnimation(Arc, angle, eventArgs.User, Owner, hitEntities);
            }

            _lastAttackTime = curTime;
            _cooldownEnd = _lastAttackTime + TimeSpan.FromSeconds(ArcCooldownTime);

            RefreshItemCooldown();

            return true;
        }

        bool IAttack.ClickAttack(AttackEventArgs eventArgs)
        {
            if (eventArgs.WideAttack) return false;

            var curTime = _gameTiming.CurTime;

            if (curTime < _cooldownEnd || !eventArgs.Target.IsValid())
                return true;

            var target = eventArgs.TargetEntity;

            var location = eventArgs.User.Transform.Coordinates;
            var diff = eventArgs.ClickLocation.ToMapPos(Owner.EntityManager) - location.ToMapPos(Owner.EntityManager);
            var angle = Angle.FromWorldVec(diff);

            if (target != null)
            {
                SoundSystem.Play(Filter.Pvs(Owner), _hitSound, target);
            }
            else
            {
                SoundSystem.Play(Filter.Pvs(Owner), _missSound, eventArgs.User);
                return false;
            }

            if (target.TryGetComponent(out IDamageableComponent? damageComponent))
            {
                damageComponent.ChangeDamage(DamageType, Damage, false, Owner);
            }
            SendMessage(new MeleeHitMessage(new List<IEntity> { target }));

            var targets = new[] { target };

            if (!OnHitEntities(targets, eventArgs))
                return false;

            if (ClickArc != null)
            {
                var sys = EntitySystem.Get<MeleeWeaponSystem>();
                sys.SendAnimation(ClickArc, angle, eventArgs.User, Owner, targets, ClickAttackEffect, false);
            }

            _lastAttackTime = curTime;
            _cooldownEnd = _lastAttackTime + TimeSpan.FromSeconds(CooldownTime);

            RefreshItemCooldown();

            return true;
        }

        private HashSet<IEntity> ArcRayCast(Vector2 position, Angle angle, IEntity ignore)
        {
            var widthRad = Angle.FromDegrees(ArcWidth);
            var increments = 1 + 35 * (int) Math.Ceiling(widthRad / (2 * Math.PI));
            var increment = widthRad / increments;
            var baseAngle = angle - widthRad / 2;

            var resSet = new HashSet<IEntity>();

            var mapId = Owner.Transform.MapID;
            for (var i = 0; i < increments; i++)
            {
                var castAngle = new Angle(baseAngle + increment * i);
                var res = EntitySystem.Get<SharedBroadPhaseSystem>().IntersectRay(mapId,
                    new CollisionRay(position, castAngle.ToWorldVec(),
                        (int) (CollisionGroup.Impassable | CollisionGroup.MobImpassable)), Range, ignore).ToList();

                if (res.Count != 0)
                {
                    resSet.Add(res[0].HitEntity);
                }
            }

            return resSet;
        }

        void IHandSelected.HandSelected(HandSelectedEventArgs eventArgs)
        {
            var curTime = _gameTiming.CurTime;
            var cool = TimeSpan.FromSeconds(CooldownTime * 0.5f);

            if (curTime < _cooldownEnd)
            {
                if (_cooldownEnd - curTime < cool)
                {
                    _lastAttackTime = curTime;
                    _cooldownEnd += cool;
                }
                else
                    return;
            }
            else
            {
                _lastAttackTime = curTime;
                _cooldownEnd = curTime + cool;
            }

            RefreshItemCooldown();
        }

        private void RefreshItemCooldown()
        {
            if (Owner.TryGetComponent(out ItemCooldownComponent? cooldown))
            {
                cooldown.CooldownStart = _lastAttackTime;
                cooldown.CooldownEnd = _cooldownEnd;
            }
        }
    }

    public class MeleeHitMessage : ComponentMessage
    {
        public readonly List<IEntity> HitEntities;

        public MeleeHitMessage(List<IEntity> hitEntities)
        {
            HitEntities = hitEntities;
        }
    }
}
