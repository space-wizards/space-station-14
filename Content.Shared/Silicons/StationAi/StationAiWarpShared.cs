using System;
using System.Collections.Generic;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

using Content.Shared.Actions;

namespace Content.Shared.Silicons.StationAi;

public sealed partial class StationAiOpenWarpActionEvent : InstantActionEvent
{
}


[Serializable, NetSerializable]
public sealed partial class StationAiWarpRequestEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class StationAiWarpTargetsEvent : EntityEventArgs
{
    public List<StationAiWarpTarget> Targets { get; }

    public StationAiWarpTargetsEvent(List<StationAiWarpTarget> targets)
    {
        Targets = targets;
    }
}

[Serializable, NetSerializable]
public readonly record struct StationAiWarpTarget(NetEntity Target, string DisplayName, StationAiWarpTargetType Type);

[Serializable, NetSerializable]
public enum StationAiWarpTargetType : byte
{
    Crew,
    Location
}

[Serializable, NetSerializable]
public sealed partial class StationAiWarpToTargetEvent : EntityEventArgs
{
    public StationAiWarpToTargetEvent(NetEntity target)
    {
        Target = target;
    }

    public NetEntity Target { get; }
}
