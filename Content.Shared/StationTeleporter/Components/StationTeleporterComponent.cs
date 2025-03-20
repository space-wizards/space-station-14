using Robust.Shared.Audio;

namespace Content.Shared.StationTeleporter.Components;

/// <summary>
/// Allows an entity to be displayed and managed using StationTeleporterConsole
/// </summary>
[RegisterComponent]
[Access(typeof(SharedStationTeleporterSystem))]
public sealed partial class StationTeleporterComponent : Component
{
    /// <summary>
    /// When initialized, the chip from this teleporter will be automatically generated inside all consoles with the same AutoLinkKey
    /// </summary>
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
