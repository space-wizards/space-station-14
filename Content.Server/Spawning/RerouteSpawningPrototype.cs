using System.ComponentModel.DataAnnotations;
using Content.Server.Maps;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Spawning;

/// <summary>
/// This prototype defines where/how a player will spawn under RerouteSpawningRule.
/// </summary>
[Prototype]
public sealed class RerouteSpawningPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    // TODO name and description for the lobby?

    /// <summary>
    /// The ProtoID of the GameMapPrototype that will be created for the player.
    /// </summary>
    [DataField, Required]
    public ProtoId<GameMapPrototype> Map;

    // TODO gear override?

    /// <summary>
    /// The protoID of the JobPrototype that will be assigned to the player.
    /// The gear assigned to this job will also be equipped
    /// </summary>
    /// <remarks> Do not use this to give "real" jobs, this system ignores job bans!
    /// Suggested use is to apply Null, Passenger or a Tutorial job
    /// </remarks>
    [DataField]
    public ProtoId<JobPrototype>? Job;

    // TODO requirements/whitelist/blacklist for this option to show up in the Lobby UI?
}
