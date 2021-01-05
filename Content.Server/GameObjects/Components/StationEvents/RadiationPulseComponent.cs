using System;
using Content.Shared.GameObjects.Components;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.StationEvents
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
        [YamlField("decay")]
        public override bool Decay
        {
            get => _decay;
            set
            {
                _decay = value;
                Dirty();
            }
        }

        [YamlField("minPulseLifespan")]
        public float MinPulseLifespan { get; set; } = 0.8f;

        [YamlField("maxPulseLifespan")]
        public float MaxPulseLifespan { get; set; } = 2.5f;

        [YamlField("dps")]
        public override float RadsPerSecond
        {
            get => _radsPerSecond;
            set
            {
                _radsPerSecond = value;
                Dirty();
            }
        }

        [YamlField("sound")] public string Sound { get; set; } = "/Audio/Weapons/Guns/Gunshots/laser3.ogg";

        [YamlField("range")]
        public override float Range
        {
            get => _range;
            set
            {
                _range = value;
                Dirty();
            }
        }

        [YamlField("draw")]
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
                EntitySystem.Get<AudioSystem>().PlayAtCoords(Sound, Owner.Transform.Coordinates);

            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            return new RadiationPulseState(_radsPerSecond, _range, Draw, Decay, _endTime);
        }

        public void Update(float frameTime)
        {
            if (!Decay || Owner.Deleted)
                return;

            if(_duration <= 0f)
                Owner.Delete();

            _duration -= frameTime;
        }
    }
}
