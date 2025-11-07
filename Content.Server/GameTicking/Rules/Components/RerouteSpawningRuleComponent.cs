using System.ComponentModel.DataAnnotations;
using Content.Server.Maps;
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

    //TODO Filter who will be targeted. For now, targeting every player is fine.

    //TODO: Specify a loadout/gear for the player.
}

public enum RerouteType : byte
{
    Solo = 0,
}
