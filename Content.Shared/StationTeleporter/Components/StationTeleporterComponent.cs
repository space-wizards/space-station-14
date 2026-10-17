using Robust.Shared.Audio;

namespace Content.Shared.StationTeleporter.Components;

/// <summary>
/// Allows an entity to be displayed and managed using <see cref="StationTeleporterConsoleComponent"/>.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedStationTeleporterSystem))]
public sealed partial class StationTeleporterComponent : Component
{
    /// <summary>
    /// When initialized, the chip from this teleporter will be automatically generated inside all consoles with the same AutoLinkKey
    /// </summary>
    [DataField]
    public string? AutoLinkKey;

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
    public EntityUid? LastLink;

    /// <summary>
    /// Used for coloring from AppearanceChanged
    /// </summary>
    [DataField]
    public string? PortalLayerMap;

    /// <summary>
    /// Color applied to the portal effect once this teleporter is linked. Set by the console that links it
    /// (e.g. Syndicate consoles use a red tint) and picked up by <see cref="SharedStationTeleporterSystem"/> when the link succeeds.
    /// </summary>
    [DataField]
    public Color PortalColor = Color.White;
}
