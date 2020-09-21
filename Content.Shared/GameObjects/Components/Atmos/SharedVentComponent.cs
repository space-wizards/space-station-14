using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Atmos
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
