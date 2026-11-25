using Content.Shared.Ghost.Roles.Raffles;
using Robust.Shared.Prototypes;

namespace Content.Server.Ghost.Roles.Raffles;

/// <summary>
/// Raffle configuration.
/// </summary>
[DataDefinition]
public sealed partial class GhostRoleRaffleConfig
{
    public GhostRoleRaffleConfig(GhostRoleRaffleSettings settings)
    {
        SettingsOverride = settings;
    }

    /// <summary>
    /// Specifies the raffle settings to use.
    /// </summary>
    [DataField("settings", required: true)]
    public ProtoId<GhostRoleRaffleSettingsPrototype> Settings { get; set; } = "default";

    /// <summary>
    /// If not null, the settings from <see cref="Settings"/> are ignored and these settings are used instead.
    /// Intended for allowing admins to set custom raffle settings for admeme ghost roles.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public GhostRoleRaffleSettings? SettingsOverride { get; set; }

    /// <summary>
    /// Sets which <see cref="IGhostRoleRaffleDecider"/> is used.
    /// </summary>
    [DataField("decider")]
    public ProtoId<GhostRoleRaffleDeciderPrototype> Decider { get; set; } = "default";
}
