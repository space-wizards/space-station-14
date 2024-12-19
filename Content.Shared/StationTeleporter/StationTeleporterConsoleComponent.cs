using Robust.Shared.GameStates;

namespace Content.Shared.StationTeleporter;

/// <summary>
/// Console that allows you to manage the StationTeleporterComponent
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(SharedStationTeleporterSystem))]
public sealed partial class StationTeleporterConsoleComponent : Component
{
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
    /// A storage from which all coordinate chips are scanned
    /// </summary>
    [DataField]
    public string ChipStorageName = "storagebase";

    [DataField, AutoPausedField]
    public TimeSpan NextUpdateTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateFrequency = TimeSpan.FromSeconds(1f);

}
