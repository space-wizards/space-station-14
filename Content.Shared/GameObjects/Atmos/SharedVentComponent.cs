using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Atmos
{
    [Serializable, NetSerializable]
    public enum VentVisuals
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public class VentVisualState
    {
        public readonly bool VentEnabled;

        public VentVisualState(bool ventEnabled)
        {
            VentEnabled = ventEnabled;
        }
    }
}
