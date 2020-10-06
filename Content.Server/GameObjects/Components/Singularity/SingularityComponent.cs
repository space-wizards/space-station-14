#nullable enable
using System;
using Content.Server.GameObjects.Components.StationEvents;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class SingularityComponent : Component, ICollideBehavior
    {
        [Dependency] private IEntityManager _entityManager = null!;
        [Dependency] private IMapManager _mapManager = null!;
        [Dependency] private IRobustRandom _random = null!;


        public override uint? NetID => ContentNetIDs.SINGULARITY;

        public override string Name => "Singularity";

        public int Energy
        {
            get => _energy;
            set
            {
                if (value == _energy) return;

                _energy = value;
                if (_energy <= 0)
                {
                    SendNetworkMessage(new SingularitySoundMessage(false));

                    if(_singularityController != null) _singularityController.LinearVelocity = Vector2.Zero;
                    _spriteComponent?.LayerSetVisible(0, false);

                    Owner.Delete();
                    return;
                }

                Level = _energy switch
                {
                    var n when n >= 1500 => 6,
                    var n when n >= 1000 => 5,
                    var n when n >= 600 => 4,
                    var n when n >= 300 => 3,
                    var n when n >= 200 => 2,
                    var n when n <  200 => 1,
                    _ => 1
                };
            }
        }
        private int _energy = 100;

        public int Level
        {
            get => _level;
            set
            {
                if (value == _level) return;
                if (value < 0) value = 0;
                if (value > 6) value = 6;

                _level = value;

                if(_radiationPulseComponent != null) _radiationPulseComponent.RadsPerSecond = 10 * value;

                _spriteComponent?.LayerSetRSI(0, "Effects/Singularity/singularity_" + _level + ".rsi");
                _spriteComponent?.LayerSetState(0, "singularity_" + _level);

                if(_collidableComponent != null && _collidableComponent.PhysicsShapes[0] is PhysShapeCircle circle)
                {
                    circle.Radius = _level - 0.5f;
                }
            }
        }
        private int _level;

        public int EnergyDrain =>
            Level switch
            {
                6 => 20,
                5 => 15,
                4 => 10,
                3 => 5,
                2 => 2,
                1 => 1,
                _ => 0
            };

        private SingularityController? _singularityController;
        private CollidableComponent? _collidableComponent;
        private SpriteComponent? _spriteComponent;
        private RadiationPulseComponent? _radiationPulseComponent;



        public override void Initialize()
        {
            base.Initialize();

            if (!Owner.TryGetComponent<CollidableComponent>(out var _collidableComponent))
            {
                Logger.Error("SingularityComponent was spawned without CollidableComponent");
            }
            else
            {
                _collidableComponent.Hard = false;
            }

            if (!Owner.TryGetComponent<SpriteComponent>(out var _spriteComponent))
            {
                Logger.Error("SingularityComponent was spawned without SpriteComponent");
            }

            _singularityController = _collidableComponent?.EnsureController<SingularityController>();
            if(_singularityController!=null)_singularityController.ControlledComponent = _collidableComponent;

            if (!Owner.TryGetComponent<RadiationPulseComponent>(out var _radiationPulseComponent))
            {
                Logger.Error("SingularityComponent was spawned without RadiationPulseComponent");
            }

            Level = 1;
        }

        protected override void Startup()
        {
            SendNetworkMessage(new SingularitySoundMessage(true));
        }

        public void Update()
        {
            Energy -= EnergyDrain;

            var pushVector = new Vector2((_random.Next(-10, 10)), _random.Next(-10, 10));
            while (pushVector.X == 0 && pushVector.Y == 0)
            {
                pushVector = new Vector2((_random.Next(-10, 10)), _random.Next(-10, 10));
            }

            _singularityController?.Push(pushVector.Normalized, 2);
        }

        void ICollideBehavior.CollideWith(IEntity entity)
        {
            if (_collidableComponent == null) return; //how did it even collide then? :D

            if (entity.TryGetComponent<IMapGridComponent>(out var mapGridComponent))
            {
                foreach (var tile in mapGridComponent.Grid.GetTilesIntersecting(((IPhysBody) _collidableComponent).WorldAABB))
                {
                    mapGridComponent.Grid.SetTile(tile.GridIndices, Tile.Empty);
                    Energy++;
                }
                return;
            }

            if (entity.HasComponent<ContainmentFieldComponent>() || (entity.TryGetComponent<ContainmentFieldGeneratorComponent>(out var component) && component.Power >= 1))
            {
                //todo check if we overlap them, then eat
                return;
            }

            if (ContainerHelpers.IsInContainer(entity)) return;

            entity.Delete();
            Energy++;
        }
    }
}
