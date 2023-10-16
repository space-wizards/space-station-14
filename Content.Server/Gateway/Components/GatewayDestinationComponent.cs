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
    /// When the destination will unlock if applicable.
    /// </summary>
    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan? UnlockTime;

    /// <summary>
    /// Name as it shows up on the ui of station gateways.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FormattedMessage Name = new();

    /// <summary>
    /// How long the portal will be open for after linking.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan OpenTime = TimeSpan.FromSeconds(600);

    /// <summary>
    /// If true, the portal can be closed by alt clicking it.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Closeable;
}
