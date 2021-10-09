using System;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Circulatory;
using Content.Server.Body.Respiratory;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.MobState;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Behavior
{
    [DataDefinition]
    public class LungBehavior : MechanismBehavior, ISerializationHooks
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private float _accumulatedFrameTime;

        [ViewVariables] private TimeSpan _lastGaspPopupTime;

        [DataField("air")]
        [ViewVariables]
        public GasMixture Air { get; set; } = new()
        {
            Volume = 6,
            Temperature = Atmospherics.NormalBodyTemperature
        };

        [DataField("gaspPopupCooldown")]
        [ViewVariables]
        public TimeSpan GaspPopupCooldown { get; private set; } = TimeSpan.FromSeconds(8);

        [ViewVariables] public LungStatus Status { get; set; }

        [DataField("cycleDelay")]
        [ViewVariables]
        public float CycleDelay { get; set; } = 2;

        void ISerializationHooks.AfterDeserialization()
        {
            IoCManager.InjectDependencies(this);
        }

        protected override void OnAddedToBody(SharedBodyComponent body)
        {
            base.OnAddedToBody(body);
            Inhale(CycleDelay);
        }

        public void Gasp()
        {
            if (_gameTiming.CurTime >= _lastGaspPopupTime + GaspPopupCooldown)
            {
                _lastGaspPopupTime = _gameTiming.CurTime;
                Owner.PopupMessageEveryone(Loc.GetString("lung-behavior-gasp"));
            }

            Inhale(CycleDelay);
        }

        public void Transfer(GasMixture from, GasMixture to, float ratio)
        {
            EntitySystem.Get<AtmosphereSystem>().Merge(to, from.RemoveRatio(ratio));
        }

        public void ToBloodstream(GasMixture mixture)
        {
            if (Body == null)
            {
                return;
            }

            if (!Body.Owner.TryGetComponent(out BloodstreamComponent? bloodstream))
            {
                return;
            }

            var to = bloodstream.Air;

            EntitySystem.Get<AtmosphereSystem>().Merge(to, mixture);
            mixture.Clear();
        }

        public override void Update(float frameTime)
        {
            if (Body != null && Body.Owner.TryGetComponent(out IMobStateComponent? mobState) && mobState.IsCritical())
            {
                return;
            }

            if (Status == LungStatus.None)
            {
                Status = LungStatus.Inhaling;
            }

            _accumulatedFrameTime += Status switch
            {
                LungStatus.Inhaling => frameTime,
                LungStatus.Exhaling => -frameTime,
                _ => throw new ArgumentOutOfRangeException()
            };

            var absoluteTime = Math.Abs(_accumulatedFrameTime);
            var delay = CycleDelay;

            if (absoluteTime < delay)
            {
                return;
            }

            switch (Status)
            {
                case LungStatus.Inhaling:
                    Inhale(absoluteTime);
                    Status = LungStatus.Exhaling;
                    break;
                case LungStatus.Exhaling:
                    Exhale(absoluteTime);
                    Status = LungStatus.Inhaling;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _accumulatedFrameTime = absoluteTime - delay;
        }

        public void Inhale(float frameTime)
        {
            if (Body != null &&
                Body.Owner.TryGetComponent(out InternalsComponent? internals) &&
                internals.BreathToolEntity != null &&
                internals.GasTankEntity != null &&
                internals.BreathToolEntity.TryGetComponent(out BreathToolComponent? breathTool) &&
                breathTool.IsFunctional &&
                internals.GasTankEntity.TryGetComponent(out GasTankComponent? gasTank) &&
                gasTank.Air != null)
            {
                Inhale(frameTime, gasTank.RemoveAirVolume(Atmospherics.BreathVolume));
                return;
            }

            if (EntitySystem.Get<AtmosphereSystem>().GetTileMixture(Owner.Transform.Coordinates, true) is not {} tileAir)
            {
                return;
            }

            Inhale(frameTime, tileAir);
        }

        public void Inhale(float frameTime, GasMixture from)
        {
            var ratio = (Atmospherics.BreathVolume / from.Volume) * frameTime;

            Transfer(from, Air, ratio);
            ToBloodstream(Air);
        }

        public void Exhale(float frameTime)
        {
            if (EntitySystem.Get<AtmosphereSystem>().GetTileMixture(Owner.Transform.Coordinates, true) is not {} tileAir)
            {
                return;
            }

            Exhale(frameTime, tileAir);
        }

        public void Exhale(float frameTime, GasMixture to)
        {
            // TODO: Make the bloodstream separately pump toxins into the lungs, making the lungs' only job to empty.
            if (Body == null)
            {
                return;
            }

            if (!Body.Owner.TryGetComponent(out BloodstreamComponent? bloodstream))
            {
                return;
            }

            bloodstream.PumpToxins(Air);

            var lungRemoved = Air.RemoveRatio(0.5f);
            EntitySystem.Get<AtmosphereSystem>().Merge(to, lungRemoved);
        }
    }

    public enum LungStatus
    {
        None = 0,
        Inhaling,
        Exhaling
    }
}
