using Content.Server.Gateway.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Server.Gateway.Components;

/// <summary>
/// A gateway destination linked to by station gateway(s).
/// </summary>
[RegisterComponent, Access(typeof(GatewaySystem))]
public sealed partial class GatewayDestinationComponent : Component
{
    /// <summary>
    /// Whether this destination is shown in the gateway ui.
    /// If you are making a gateway for an admeme set this once you are ready for players to select it.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;

    /// <summary>
    /// Generator that made this destination if applicable.
    /// Used to determine whether it can be unlocked or not.
    /// </summary>
    [DataField]
    public EntityUid? Generator;

    /// <summary>
    /// Name as it shows up on the ui of station gateways.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FormattedMessage Name = new();

    /// <summary>
    /// Next time the destination can have its linked portal changed.
    /// </summary>
    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextReady;

    /// <summary>
    /// Cooldown to change configuration.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    /// <summary>
    /// If true, the portal can be closed by alt clicking it.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Closeable;
}
