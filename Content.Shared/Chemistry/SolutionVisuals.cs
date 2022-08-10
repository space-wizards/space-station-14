using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    [Serializable, NetSerializable]
    public enum SolutionContainerVisuals : byte
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public sealed class SolutionContainerVisualState : ICloneable
    {
        public readonly Color Color;

        public readonly byte FilledVolumeFraction;

        // do we really need this just to save three bytes?
        // This does seem silly
        public float FilledVolumePercent => (float) FilledVolumeFraction / byte.MaxValue;

        /// <summary>
        ///     Sets the solution state of a container.
        /// </summary>
        public SolutionContainerVisualState(Color color, float filledVolumePercent)
        {
            Color = color;
            FilledVolumeFraction = (byte) (byte.MaxValue * filledVolumePercent);
        }

        public SolutionContainerVisualState(Color color, byte filledVolumeFraction)
        {
            Color = color;
            FilledVolumeFraction = filledVolumeFraction;
        }

        public object Clone()
        {
            return new SolutionContainerVisualState(Color, FilledVolumeFraction);
        }
    }

    public enum SolutionContainerLayers : byte
    {
        Fill,
        Base
    }
}
