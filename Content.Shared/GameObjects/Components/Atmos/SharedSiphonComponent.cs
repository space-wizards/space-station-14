#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Atmos
{
    [Serializable, NetSerializable]
    public enum SiphonVisuals
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public class SiphonVisualState
    {
        public readonly bool SiphonEnabled;

        public SiphonVisualState(bool siphonEnabled)
        {
            SiphonEnabled = siphonEnabled;
        }
    }
}
