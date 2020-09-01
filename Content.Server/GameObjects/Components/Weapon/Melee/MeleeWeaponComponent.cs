using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Items;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Content.Shared.Damage;
using Content.Shared.Interfaces.GameObjects.Components;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class MeleeWeaponComponent : Component, IAttack
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override string Name => "MeleeWeapon";
        private TimeSpan _lastAttackTime;

        private int _damage;
        private float _range;
        private float _arcWidth;
        private string _arc;
        private string _hitSound;
        public float CooldownTime => _cooldownTime;
        private float _cooldownTime = 1f;

        [ViewVariables(VVAccess.ReadWrite)]
        public string Arc
        {
            get => _arc;
            set => _arc = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float ArcWidth
        {
            get => _arcWidth;
            set => _arcWidth = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float Range
        {
            get => _range;
            set => _range = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public int Damage
        {
            get => _damage;
            set => _damage = value;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _damage, "damage", 5);
            serializer.DataField(ref _range, "range", 1);
            serializer.DataField(ref _arcWidth, "arcwidth", 90);
            serializer.DataField(ref _arc, "arc", "default");
            serializer.DataField(ref _hitSound, "hitSound", "/Audio/Weapons/genhit1.ogg");
            serializer.DataField(ref _cooldownTime, "cooldownTime", 1f);
        }

        protected virtual bool OnHitEntities(IReadOnlyList<IEntity> entities, AttackEventArgs eventArgs)
        {
            return true;
        }

        void IAttack.Attack(AttackEventArgs eventArgs)
        {
            var curTime = _gameTiming.CurTime;
            var span = curTime - _lastAttackTime;
            if(span.TotalSeconds < _cooldownTime) {
                return;
            }
            var location = eventArgs.User.Transform.GridPosition;
            var angle = new Angle(eventArgs.ClickLocation.ToMapPos(_mapManager) - location.ToMapPos(_mapManager));

            // This should really be improved. GetEntitiesInArc uses pos instead of bounding boxes.
            var entities = ArcRayCast(eventArgs.User.Transform.WorldPosition, angle, eventArgs.User);

            var audioSystem = EntitySystem.Get<AudioSystem>();
            if (entities.Count() != 0)
            {
                audioSystem.PlayFromEntity( _hitSound, entities.First());
            }
            else
            {
                audioSystem.PlayFromEntity("/Audio/Weapons/punchmiss.ogg", eventArgs.User);
            }

            var hitEntities = new List<IEntity>();
            foreach (var entity in entities)
            {
                if (!entity.Transform.IsMapTransform || entity == eventArgs.User)
                    continue;

                if (entity.TryGetComponent(out IDamageableComponent damageComponent))
                {
                    damageComponent.ChangeDamage(DamageType.Blunt, Damage, false, Owner);
                    hitEntities.Add(entity);
                }
            }

            if(!OnHitEntities(hitEntities, eventArgs)) return;

            if (Arc != null)
            {
                var sys = _entitySystemManager.GetEntitySystem<MeleeWeaponSystem>();
                sys.SendAnimation(Arc, angle, eventArgs.User, hitEntities);
            }

            _lastAttackTime = _gameTiming.CurTime;

            if (Owner.TryGetComponent(out ItemCooldownComponent cooldown))
            {
                cooldown.CooldownStart = _lastAttackTime;
                cooldown.CooldownEnd = _lastAttackTime + TimeSpan.FromSeconds(_cooldownTime);
            }
        }

        private HashSet<IEntity> ArcRayCast(Vector2 position, Angle angle, IEntity ignore)
        {
            var widthRad = Angle.FromDegrees(ArcWidth);
            var increments = 1 + (35 * (int) Math.Ceiling(widthRad / (2 * Math.PI)));
            var increment = widthRad / increments;
            var baseAngle = angle - widthRad / 2;

            var resSet = new HashSet<IEntity>();

            var mapId = Owner.Transform.MapID;
            for (var i = 0; i < increments; i++)
            {
                var castAngle = new Angle(baseAngle + increment * i);
                var res = _physicsManager.IntersectRay(mapId, new CollisionRay(position, castAngle.ToVec(), 23), _range, ignore).FirstOrDefault();
                if (res.HitEntity != null)
                {
                    resSet.Add(res.HitEntity);
                }
            }

            return resSet;
        }
    }
}
