using System;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible
{
    [Flags, FlagsFor(typeof(ActsFlags))]
    [Serializable]
    public enum ThresholdActs
    {
        Breakage,
        Destruction
    }
}
