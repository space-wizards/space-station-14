using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Dynamic;

/// <summary>
///     A single "dynamic candidate".
/// </summary>
/// <param name="Entity">The entity corresponding to the candidate.</param>
/// <param name="Mind">The candidate's mind.</param>
public record Candidate(
    EntityUid Entity,
    Mind.Mind Mind
);

/// <summary>
///     Data passed into event conditions and effects.
/// </summary>
/// <param name="PlayerCount">The total number of players in the round.</param>
/// <param name="Candidates">A list of possible candidates.</param>
public record GameEventData(
    int PlayerCount,
    HashSet<Candidate> Candidates
);

/// <summary>
///     The various ways that a dynamic event can occur.
/// </summary>
[Flags, FlagsFor(typeof(DynamicEventType))]
public enum DynamicEventTypeEnum
{
    Roundstart = 0,     // 0
    Midround = 1 << 0,  // 1
    Latejoin = 1 << 1   // 2
}

public sealed class DynamicEventType {}
