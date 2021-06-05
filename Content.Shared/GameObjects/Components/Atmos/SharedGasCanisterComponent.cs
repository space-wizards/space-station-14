#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Atmos
{
    /// <summary>
    /// Key representing which <see cref="BoundUserInterface"/> is currently open.
    /// Useful when there are multiple UI for an object. Here it's future-proofing only.
    /// </summary>
    [Serializable, NetSerializable]
    public enum GasCanisterUiKey
    {
        Key,
    }

    #region Enums

    /// <summary>
    /// Used in <see cref="GasCanisterVisualizer"/> to determine which visuals to update.
    /// </summary>
    [Serializable, NetSerializable]
    public enum GasCanisterVisuals
    {
        PressureState,
        TankInserted,
    }

    #endregion

    /// <summary>
    /// Represents a <see cref="GasCanisterComponent"/> state that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public class GasCanisterBoundUserInterfaceState : BoundUserInterfaceState
    {
        public GasCanisterBoundUserInterfaceState(string newLabel, float volume, float releasePressure, bool valveOpened)
        {}
    }

    #region NetMessages



    #endregion
}
