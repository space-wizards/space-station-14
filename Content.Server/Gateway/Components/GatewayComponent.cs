using Content.Server.Gateway.Systems;
using Robust.Shared.Audio;

namespace Content.Server.Gateway.Components;

/// <summary>
/// Controlling gateway that links to other gateway destinations on the server.
/// </summary>
[RegisterComponent, Access(typeof(GatewaySystem))]
public sealed class GatewayComponent : Component
{
    /// <summary>
    /// Sound to play when opening or closing the portal.
    /// </summary>
    [DataField("portalSound")]
    public SoundSpecifier PortalSound = new SoundPathSpecifier("/Audio/Effects/Lightning/lightningbolt.ogg");

    /// <summary>
    /// Every other gateway destination on the server.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> Destinations = new();

    /// <summary>
    /// The time at which the portal will be closed.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextClose;

    /// <summary>
    /// The time at which the portal was last opened.
    /// Only used for UI.
    /// </summary>
    [ViewVariables]
    public TimeSpan LastOpen;
}
