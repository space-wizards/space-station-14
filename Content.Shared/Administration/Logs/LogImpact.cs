using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Logs;

// DO NOT CHANGE THE NUMERIC VALUES OF THESE
[Serializable, NetSerializable]
public enum LogImpact : sbyte
{
    Low = -1,
    Medium = 0,
    High = 1,
    Extreme = 2 // Nar'Sie just dropped
}
