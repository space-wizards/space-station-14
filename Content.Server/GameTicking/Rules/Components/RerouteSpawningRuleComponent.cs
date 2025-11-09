using System.ComponentModel.DataAnnotations;
using Content.Server.Maps;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// When this gamerule is active, spawning players will be rerouted to their own personal maps.
/// </summary>
[RegisterComponent]
public sealed partial class RerouteSpawningRuleComponent : Component
{
    /// <summary>
    /// The map that will be created for each player.
    /// </summary>
    [DataField, Required]
    public ProtoId<GameMapPrototype> Map;

    /// <summary>
    /// The gear that will be equipped on the player.
    /// If there are any JobPrototypes that use this StartingGear, their Loadouts will also be equipped
    /// </summary>
    [DataField, Required]
    public ProtoId<StartingGearPrototype> Gear;

    /// <summary>
    /// The job that will be assigned to the player.
    /// </summary>
    /// <remarks> Do not use this to give "real" jobs, this system ignores job bans!
    /// Suggested use is to apply the Tutorial job, or perhaps Passenger
    /// </remarks>
    [DataField, Required]
    public ProtoId<JobPrototype>? Job;

    //TODO Filter who will be targeted. For now, targeting every player is fine.
}

public enum RerouteType : byte
{
    Solo = 0,
}
