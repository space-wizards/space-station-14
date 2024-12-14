namespace Content.Server.Ghost.Roles.Raffles;

/// <summary>
/// Defines settings for a ghost role raffle.
/// </summary>
[DataDefinition]
public sealed partial class GhostRoleRaffleSettings
{
    /// <summary>
    /// The initial duration of a raffle in seconds. This is the countdown timer's value when the raffle starts.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true)]
    public uint InitialDuration { get; set; }

    /// <summary>
    /// When the raffle is joined by a player, the countdown timer is extended by this value in seconds.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true)]
    public uint JoinExtendsDurationBy { get; set; }

    /// <summary>
    /// The maximum duration in seconds for the ghost role raffle. A raffle cannot run for longer than this
    /// duration, even if extended by joiners. Must be greater than or equal to <see cref="InitialDuration"/>.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true)]
    public uint MaxDuration { get; set; }
}
