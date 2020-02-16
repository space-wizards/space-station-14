using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Items;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class MeleeWeaponComponent : Component, IAttack
    {
        public override string Name => "MeleeWeapon";
        private TimeSpan _lastAttackTime;

#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IPhysicsManager _physicsManager;
#pragma warning restore 649

        private int _damage = 1;
        private float _range = 1;
        private float _arcWidth = 90;
        private string _arc;
        private string _hitSound;
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
            serializer.DataField(ref _hitSound, "hitSound", "/Audio/weapons/genhit1.ogg");
            serializer.DataField(ref _cooldownTime, "cooldownTime", 1f);
        }

        void IAttack.Attack(AttackEventArgs eventArgs)
        {
            var curTime = IoCManager.Resolve<IGameTiming>().CurTime;
            var span = curTime - _lastAttackTime;
            if(span.TotalSeconds < _cooldownTime) {
                return;
            }
            var location = eventArgs.User.Transform.GridPosition;
            var angle = new Angle(eventArgs.ClickLocation.ToMapPos(_mapManager) - location.ToMapPos(_mapManager));

            // This should really be improved. GetEntitiesInArc uses pos instead of bounding boxes.
            var entities = ArcRayCast(eventArgs.User.Transform.WorldPosition, angle, eventArgs.User);

            var hitEntities = new List<IEntity>();
            foreach (var entity in entities)
            {
                if (!entity.Transform.IsMapTransform || entity == eventArgs.User)
                    continue;

                if (entity.TryGetComponent(out DamageableComponent damageComponent))
                {
                    damageComponent.TakeDamage(DamageType.Brute, Damage, Owner, eventArgs.User);
                    hitEntities.Add(entity);
                }
            }

            var audioSystem = _entitySystemManager.GetEntitySystem<AudioSystem>();
            var emitter = hitEntities.Count == 0 ? eventArgs.User : hitEntities[0];
            audioSystem.Play(hitEntities.Count > 0 ? _hitSound : "/Audio/weapons/punchmiss.ogg", emitter);

            if (Arc != null)
            {
                var sys = _entitySystemManager.GetEntitySystem<MeleeWeaponSystem>();
                sys.SendAnimation(Arc, angle, eventArgs.User, hitEntities);
            }

            _lastAttackTime = IoCManager.Resolve<IGameTiming>().CurTime;

            if (Owner.TryGetComponent(out ItemCooldownComponent cooldown))
            {
                cooldown.CooldownStart = _lastAttackTime;
                cooldown.CooldownEnd = _lastAttackTime + TimeSpan.FromSeconds(_cooldownTime);
            }
        }

        private HashSet<IEntity> ArcRayCast(Vector2 position, Angle angle, IEntity ignore)
        {
            // Maybe make this increment count depend on the width/length?
            const int increments = 5;
            var widthRad = Angle.FromDegrees(ArcWidth);
            var increment = widthRad / increments;
            var baseAngle = angle - widthRad / 2;

            var resSet = new HashSet<IEntity>();

            var mapId = Owner.Transform.MapID;
            for (var i = 0; i < increments; i++)
            {
                var castAngle = new Angle(baseAngle + increment * i);
                var res = _physicsManager.IntersectRay(mapId, new CollisionRay(position, castAngle.ToVec(), 23), _range, ignore);
                if (res.HitEntity != null)
                {
                    resSet.Add(res.HitEntity);
                }
            }

            return resSet;
        }
    }
}
