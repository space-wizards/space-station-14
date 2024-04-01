using Robust.Shared.Prototypes;

namespace Content.Server.Ghost.Roles.Raffles;

/// <summary>
/// Allows specifying the settings for a ghost role raffle as a prototype.
/// </summary>
[Prototype("ghostRoleRaffleSettings")]
public sealed class GhostRoleRaffleSettingsPrototype : IPrototype
{
    /// <inheritdoc />
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The settings for a ghost role raffle.
    /// </summary>
    /// <seealso cref="GhostRoleRaffleSettings"/>
    [DataField("settings", required: true)]
    public GhostRoleRaffleSettings Settings { get; private set; } = new();
}
