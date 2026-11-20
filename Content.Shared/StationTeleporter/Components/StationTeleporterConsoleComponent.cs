using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.StationTeleporter.Components;

/// <summary>
/// Console that allows you to manage <see cref="StationTeleporterComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedStationTeleporterSystem))]
public sealed partial class StationTeleporterConsoleComponent : Component
{
    /// <summary>
    /// When initialized, teleporters can automatically generate chips in this console if they have a matching AutoLinkKey.
    /// </summary>
    [DataField]
    public string? AutoLinkKey;

    /// <summary>
    /// These are the chips that will appear in the console and automatically communicate with the teleporters.
    /// </summary>
    [DataField]
    public EntProtoId? AutoLinkChipsProto = "TeleporterChipBlank";

    /// <summary>
    /// Selected via UI gate. Defines the behavior of the console.
    /// </summary>
    [DataField]
    public EntityUid? SelectedTeleporter;

    /// <summary>
    /// Portals created by this console will be colored in the specified color. This can be used to make Syndicate portals blood red.
    /// </summary>
    [DataField]
    public Color PortalColor = Color.White;

    /// <summary>
    /// The storage container from which all coordinate chips are scanned.
    /// </summary>
    [DataField]
    public string ChipStorageName = "storagebase";
}
