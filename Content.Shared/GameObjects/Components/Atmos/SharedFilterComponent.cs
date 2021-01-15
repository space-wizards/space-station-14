#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Atmos
{
    [Serializable, NetSerializable]
    public enum FilterVisuals
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public class FilterVisualState
    {
        public bool Enabled { get; }

        public FilterVisualState(bool enabled)
        {
            Enabled = enabled;
        }
    }
}
