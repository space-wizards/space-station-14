using Content.Server.Ghost.Roles.Raffles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ghost.Roles.Raffles;

/// <summary>
/// Allows specifying the settings for a ghost role raffle as a prototype.
/// </summary>
[Prototype]
public sealed partial class GhostRoleRaffleSettingsPrototype : IPrototype
{
    /// <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The settings for a ghost role raffle.
    /// </summary>
    /// <seealso cref="GhostRoleRaffleSettings"/>
    [DataField(required: true)]
    public GhostRoleRaffleSettings Settings { get; private set; } = new();
}
