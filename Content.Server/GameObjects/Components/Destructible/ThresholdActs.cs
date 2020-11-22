using System;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible
{
    [Flags, FlagsFor(typeof(ActsFlags))]
    [Serializable]
    public enum ThresholdActs
    {
        Invalid = 0,
        Breakage,
        Destruction
    }
}
