using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
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

#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IServerEntityManager _serverEntityManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        private int _damage = 1;
        private float _range = 1;
        private float _arcWidth = 90;
        private string _arc;
        private string _hitSound;

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
        }

        void IAttack.Attack(AttackEventArgs eventArgs)
        {
            var location = eventArgs.User.Transform.GridPosition;
            var angle = new Angle(eventArgs.ClickLocation.ToWorld(_mapManager).Position -
                                  location.ToWorld(_mapManager).Position);

            // This should really be improved. GetEntitiesInArc uses pos instead of bounding boxes.
            var entities =
                _serverEntityManager.GetEntitiesInArc(eventArgs.User.Transform.GridPosition, Range, angle, ArcWidth);

            var hitEntities = new List<IEntity>();
            foreach (var entity in entities)
            {
                if (!entity.Transform.IsMapTransform || entity == eventArgs.User)
                    continue;

                if (entity.TryGetComponent(out DamageableComponent damageComponent))
                {
                    damageComponent.TakeDamage(DamageType.Brute, Damage);
                    hitEntities.Add(entity);
                }
            }

            var audioSystem = _entitySystemManager.GetEntitySystem<AudioSystem>();
            audioSystem.Play(hitEntities.Count > 0  ? _hitSound : "/Audio/weapons/punchmiss.ogg");

            if (Arc != null)
            {
                var sys = _entitySystemManager.GetEntitySystem<MeleeWeaponSystem>();
                sys.SendAnimation(Arc, angle, eventArgs.User, hitEntities);
            }
        }
    }
}
