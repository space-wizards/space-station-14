#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.StationEvents;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Timers;
using Content.Server.GameObjects.Components.Effects;
using Content.Server.GameObjects.Components.Observer;

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
                    if(_singularityController != null) _singularityController.LinearVelocity = Vector2.Zero;

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
                _level = 5;
                value = 5;

                if(_radiationPulseComponent != null) _radiationPulseComponent.RadsPerSecond = 10 * value;
                if (_shaderComponent != null)
                {
                    _shaderComponent.SetSingularityTexture("Effects/Singularity/singularity_" + _level + ".rsi", "singularity_" + _level);
                    switch (value)
                    {
                        case 0:
                            _shaderComponent.SetEffectIntensity(0.0f, 100.0f);
                            break;
                        case 1:
                            _shaderComponent.SetEffectIntensity(2.7f, 6.4f);
                            break;
                        case 2:
                            _shaderComponent.SetEffectIntensity(14.4f, 7.0f);
                            break;
                        case 3:
                            _shaderComponent.SetEffectIntensity(39.2f, 8.0f);
                            break;
                        case 4:
                            _shaderComponent.SetEffectIntensity(180f, 10.0f);
                            break;
                        case 5:
                            _shaderComponent.SetEffectIntensity(600f, 12.0f);
                            break;
                        case 6:
                            _shaderComponent.SetEffectIntensity(600f, 12.0f);
                            break;
                    }
                }
                //_shaderComponent.

                if (_collidableComponent != null && _collidableComponent.PhysicsShapes.Any() && _collidableComponent.PhysicsShapes[0] is PhysShapeCircle circle)
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
        private SingularityShaderAuraComponent? _shaderComponent;
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

            if (!Owner.TryGetComponent(out _shaderComponent))
            {
                Logger.Error("SingularityComponent was spawned without SingularityShaderAuraComponent");
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

        List<IEntity> _previousPulledEntites = new List<IEntity>();
        public void PullUpdate()
        {
            foreach (var previousPulledEntity in _previousPulledEntites)
            {
                if(previousPulledEntity.Deleted) continue;
                if (!previousPulledEntity.TryGetComponent<PhysicsComponent>(out var collidableComponent)) continue;
                var controller = collidableComponent.EnsureController<SingularityPullController>();
                controller.StopPull();
            }
            _previousPulledEntites.Clear();

            var entitiesToPull = _entityManager.GetEntitiesInRange(Owner.Transform.Coordinates, Level * 10);
            
            foreach (var entity in entitiesToPull)
            {
                if (entity.TryGetComponent<GhostComponent>(out var a)) continue; //Temporary fix for ghosts
                if (!entity.TryGetComponent<PhysicsComponent>(out var collidableComponent)) continue;
                var controller = collidableComponent.EnsureController<SingularityPullController>();
                if(Owner.Transform.Coordinates.EntityId != entity.Transform.Coordinates.EntityId) continue;
                var vec = (Owner.Transform.Coordinates - entity.Transform.Coordinates).Position;
                if (vec == Vector2.Zero) continue;

                var speed = 10 / vec.Length * Level;

                controller.Pull(vec.Normalized, speed);
                _previousPulledEntites.Add(entity);
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

            if (ContainerHelpers.IsInContainer(entity)) return;

            entity.Delete();
            Energy++;
        }

        public override void OnRemove()
        {
            _playingSound?.Stop();
            _audioSystem.PlayAtCoords("/Audio/Effects/singularity_collapse.ogg", Owner.Transform.Coordinates);
            base.OnRemove();
        }
    }
}
