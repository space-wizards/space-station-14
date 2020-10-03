using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    [NetSerializable, Serializable]
    public enum ParticleAcceleratorVisuals
    {
        VisualState
    }

    [NetSerializable, Serializable]
    public enum ParticleAcceleratorVisualState
    {
        Open, //no prefix
        Wired, //w prefix
        Closed, //c prefix
        Powered, //p prefix
        Level0, //0 prefix
        Level1, //1 prefix
        Level2, //2 prefix
        Level3 //3 prefix
    }
}
