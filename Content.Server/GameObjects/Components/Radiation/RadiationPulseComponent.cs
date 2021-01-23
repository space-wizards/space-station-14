#nullable enable
using System;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Radiation
{
    [ComponentReference(typeof(SharedRadiationPulseComponent))]
    public abstract class RadiationPulseComponent : SharedRadiationPulseComponent
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        [ComponentDependency] protected readonly AppearanceComponent? Appearance = default!;

        private float _energy;
        private float _range;
        private TimeSpan _endTime;
        private TimeSpan _startTime;

        private bool _decay;
        private float _duration;

        public override float Energy
        {
            get => _energy;
            set
            {
                _energy = value;
                Dirty();
            }
        }
        public override float Range
        {
            get => _range;
            protected set
            {
                _range = value;
                Dirty();
            }
        }
        public override TimeSpan EndTime
        {
            get => _endTime;
            protected set
            {
                _endTime = value;
                _duration = (float)(_endTime - _gameTiming.CurTime).TotalSeconds;
                Dirty();
            }
        }
        public override TimeSpan StartTime
        {
            get => _startTime;
            protected set
            {
                _startTime = value;
                Dirty();
            }
        }
        protected override bool Decay
        {
            get => _decay;
            set
            {
                _decay = value;
                Dirty();
            }
        }

        public override float Cooldown { get; protected set; }

        /// <summary>
        /// Limits the amount of radiation pulses the component can generate.
        /// This changes the realtime* damage approach but if the values remain between [0.0, 1.0]
        /// the expected value should remains the same.
        /// </summary>
        protected float MinCooldownAct { get; private set; }
        protected float MaxCooldownAct { get; private set; }

        /// <summary>
        /// Gives some variability to the lifespan of the pulses.
        /// </summary>
        protected float MinPulseLifespan { get; private set; }
        protected float MaxPulseLifespan { get; private set; }

        public string? Sound { get; private set; }

        public override void Initialize()
        {
            base.Initialize();

            _duration = _random.NextFloat() * (MaxPulseLifespan - MinPulseLifespan) + MinPulseLifespan;
            _startTime = _gameTiming.CurTime;

            ComputeCooldown();

            if (Decay)
            {
                UpdateEndTime();
                if (!string.IsNullOrEmpty(Sound))
                {
                    EntitySystem.Get<AudioSystem>().PlayAtCoords(Sound, Owner.Transform.Coordinates);
                }
            }
        }

        protected virtual void ComputeCooldown()
        {
            Cooldown = _random.NextFloat() * (MaxCooldownAct - MinCooldownAct) + MinCooldownAct;
        }

        private void UpdateEndTime()
        {
            EndTime = _startTime + TimeSpan.FromSeconds(_duration);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.Energy, "energy", 20.0f);
            serializer.DataField(this, x => x.Range, "range", 5.0f);
            serializer.DataField(this, x => x.Decay, "decay", true);
            serializer.DataField(this, x => x.MinPulseLifespan, "minPulseLifespan", 3.0f);
            serializer.DataField(this, x => x.MaxPulseLifespan, "maxPulseLifespan", 6.0f);
            serializer.DataField(this, x => x.MinCooldownAct, "minCooldownAct", 0.2f);
            serializer.DataField(this, x => x.MaxCooldownAct, "maxCooldownAct", 0.6f);
        }

        public void Update(float frameTime)
        {
            if (!Decay || Owner.Deleted)
            {
                return;
            }

            if (_duration <= 0.0f)
            {
                Owner.Delete();
            }

            _duration -= frameTime;
        }

        public abstract bool CanRadiate(IEntity entity);
    }

    [RegisterComponent]
    [ComponentReference(typeof(RadiationPulseComponent))]
    public sealed class RadiationPulseAnomaly : RadiationPulseComponent
    {
        public override string Name => "RadiationPulseAnomaly";
        public override uint? NetID => ContentNetIDs.RADIATION_PULSE;

        public override void Initialize()
        {
            base.Initialize();
            Appearance?.SetData(RadiationPulseVisual.State, RadiationPulseVisuals.Visible);
        }

        public override bool CanRadiate(IEntity entity)
        {
            return this.InRangeUnOccluded(entity, Range);
        }

        public override ComponentState GetComponentState()
        {
            return new RadiationPulseAnomalyState(Range, StartTime, EndTime);
        }
    }

    [RegisterComponent]
    [ComponentReference(typeof(RadiationPulseComponent))]
    public sealed class RadiationPulseSingularity : RadiationPulseComponent
    {
        public override string Name => "RadiationPulseSingularity";

        public override void Initialize()
        {
            base.Initialize();
            Appearance?.SetData(RadiationPulseVisual.State, RadiationPulseVisuals.None);
        }

        public override bool CanRadiate(IEntity entity)
        {
            return true;
        }
    }
}
