using System.ComponentModel.DataAnnotations;
using Content.Server.Maps;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Prototypes;

/// <summary>
/// This prototype defines details for a spawn option for SolitarySpawningRule.
/// </summary>
[Prototype]
public sealed class SolitarySpawningPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The label of the button on the Join GUI
    /// </summary>
    [DataField]
    public LocId Name = string.Empty;

    /// <summary>
    /// The tooltip of the button on the Join GUI
    /// </summary>
    [DataField]
    public LocId Description = string.Empty;

    /// <summary>
    /// This message is sent to each player when they spawn
    /// </summary>
    [DataField]
    public LocId? WelcomeLoc;

    /// <summary>
    /// The station that will be created for the player.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<GameMapPrototype> Map;

    // TODO gear override?

    /// <summary>
    /// The job that will be assigned to the player.
    /// The starting gear and loadout specified on this job prototype will also be equipped
    /// </summary>
    /// <remarks>
    /// SolitarySpawning ignores job bans, but real jobs should in general not be used with the system.
    /// We don't want players trying to pad their role times using the tutorial.
    /// Recommended that all tutorials use the 'Tutorial' job, and place any necessary equipment on the map itself.
    /// </remarks>
    [DataField]
    public ProtoId<JobPrototype> Job = "Trainee";

    // TODO requirements/whitelist/blacklist for this option to show up in the Lobby UI?
}
