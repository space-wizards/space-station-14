using System;
using Content.Shared.Radiation;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;

namespace Content.Server.Radiation
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedRadiationPulseComponent))]
    public sealed class RadiationPulseComponent : SharedRadiationPulseComponent
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private float _duration;
        private float _radsPerSecond = 40f;
        private float _range = 5f;
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

        [DataField("dps")]
        public override float RadsPerSecond
        {
            get => _radsPerSecond;
            set
            {
                _radsPerSecond = value;
                Dirty();
            }
        }

        [DataField("sound")] public string? Sound { get; set; } = "/Audio/Weapons/Guns/Gunshots/laser3.ogg";

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

        public override TimeSpan EndTime => _endTime;

        public void DoPulse()
        {
            if (Decay)
            {
                var currentTime = _gameTiming.CurTime;
                _duration = _random.NextFloat() * (MaxPulseLifespan - MinPulseLifespan) + MinPulseLifespan;
                _endTime = currentTime + TimeSpan.FromSeconds(_duration);
            }

            if(!string.IsNullOrEmpty(Sound))
                SoundSystem.Play(Filter.Pvs(Owner), Sound, Owner.Transform.Coordinates);

            Dirty();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new RadiationPulseState(_radsPerSecond, _range, Draw, Decay, _endTime);
        }

        public void Update(float frameTime)
        {
            if (!Decay || Owner.Deleted)
                return;

            if(_duration <= 0f)
                Owner.QueueDelete();

            _duration -= frameTime;
        }
    }
}
