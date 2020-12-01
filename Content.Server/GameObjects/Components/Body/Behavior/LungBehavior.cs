#nullable enable
using System;
using System.Linq;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.GameObjects.Components.Body.Respiratory;
using Content.Server.Utility;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components.Body.Behavior;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body.Behavior
{
    public class LungBehavior : MechanismBehavior
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private float _accumulatedFrameTime;

        [ViewVariables] private TimeSpan _lastGaspPopupTime;

        [ViewVariables] public GasMixture Air { get; set; } = default!;

        [ViewVariables] public float Temperature => Air.Temperature;

        [ViewVariables] public float Volume => Air.Volume;

        [ViewVariables] public TimeSpan GaspPopupCooldown { get; private set; }

        [ViewVariables] public LungStatus Status { get; set; }

        [ViewVariables] public float CycleDelay { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            Air = new GasMixture {Temperature = Atmospherics.NormalBodyTemperature};

            serializer.DataField(this, l => l.CycleDelay, "cycleDelay", 2);

            serializer.DataReadWriteFunction(
                "volume",
                6,
                vol => Air.Volume = vol,
                () => Air.Volume);

            serializer.DataReadWriteFunction(
                "temperature",
                Atmospherics.NormalBodyTemperature,
                temp => Air.Temperature = temp,
                () => Air.Temperature);

            serializer.DataReadWriteFunction(
                "gaspPopupCooldown",
                8f,
                delay => GaspPopupCooldown = TimeSpan.FromSeconds(delay),
                () => GaspPopupCooldown.TotalSeconds);
        }

        public void Gasp()
        {
            if (_gameTiming.CurTime >= _lastGaspPopupTime + GaspPopupCooldown)
            {
                _lastGaspPopupTime = _gameTiming.CurTime;
                Owner.PopupMessageEveryone(Loc.GetString("Gasp"));
            }

            Inhale(CycleDelay);
        }

        public void Transfer(GasMixture from, GasMixture to, float ratio)
        {
            to.Merge(from.RemoveRatio(ratio));
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

            to.Merge(mixture);
            mixture.Clear();
        }

        public override void Update(float frameTime)
        {
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
            if (Body != null && Body.Owner.TryGetComponent(out InternalsComponent? internals)
                             && internals.BreathToolEntity != null && internals.GasTankEntity != null
                             && internals.BreathToolEntity.TryGetComponent(out BreathToolComponent? breathTool)
                             && breathTool.IsFunctional && internals.GasTankEntity.TryGetComponent(out GasTankComponent? gasTank)
                             && gasTank.Air != null)
            {
                Inhale(frameTime, gasTank.RemoveAirVolume(Atmospherics.BreathVolume));
                return;
            }

            if (!Owner.Transform.Coordinates.TryGetTileAir(out var tileAir))
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
            if (!Owner.Transform.Coordinates.TryGetTileAir(out var tileAir))
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
            to.Merge(lungRemoved);
        }
    }

    public enum LungStatus
    {
        None = 0,
        Inhaling,
        Exhaling
    }
}
