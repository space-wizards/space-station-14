using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    [Serializable, NetSerializable]
    public enum SolutionContainerVisuals : byte
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public class SolutionContainerVisualState
    {
        public readonly Color Color;

        /// <summary>
        ///     Represents how full the container is, as a fraction equivalent to <see cref="FilledVolumeFraction"/>/<see cref="byte.MaxValue"/>.
        /// </summary>
        public readonly byte FilledVolumeFraction;

        // do we really need this just to save three bytes?
        public float FilledVolumePercent => (float) FilledVolumeFraction / byte.MaxValue;

        /// <summary>
        ///     Sets the solution state of a container.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="filledVolumeFraction">The fraction of the container's volume that is filled.</param>
        public SolutionContainerVisualState(Color color, float filledVolumeFraction)
        {
            Color = color;
            FilledVolumeFraction = (byte) (byte.MaxValue * filledVolumeFraction);
        }
    }

    public enum SolutionContainerLayers : byte
    {
        Fill,
        Base
    }
}
