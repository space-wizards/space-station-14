using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

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
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("settings",
        required: true,
        customTypeSerializer: typeof(PrototypeIdSerializer<GhostRoleRaffleSettingsPrototype>))]
    public string Settings { get; set; } = "default";

    /// <summary>
    /// If not null, the settings from <see cref="Settings"/> are ignored and these settings are used instead.
    /// Intended for allowing admins to set custom raffle settings for admeme ghost roles.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public GhostRoleRaffleSettings? SettingsOverride { get; set; }

    /// <summary>
    /// Sets which <see cref="IGhostRoleRaffleDecider"/> is used.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("decider",
        customTypeSerializer: typeof(PrototypeIdSerializer<GhostRoleRaffleDeciderPrototype>))]
    public string Decider { get; set; } = "default";
}
