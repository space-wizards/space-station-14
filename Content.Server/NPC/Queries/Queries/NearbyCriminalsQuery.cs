using System.Collections.Generic;
using Content.Shared.Security;
using Robust.Shared.Serialization;

namespace Content.Server.NPC.Queries.Queries;

/// <summary>
/// Returns nearby entities that have criminal records matching a set of security statuses.
/// </summary>
public sealed partial class NearbyCriminalsQuery : UtilityQuery
{
    /// <summary>
    /// Security statuses that should be considered valid targets.
    /// </summary>
    [DataField]
    public HashSet<SecurityStatus> Statuses { get; private set; } = new()
    {
        SecurityStatus.Wanted
    };
}
