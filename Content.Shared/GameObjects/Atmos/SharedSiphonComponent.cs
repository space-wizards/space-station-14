using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Atmos
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
