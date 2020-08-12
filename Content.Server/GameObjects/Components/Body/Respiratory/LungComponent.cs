using System;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body.Respiratory
{
    [RegisterComponent]
    public class LungComponent : Component, IGasMixtureHolder
    {
        public override string Name => "Lung";

        private float _accumulatedFrameTime;

        /// <summary>
        ///     The pressure that this lung exerts on the air around it
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] private float Pressure { get; set; }

        [ViewVariables] public GasMixture Air { get; set; } = new GasMixture();

        [ViewVariables] public LungStatus Status { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "volume",
                6,
                vol => Air.Volume = vol,
                () => Air.Volume);
            serializer.DataField(this, l => l.Pressure, "pressure", 100);
        }

        public void Update(float frameTime)
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

            if (Math.Abs(_accumulatedFrameTime) < 1)
            {
                return;
            }

            _accumulatedFrameTime -= 1;

            switch (Status)
            {
                case LungStatus.Inhaling:
                    Inhale(frameTime);
                    Status = LungStatus.Exhaling;
                    break;
                case LungStatus.Exhaling:
                    Exhale(frameTime);
                    Status = LungStatus.Inhaling;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Inhale(float frameTime)
        {
            if (!Owner.TryGetComponent(out BloodstreamComponent bloodstream))
            {
                return;
            }

            if (!Owner.Transform.GridPosition.TryGetTileAir(out var air))
            {
                return;
            }

            air.PumpGasTo(Air, Pressure);
            Air.PumpGasTo(bloodstream.Air, Pressure);
        }

        public void Exhale(float frameTime)
        {
            if (!Owner.TryGetComponent(out BloodstreamComponent bloodstream))
            {
                return;
            }

            if (!Owner.Transform.GridPosition.TryGetTileAir(out var air))
            {
                return;
            }

            bloodstream.PumpToxins(Air, Pressure);
            Air.PumpGasTo(air, Pressure);
        }
    }

    public enum LungStatus
    {
        None = 0,
        Inhaling,
        Exhaling
    }
}
