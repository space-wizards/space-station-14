#nullable enable
using Content.Server.GameObjects.Components.Observer;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.StationEvents;
using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics.Shapes;
using Robust.Shared.Random;
using Robust.Server.GameObjects;
using Content.Shared.GameObjects.Components.Singularity;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Timing;


namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class ServerSingularityComponent : SharedSingularityComponent, IStartCollide
    {
        [Dependency] private readonly IRobustRandom _random = default!;


        public int Energy
        {
            get => _energy;
            set
            {
                if (value == _energy) return;

                _energy = value;
                if (_energy <= 0)
                {
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

                if (_collidableComponent != null && _collidableComponent.Fixtures.Any() && _collidableComponent.Fixtures[0].Shape is PhysShapeCircle circle)
                {
                    circle.Radius = _level - 0.5f;
                }

                Dirty();
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

        private PhysicsComponent? _collidableComponent;
        private SpriteComponent? _spriteComponent;
        private RadiationPulseComponent? _radiationPulseComponent;
        private IPlayingAudioStream? _playingSound;

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new SingularityComponentState(Level);
        }

        public override void Initialize()
        {
            base.Initialize();

            var audioParams = AudioParams.Default;
            audioParams.Loop = true;
            audioParams.MaxDistance = 20f;
            audioParams.Volume = 5;
            SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Effects/singularity_form.ogg", Owner);
            Timer.Spawn(5200,() => _playingSound = SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Effects/singularity.ogg", Owner, audioParams));

            if (!Owner.TryGetComponent(out _spriteComponent))
                Logger.Error("SingularityComponent was spawned without SpriteComponent");
            if (!Owner.TryGetComponent(out _radiationPulseComponent))
                Logger.Error("SingularityComponent was spawned without RadiationPulseComponent");
            if (!Owner.TryGetComponent(out _collidableComponent))
                Logger.Error("SingularityComponent was spawned without CollidableComponent!");
            else
                _collidableComponent.Hard = false;
            Level = 1;
        }

        public void Update(int seconds)
        {
            Energy -= EnergyDrain * seconds;
        }

        void IStartCollide.CollideWith(IPhysBody ourBody, IPhysBody otherBody, in Manifold manifold)
        {
            var otherEntity = otherBody.Entity;

            if (otherEntity.TryGetComponent<IMapGridComponent>(out var mapGridComponent))
            {
                foreach (var tile in mapGridComponent.Grid.GetTilesIntersecting(ourBody.GetWorldAABB()))
                {
                    mapGridComponent.Grid.SetTile(tile.GridIndices, Tile.Empty);
                    Energy++;
                }
                return;
            }

            if (otherEntity.HasComponent<ContainmentFieldComponent>() || (otherEntity.TryGetComponent<ContainmentFieldGeneratorComponent>(out var component) && component.CanRepell(Owner)))
            {
                return;
            }

            if (otherEntity.IsInContainer())
                return;

            otherEntity.Delete();
            Energy++;
        }

        public override void OnRemove()
        {
            _playingSound?.Stop();
            SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Effects/singularity_collapse.ogg", Owner.Transform.Coordinates);
            base.OnRemove();
        }
    }
}
