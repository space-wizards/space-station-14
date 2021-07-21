using Content.Shared.Singularity;
using Content.Shared.Singularity.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.Singularity.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSingularityComponent))]
    public class ServerSingularityComponent : SharedSingularityComponent
    {
        private SharedSingularitySystem _singularitySystem = default!;

        [ViewVariables(VVAccess.ReadWrite)]
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

                var level = _energy switch
                {
                    >= 1500 => 6,
                    >= 1000 => 5,
                    >= 600 => 4,
                    >= 300 => 3,
                    >= 200 => 2,
                    < 200 => 1
                };
                _singularitySystem.ChangeSingularityLevel(this, level);
            }
        }
        private int _energy = 180;

        [ViewVariables]
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

        // This is an interesting little workaround.
        // See, two singularities queuing deletion of each other at the same time will annihilate.
        // This is undesirable behaviour, so this flag allows the imperatively first one processed to take priority.
        [ViewVariables(VVAccess.ReadWrite)]
        public bool BeingDeletedByAnotherSingularity { get; set; }

        private IPlayingAudioStream? _playingSound;

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new SingularityComponentState(Level);
        }

        protected override void Initialize()
        {
            base.Initialize();

            _singularitySystem = EntitySystem.Get<SharedSingularitySystem>();

            var audioParams = AudioParams.Default;
            audioParams.Loop = true;
            audioParams.MaxDistance = 20f;
            audioParams.Volume = 5;
            SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Effects/singularity_form.ogg", Owner);
            Timer.Spawn(5200,() => _playingSound = SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Effects/singularity.ogg", Owner, audioParams));

            _singularitySystem.ChangeSingularityLevel(this, 1);
        }

        public void Update(int seconds)
        {
            Energy -= EnergyDrain * seconds;
        }

        protected override void OnRemove()
        {
            _playingSound?.Stop();
            SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Effects/singularity_collapse.ogg", Owner.Transform.Coordinates);
            base.OnRemove();
        }
    }
}
