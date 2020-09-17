#nullable enable
using System;
using System.Linq;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.Utility;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components.Body.Behavior;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body.Behavior
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedLungBehaviorComponent))]
    public class LungBehaviorComponent : SharedLungBehaviorComponent
    {
        private float _accumulatedFrameTime;

        [ViewVariables] public GasMixture Air { get; set; } = default!;

        [ViewVariables] public override float Temperature => Air.Temperature;

        [ViewVariables] public override float Volume => Air.Volume;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            Air = new GasMixture {Temperature = Atmospherics.NormalBodyTemperature};

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
        }

        public override void Gasp()
        {
            Owner.PopupMessageEveryone("Gasp");
            Inhale(CycleDelay);
        }

        public void Transfer(GasMixture from, GasMixture to, float ratio)
        {
            var removed = from.RemoveRatio(ratio);
            var toOld = to.Gases.ToArray();

            to.Merge(removed);

            for (var gas = 0; gas < Atmospherics.TotalNumberOfGases; gas++)
            {
                var newAmount = to.GetMoles(gas);
                var oldAmount = toOld[gas];
                var delta = newAmount - oldAmount;

                removed.AdjustMoles(gas, -delta);
            }

            from.Merge(removed);
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

        public override void Inhale(float frameTime)
        {
            if (!Owner.Transform.Coordinates.TryGetTileAir(out var tileAir))
            {
                return;
            }

            Inhale(frameTime, tileAir);
        }

        public void Inhale(float frameTime, GasMixture from)
        {
            var ratio = Atmospherics.BreathPercentage * frameTime;

            Transfer(from, Air, ratio);
            ToBloodstream(Air);
        }

        public override void Exhale(float frameTime)
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
            var toOld = to.Gases.ToArray();

            to.Merge(lungRemoved);

            for (var gas = 0; gas < Atmospherics.TotalNumberOfGases; gas++)
            {
                var newAmount = to.GetMoles(gas);
                var oldAmount = toOld[gas];
                var delta = newAmount - oldAmount;

                lungRemoved.AdjustMoles(gas, -delta);
            }

            Air.Merge(lungRemoved);
        }
    }
}
