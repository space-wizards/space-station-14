using System;
using Content.Shared.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Server.GameObjects.Components.StationEvents
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedRadiationPulseComponent))]
    public sealed class RadiationPulseComponent : SharedRadiationPulseComponent
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private float _duration;
        private float _radsPerSecond;
        private float _range;
        private TimeSpan _endTime;
        private bool _draw;
        private bool _decay;

        /// <summary>
        ///     Whether the entity will delete itself after a certain duration defined by
        ///     <see cref="MinPulseLifespan"/> and <see cref="MaxPulseLifespan"/>
        /// </summary>
        public override bool Decay
        {
            get => _decay;
            set
            {
                _decay = value;
                Dirty();
            }
        }

        public float MinPulseLifespan { get; set; }

        public float MaxPulseLifespan { get; set; }

        public override float RadsPerSecond
        {
            get => _radsPerSecond;
            set
            {
                _radsPerSecond = value;
                Dirty();
            }
        }

        public string Sound { get; set; }

        public override float Range
        {
            get => _range;
            set
            {
                _range = value;
                Dirty();
            }
        }

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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.RadsPerSecond, "dps", 40.0f);
            serializer.DataField(this, x => x.Sound, "sound", "/Audio/Weapons/Guns/Gunshots/laser3.ogg");
            serializer.DataField(this, x => x.Range, "range", 5.0f);
            serializer.DataField(this, x => x.Draw, "draw", true);
            serializer.DataField(this, x => x.Decay, "decay", true);
            serializer.DataField(this, x => x.MaxPulseLifespan, "maxPulseLifespan", 2.5f);
            serializer.DataField(this, x => x.MinPulseLifespan, "minPulseLifespan", 0.8f);
        }

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

        public override ComponentState GetComponentState(ICommonSession player)
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
