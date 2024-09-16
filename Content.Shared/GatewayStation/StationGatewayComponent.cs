using Robust.Shared.Audio;

namespace Content.Shared.GatewayStation;

/// <summary>
/// Allows an entity to be displayed and managed using StationGatewayConsole
/// </summary>
[RegisterComponent]
[Access(typeof(SharedStationGatewaySystem))]
public sealed partial class StationGatewayComponent : Component
{
    /// <summary>
    /// Public name of the gateway displayed in the UI
    /// </summary>
    [DataField]
    public string GateName = "Unknown Coordinates";

    /// <summary>
    /// When initialized, chip will attempt to contact a random gateway that has the same code. Can be used for pre-created gateways
    /// </summary>
    // Not ProtoId<TagPrototype> because we can random generate this keys for expeditions
    [DataField]
    public string? AutoLinkKey = null;

    /// <summary>
    /// The sound that plays at the portal when it connects to something
    /// </summary>
    [DataField]
    public SoundSpecifier LinkSound = new SoundPathSpecifier("/Audio/Effects/Lightning/lightningbolt.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f),
    };

    /// <summary>
    /// The sound played at the portal when it disconnects
    /// </summary>
    [DataField]
    public SoundSpecifier UnlinkSound = new SoundPathSpecifier("/Audio/Effects/gateway_off.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f),
    };

    /// <summary>
    /// The last link that the portal will attempt to connect to after power up.
    /// </summary>
    [DataField]
    public EntityUid? LastLink = null;

    /// <summary>
    /// Used for coloring from AppearanceChanged
    /// </summary>
    [DataField]
    public string? PortalLayerMap;
}
