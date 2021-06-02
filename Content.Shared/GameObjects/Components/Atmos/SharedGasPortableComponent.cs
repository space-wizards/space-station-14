using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Atmos
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
