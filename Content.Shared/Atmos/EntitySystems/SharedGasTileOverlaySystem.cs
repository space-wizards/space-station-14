using Content.Shared.Atmos.Components;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace Content.Shared.Atmos.EntitySystems
{
    public abstract class SharedGasTileOverlaySystem : EntitySystem
    {
        public const byte ChunkSize = 8;
        protected float AccumulatedFrameTime;
        protected bool PvsEnabled;

        [Dependency] protected readonly IPrototypeManager ProtoMan = default!;
        [Dependency] protected readonly IConfigurationManager ConfMan = default!;
        [Dependency] private readonly SharedAtmosphereSystem _atmosphere = default!;

        public int[] VisibleGasId = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GasTileOverlayComponent, ComponentGetState>(OnGetState);

            List<int> visibleGases = new();
            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gasPrototype = _atmosphere.GetGas(i);
                if (!string.IsNullOrEmpty(gasPrototype.GasOverlayTexture) ||
                    (!string.IsNullOrEmpty(gasPrototype.GasOverlaySprite) && !string.IsNullOrEmpty(gasPrototype.GasOverlayState)))
                    visibleGases.Add(i);
            }
            VisibleGasId = visibleGases.ToArray();
        }

        // --- NEW LOGIC START ---

        /// <summary>
        /// Check if the temperature change is significant enough to send an update.
        /// </summary>
        public bool CheckTemperatureTolerance(float tempA, float tempB, float tolerance)
        {
            // 1. If the difference is huge (e.g. > 5 degrees), always update.
            // This catches fast heating/cooling.
            if (Math.Abs(tempA - tempB) > 5.0f)
                return true;

            // 2. Critical Visual Thresholds (Kelvin)
            // These match the color breakpoints in your shader/overlay.
            // -150C (123K), -50C (223K), 0C (273K), 50C (323K), 100C (373K), 300C (573K)
            // We check if the temp crossed any of these lines.
            if (CrossesThreshold(tempA, tempB, 123.15f)) return true;
            if (CrossesThreshold(tempA, tempB, 223.15f)) return true;
            if (CrossesThreshold(tempA, tempB, 273.15f)) return true; // Freezing Point
            if (CrossesThreshold(tempA, tempB, 323.15f)) return true; // Safe/Heat boundary
            if (CrossesThreshold(tempA, tempB, 373.15f)) return true; // Boiling Point
            if (CrossesThreshold(tempA, tempB, 573.15f)) return true; // Fire Point

            // 3. Otherwise, use the standard strict tolerance (0.5f is a good balance)
            return Math.Abs(tempA - tempB) > tolerance;
        }

        // Helper: returns true if 'val' crosses 'threshold' compared to 'oldVal'
        private bool CrossesThreshold(float val1, float val2, float threshold)
        {
            return (val1 < threshold && val2 >= threshold) ||
                   (val1 >= threshold && val2 < threshold);
        }
        // --- NEW LOGIC END ---

        private void OnGetState(EntityUid uid, GasTileOverlayComponent component, ref ComponentGetState args)
        {
            if (PvsEnabled && !args.ReplayState) return;

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
            return new((int)MathF.Floor((float)indices.X / ChunkSize), (int)MathF.Floor((float)indices.Y / ChunkSize));
        }

        [Serializable, NetSerializable]
        public readonly struct GasOverlayData : IEquatable<GasOverlayData>
        {
            [ViewVariables] public readonly byte FireState;
            [ViewVariables] public readonly byte[] Opacity;
            [ViewVariables] public readonly float Temperature;

            public GasOverlayData(byte fireState, byte[] opacity, float temperature)
            {
                FireState = fireState;
                Opacity = opacity;
                Temperature = temperature;
            }

            public bool Equals(GasOverlayData other)
            {
                if (FireState != other.FireState) return false;
                if (Opacity?.Length != other.Opacity?.Length) return false;

                if (Opacity != null && other.Opacity != null)
                {
                    for (var i = 0; i < Opacity.Length; i++)
                    {
                        if (Opacity[i] != other.Opacity[i]) return false;
                    }
                }

                // Use a reasonable tolerance for standard equality checks (e.g. 0.5 degrees)
                // The "Critical Threshold" check logic is usually handled in the System 
                // that decides *when* to dirty the chunk, but having a base tolerance here prevents spam.
                if (Math.Abs(Temperature - other.Temperature) > 0.5f)
                    return false;

                return true;
            }
        }

        [Serializable, NetSerializable]
        public sealed class GasOverlayUpdateEvent : EntityEventArgs
        {
            public Dictionary<NetEntity, List<GasOverlayChunk>> UpdatedChunks = new();
            public Dictionary<NetEntity, HashSet<Vector2i>> RemovedChunks = new();
        }
    }
}
