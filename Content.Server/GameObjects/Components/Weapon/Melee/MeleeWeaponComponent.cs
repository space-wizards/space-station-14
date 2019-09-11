using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
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
        [Dependency] private readonly IGameTiming _gameTiming;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649

        private int _damage = 1;
        private float _range = 1;
        private float _arcWidth = 90;
        private string _arc;

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
        }

        void IAttack.Attack(AttackEventArgs eventArgs)
        {
            var location = eventArgs.User.Transform.GridPosition;
            var angle = new Angle(eventArgs.ClickLocation.ToWorld(_mapManager).Position -
                                  location.ToWorld(_mapManager).Position);
            var entities =
                _serverEntityManager.GetEntitiesInArc(eventArgs.User.Transform.GridPosition, Range, angle, ArcWidth);

            foreach (var entity in entities)
            {
                if (!entity.Transform.IsMapTransform || entity == eventArgs.User)
                    continue;

                if (entity.TryGetComponent(out DamageableComponent damageComponent))
                {
                    damageComponent.TakeDamage(DamageType.Brute, Damage);
                }
            }

            if (Arc != null)
            {
                _entitySystemManager.GetEntitySystem<MeleeWeaponSystem>()
                    .SendArc(Arc, eventArgs.User.Transform.GridPosition, angle);

                /*
                var effects = _entitySystemManager.GetEntitySystem<EffectSystem>();
                var time = _gameTiming.CurTime;

                var offset = angle.RotateVec(new Vector2(0.5f, 0));

                var arcPrototype = _prototypeManager.Index<WeaponArcPrototype>(Arc);

                var effectSystemMessage = new EffectSystemMessage
                {
                    Color = arcPrototype.Color,
                    ColorDelta = arcPrototype.ColorDelta,
                    EffectSprite = "/Textures/Effects/weapons/arcs.rsi",
                    RsiState = arcPrototype.State,
                    Born = time,
                    DeathTime = time + arcPrototype.Length
                };

                switch (arcPrototype.ArcType)
                {
                    case WeaponArcType.Slash:
                        effectSystemMessage.EmitterCoordinates = eventArgs.User.Transform.GridPosition;
                        effectSystemMessage.Coordinates = eventArgs.User.Transform.GridPosition.Offset(offset);
                        effectSystemMessage.Rotation = (float) angle;
                        effectSystemMessage.RadialVelocity = arcPrototype.Speed;
                        break;
                    case WeaponArcType.Poke:
                        effectSystemMessage.Coordinates = eventArgs.User.Transform.GridPosition.Offset(offset);
                        effectSystemMessage.Rotation = (float) angle;
                        effectSystemMessage.Velocity = angle.RotateVec(new Vector2(1, 0) * arcPrototype.Speed);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                effects.CreateParticle(effectSystemMessage);
                */
            }

            if (eventArgs.User.TryGetComponent(out CameraRecoilComponent recoilComponent))
            {
                recoilComponent.Kick(angle.RotateVec((0.15f, 0)));
            }
        }
    }
}
