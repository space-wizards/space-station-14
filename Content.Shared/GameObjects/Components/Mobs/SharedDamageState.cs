using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs
{
    [Serializable, NetSerializable]
    public enum DamageStateVisuals
    {
        State
    }
    
    [Serializable, NetSerializable]
    public enum DamageStateVisualData
    {
        Normal,
        Crit,
        Dead
    }
}