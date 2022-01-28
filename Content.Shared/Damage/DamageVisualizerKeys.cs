using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage
{
    [Serializable, NetSerializable]
    public enum DamageVisualizerKeys
    {
        Disabled,
        DamageSpecifierDelta,
        DamageUpdateGroups,
        ForceUpdate
    }
}
