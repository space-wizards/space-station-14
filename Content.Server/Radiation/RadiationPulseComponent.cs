using System;
using Content.Shared.Radiation;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;

namespace Content.Server.Radiation
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedRadiationPulseComponent))]
    public sealed class RadiationPulseComponent : SharedRadiationPulseComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private float _duration;
        private float _range = 5f;
        private TimeSpan _startTime;
        private TimeSpan _endTime;
        private bool _draw = true;
        private bool _decay = true;

        /// <summary>
        ///     Whether the entity will delete itself after a certain duration defined by
        ///     <see cref="MinPulseLifespan"/> and <see cref="MaxPulseLifespan"/>
        /// </summary>
        [DataField("decay")]
        public override bool Decay
        {
            get => _decay;
            set
            {
                _decay = value;
                Dirty();
            }
        }

        [DataField("minPulseLifespan")]
        public float MinPulseLifespan { get; set; } = 0.8f;

        [DataField("maxPulseLifespan")]
        public float MaxPulseLifespan { get; set; } = 2.5f;

        [DataField("sound")] public SoundSpecifier Sound { get; set; } = new SoundCollectionSpecifier("RadiationPulse");

        [DataField("range")]
        public override float Range
        {
            get => _range;
            set
            {
                _range = value;
                Dirty();
            }
        }

        [DataField("draw")]
        public override bool Draw
        {
            get => _draw;
            set
            {
                _draw = value;
                Dirty();
            }
        }

        public override TimeSpan StartTime => _startTime;
        public override TimeSpan EndTime => _endTime;

        public void DoPulse()
        {
            if (Decay)
            {
                var currentTime = _gameTiming.CurTime;
                _startTime = currentTime;
                _duration = _random.NextFloat() * (MaxPulseLifespan - MinPulseLifespan) + MinPulseLifespan;
                _endTime = currentTime + TimeSpan.FromSeconds(_duration);
            }

            SoundSystem.Play(Filter.Pvs(Owner), Sound.GetSound(), Owner);

            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            return new RadiationPulseState(_range, Draw, Decay, _startTime, _endTime);
        }

        public void Update(float frameTime)
        {
            if (!Decay || _entMan.Deleted(Owner))
                return;

            if (_duration <= 0f)
                _entMan.QueueDeleteEntity(Owner);

            _duration -= frameTime;
        }
    }
}
