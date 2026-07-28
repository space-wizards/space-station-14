// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Revolutionary;

/// <summary>
/// Complete revolutionary icon roster sent only to players allowed to see it.
/// </summary>
[Serializable, NetSerializable]
public sealed class RevolutionaryRosterSyncEvent : EntityEventArgs
{
    public Dictionary<NetEntity, ProtoId<FactionIconPrototype>> Revolutionaries { get; }
    public Dictionary<NetEntity, ProtoId<FactionIconPrototype>> HeadRevolutionaries { get; }

    public RevolutionaryRosterSyncEvent(
        Dictionary<NetEntity, ProtoId<FactionIconPrototype>> revolutionaries,
        Dictionary<NetEntity, ProtoId<FactionIconPrototype>> headRevolutionaries)
    {
        Revolutionaries = revolutionaries;
        HeadRevolutionaries = headRevolutionaries;
    }
}

/// <summary>
/// Batched incremental update for clients that already have a revolutionary icon roster.
/// </summary>
[Serializable, NetSerializable]
public sealed class RevolutionaryRosterDeltaEvent : EntityEventArgs
{
    public Dictionary<NetEntity, ProtoId<FactionIconPrototype>> AddedRevolutionaries { get; }
    public NetEntity[] RemovedRevolutionaries { get; }
    public Dictionary<NetEntity, ProtoId<FactionIconPrototype>> AddedHeadRevolutionaries { get; }
    public NetEntity[] RemovedHeadRevolutionaries { get; }

    public RevolutionaryRosterDeltaEvent(
        Dictionary<NetEntity, ProtoId<FactionIconPrototype>> addedRevolutionaries,
        NetEntity[] removedRevolutionaries,
        Dictionary<NetEntity, ProtoId<FactionIconPrototype>> addedHeadRevolutionaries,
        NetEntity[] removedHeadRevolutionaries)
    {
        AddedRevolutionaries = addedRevolutionaries;
        RemovedRevolutionaries = removedRevolutionaries;
        AddedHeadRevolutionaries = addedHeadRevolutionaries;
        RemovedHeadRevolutionaries = removedHeadRevolutionaries;
    }
}

/// <summary>
/// Clears a roster when a player is no longer allowed to see revolutionary icons.
/// </summary>
[Serializable, NetSerializable]
public sealed class RevolutionaryRosterClearEvent : EntityEventArgs;
