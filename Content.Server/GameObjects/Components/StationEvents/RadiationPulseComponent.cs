using System;
using Content.Shared.GameObjects.Components;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;

namespace Content.Server.GameObjects.Components.StationEvents
{
    [RegisterComponent]
    public sealed class RadiationPulseComponent : SharedRadiationPulseComponent
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public float MinPulseLifespan { get; set; }
        public float MaxPulseLifespan { get; set; }
        public float DPS { get; set; }
        public string Sound { get; set; }

        /// <summary>
        /// Radius of the pulse from its position
        /// </summary>
        public float Range { get; set; }

        public bool Draw { get; set; }

        private float _duration;
        private TimeSpan _endTime;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.DPS, "dps", 40.0f);
            serializer.DataField(this, x => x.Sound, "sound", "/Audio/Weapons/Guns/Gunshots/laser3.ogg");
            serializer.DataField(this, x => x.Range, "range", 5.0f);
            serializer.DataField(this, x => x.Draw, "draw", true);
            serializer.DataField(this, x => x.MaxPulseLifespan, "maxPulseLifespan", 2.5f);
            serializer.DataField(this, x => x.MinPulseLifespan, "minPulseLifespan", 0.8f);
        }

        public void DoPulse()
        {
            var currentTime = _gameTiming.CurTime;
            _duration = _random.NextFloat() * (MaxPulseLifespan - MinPulseLifespan) + MinPulseLifespan;
            _endTime = currentTime + TimeSpan.FromSeconds(_duration);

            if(!string.IsNullOrEmpty(Sound))
                EntitySystem.Get<AudioSystem>().PlayAtCoords(Sound, Owner.Transform.GridPosition);

            if(Draw)
                Dirty();
        }

        public override ComponentState GetComponentState()
        {
            return new RadiationPulseState(_endTime, Range);
        }

        public void Update(float frameTime)
        {
            if (!Owner.Deleted)
                return;

            if(_duration <= 0f)
                Owner.Delete();

            _duration -= frameTime;
        }
    }
}
