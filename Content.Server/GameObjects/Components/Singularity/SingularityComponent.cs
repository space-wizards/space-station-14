#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.StationEvents;
using Content.Server.GameObjects.Components.Observer;
using Content.Shared.GameObjects;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class SingularityComponent : Component, ICollideBehavior
    {
        [Dependency] private readonly IRobustRandom _random = default!;

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
        private int _energy = 180;

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

                _spriteComponent?.LayerSetRSI(0, "Constructible/Power/Singularity/singularity_" + _level + ".rsi");
                _spriteComponent?.LayerSetState(0, "singularity_" + _level);

                if(_collidableComponent != null && _collidableComponent.PhysicsShapes.Any() && _collidableComponent.PhysicsShapes[0] is PhysShapeCircle circle)
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
        private PhysicsComponent? _collidableComponent;
        private SpriteComponent? _spriteComponent;
        private RadiationPulseComponent? _radiationPulseComponent;
        private AudioSystem _audioSystem = null!;
        private AudioSystem.AudioSourceServer? _playingSound;

        public override void Initialize()
        {
            base.Initialize();

            _audioSystem = EntitySystem.Get<AudioSystem>();
            var audioParams = AudioParams.Default;
            audioParams.Loop = true;
            audioParams.MaxDistance = 20f;
            audioParams.Volume = 5;
            _audioSystem.PlayFromEntity("/Audio/Effects/singularity_form.ogg", Owner);
            Timer.Spawn(5200,() => _playingSound = _audioSystem.PlayFromEntity("/Audio/Effects/singularity.ogg", Owner, audioParams));


            if (!Owner.TryGetComponent(out _collidableComponent))
            {
                Logger.Error("SingularityComponent was spawned without CollidableComponent");
            }
            else
            {
                _collidableComponent.Hard = false;
            }

            if (!Owner.TryGetComponent(out _spriteComponent))
            {
                Logger.Error("SingularityComponent was spawned without SpriteComponent");
            }

            _singularityController = _collidableComponent?.EnsureController<SingularityController>();
            if(_singularityController!=null)_singularityController.ControlledComponent = _collidableComponent;

            if (!Owner.TryGetComponent(out _radiationPulseComponent))
            {
                Logger.Error("SingularityComponent was spawned without RadiationPulseComponent");
            }

            Level = 1;
        }

        public void Update()
        {
            Energy -= EnergyDrain;

            if(Level == 1) return;
            //pushing
            var pushVector = new Vector2((_random.Next(-10, 10)), _random.Next(-10, 10));
            while (pushVector.X == 0 && pushVector.Y == 0)
            {
                pushVector = new Vector2((_random.Next(-10, 10)), _random.Next(-10, 10));
            }
            _singularityController?.Push(pushVector.Normalized, 2);
        }

        private readonly List<IEntity> _previousPulledEntities = new();
        public void CleanupPulledEntities()
        {
            foreach (var previousPulledEntity in _previousPulledEntities)
            {
                if(previousPulledEntity.Deleted) continue;
                if (!previousPulledEntity.TryGetComponent<PhysicsComponent>(out var collidableComponent)) continue;
                var controller = collidableComponent.EnsureController<SingularityPullController>();
                controller.StopPull();
            }
            _previousPulledEntities.Clear();
        }

        public void PullUpdate()
        {
            CleanupPulledEntities();
            var entitiesToPull = Owner.EntityManager.GetEntitiesInRange(Owner.Transform.Coordinates, Level * 10);
            foreach (var entity in entitiesToPull)
            {
                if (!entity.TryGetComponent<PhysicsComponent>(out var collidableComponent)) continue;
                if (entity.HasComponent<GhostComponent>()) continue;
                var controller = collidableComponent.EnsureController<SingularityPullController>();
                if(Owner.Transform.Coordinates.EntityId != entity.Transform.Coordinates.EntityId) continue;
                var vec = (Owner.Transform.Coordinates - entity.Transform.Coordinates).Position;
                if (vec == Vector2.Zero) continue;

                var speed = 10 / vec.Length * Level;

                controller.Pull(vec.Normalized, speed);
                _previousPulledEntities.Add(entity);
            }
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

            if (entity.HasComponent<ContainmentFieldComponent>() || (entity.TryGetComponent<ContainmentFieldGeneratorComponent>(out var component) && component.CanRepell(Owner)))
            {
                return;
            }

            if (entity.IsInContainer()) return;

            entity.Delete();
            Energy++;
        }

        public override void OnRemove()
        {
            _playingSound?.Stop();
            _audioSystem.PlayAtCoords("/Audio/Effects/singularity_collapse.ogg", Owner.Transform.Coordinates);
            CleanupPulledEntities();
            base.OnRemove();
        }
    }
}
