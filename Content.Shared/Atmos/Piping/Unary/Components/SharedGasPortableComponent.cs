using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Unary.Components
{
    /// <summary>
    /// Used in <see cref="GasPortableVisualizer"/> to determine which visuals to update.
    /// </summary>
    [Serializable, NetSerializable]
        public enum GasPortableVisuals
        {
            ConnectedState,
        }
}
