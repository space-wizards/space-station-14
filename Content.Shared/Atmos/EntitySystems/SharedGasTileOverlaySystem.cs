using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Prototypes;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.EntitySystems
{
    public abstract class SharedGasTileOverlaySystem : EntitySystem
    {
        /// <summary>
        /// The temperature at which the heat distortion effect starts to be applied.
        /// </summary>
        private float _tempAtMinHeatDistortion;
        /// <summary>
        /// The temperature at which the heat distortion effect is at maximum strength.
        /// </summary>
        private float _tempAtMaxHeatDistortion;
        /// <summary>
        /// Calculated linear slope and intercept to map temperature to a heat distortion strength from 0.0 to 1.0
        /// </summary>
        private float _heatDistortionSlope;
        private float _heatDistortionIntercept;

        public const byte ChunkSize = 8;
        protected float AccumulatedFrameTime;
        protected bool PvsEnabled;

        [Dependency] protected readonly IPrototypeManager ProtoMan = default!;
        [Dependency] protected readonly IConfigurationManager ConfMan = default!;

        /// <summary>
        ///     array of the ids of all visible gases.
        /// </summary>
        public int[] VisibleGasId = default!;

        public override void Initialize()
        {
            base.Initialize();

            // Make sure the heat distortion variables are updated if the CVars change
            Subs.CVar(ConfMan, CCVars.GasOverlayHeatMinimum, UpdateMinHeat, true);
            Subs.CVar(ConfMan, CCVars.GasOverlayHeatMaximum, UpdateMaxHeat, true);

            SubscribeLocalEvent<GasTileOverlayComponent, ComponentGetState>(OnGetState);

            List<int> visibleGases = new();

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gasPrototype = ProtoMan.Index<GasPrototype>(i.ToString());
                if (!string.IsNullOrEmpty(gasPrototype.GasOverlayTexture) || !string.IsNullOrEmpty(gasPrototype.GasOverlaySprite) && !string.IsNullOrEmpty(gasPrototype.GasOverlayState))
                    visibleGases.Add(i);
            }

            VisibleGasId = visibleGases.ToArray();
        }

        private void UpdateMaxHeat(float val)
        {
            _tempAtMaxHeatDistortion = val;
            UpdateHeatSlopeAndIntercept();
        }

        private void UpdateMinHeat(float val)
        {
            _tempAtMinHeatDistortion = val;
            UpdateHeatSlopeAndIntercept();
        }

        private void UpdateHeatSlopeAndIntercept()
        {
            // Make sure to avoid invalid settings (min == max or min > max)
            // I'm not sure if CVars can have constraints or if CVar subscribers can reject changes.
            var diff = _tempAtMinHeatDistortion < _tempAtMaxHeatDistortion
                ? _tempAtMaxHeatDistortion - _tempAtMinHeatDistortion
                : 0.001f;
            _heatDistortionSlope = 1.0f / diff;
            _heatDistortionIntercept = -_tempAtMinHeatDistortion * _heatDistortionSlope;
        }

        private void OnGetState(EntityUid uid, GasTileOverlayComponent component, ref ComponentGetState args)
        {
            if (PvsEnabled && !args.ReplayState)
                return;

            // Should this be a full component state or a delta-state?
            if (args.FromTick <= component.CreationTick || args.FromTick <= component.ForceTick)
            {
                args.State = new GasTileOverlayState(component.Chunks);
                return;
            }

            var data = new Dictionary<Vector2i, GasOverlayChunk>();
            foreach (var (index, chunk) in component.Chunks)
            {
                if (chunk.LastUpdate >= args.FromTick)
                    data[index] = chunk;
            }

            args.State = new GasTileOverlayDeltaState(data, new(component.Chunks.Keys));
        }

        public static Vector2i GetGasChunkIndices(Vector2i indices)
        {
            return new((int) MathF.Floor((float) indices.X / ChunkSize), (int) MathF.Floor((float) indices.Y / ChunkSize));
        }

        [Serializable, NetSerializable]
        public readonly struct GasOverlayData : IEquatable<GasOverlayData>
        {
            [ViewVariables]
            public readonly byte FireState;

            [ViewVariables]
            public readonly byte[] Opacity;

            /// <summary>
            /// This temperature is currently only used by the GasTileHeatOverlay.
            /// This value will only reflect the true temperature of the gas when the temperature is between
            /// <see cref="SharedGasTileOverlaySystem._tempAtMinHeatDistortion"/> and <see cref="SharedGasTileOverlaySystem._tempAtMaxHeatDistortion"/> as these are the only
            /// values at which the heat distortion varies.
            /// Additionally, it will only update when the heat distortion strength changes by
            /// <see cref="_heatDistortionStrengthChangeTolerance"/>. By default, this is 5%, which corresponds to
            /// 20 steps from <see cref="SharedGasTileOverlaySystem._tempAtMinHeatDistortion"/> to <see cref="SharedGasTileOverlaySystem._tempAtMaxHeatDistortion"/>.
            /// For 325K to 1000K with 5% tolerance, then this field will dirty only if it differs by 33.75K, or 20 steps.
            /// </summary>
            [ViewVariables]
            public readonly float Temperature;

            // TODO change fire color based on temps

            public GasOverlayData(byte fireState, byte[] opacity, float temperature)
            {
                FireState = fireState;
                Opacity = opacity;
                Temperature = temperature;
            }

            public bool Equals(GasOverlayData other)
            {
                if (FireState != other.FireState)
                    return false;

                if (Opacity?.Length != other.Opacity?.Length)
                    return false;

                if (Opacity != null && other.Opacity != null)
                {
                    for (var i = 0; i < Opacity.Length; i++)
                    {
                        if (Opacity[i] != other.Opacity[i])
                            return false;
                    }
                }

                // This is only checking if two datas are equal -- a different routine is used to check if the
                // temperature differs enough to dirty the chunk using a much wider tolerance.
                if (!MathHelper.CloseToPercent(Temperature, other.Temperature))
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Calculate the heat distortion from a temperature.
        /// Returns 0.0f below TempAtMinHeatDistortion and 1.0f above TempAtMaxHeatDistortion.
        /// </summary>
        /// <param name="temp"></param>
        /// <returns></returns>
        public float GetHeatDistortionStrength(float temp)
        {
            return MathHelper.Clamp01(temp * _heatDistortionSlope + _heatDistortionIntercept);
        }

        [Serializable, NetSerializable]
        public sealed class GasOverlayUpdateEvent : EntityEventArgs
        {
            public Dictionary<NetEntity, List<GasOverlayChunk>> UpdatedChunks = new();
            public Dictionary<NetEntity, HashSet<Vector2i>> RemovedChunks = new();
        }
    }
}
