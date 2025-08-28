using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.ScanGate.Components;

/// <summary>
/// Marks an entity as a scan gate that can detect entities with <see cref="ScanDetectableComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ScanGateComponent : Component
{
    /// <summary>
    /// The delay between scans.
    /// </summary>
    [DataField("scanDelay"), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The next time the scan gate can perform a scan.
    /// </summary>
    [AutoNetworkedField, AutoPausedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextScanTime = TimeSpan.Zero;

    /// <summary>
    /// The sound played when a scan is performed.
    /// </summary>
    [DataField("scanSound")]
    public SoundSpecifier ScanSound = new SoundCollectionSpecifier("ScanGateScan");

    /// <summary>
    /// The sound played when a scan successfully detects an item.
    /// </summary>
    [DataField("scanFailSound")]
    public SoundSpecifier ScanFailSound = new SoundPathSpecifier("/Audio/_Starlight/Effects/ScanGate/scan_fail.ogg");

    /// <summary>
    /// Sprite state to set on successful scan.
    /// </summary>
    [DataField("scanSuccessState")]
    public string ScanSuccessState = "success";

    /// <summary>
    /// Sprite state to set on failed scan.
    /// </summary>
    [DataField("scanFailState")]
    public string ScanFailState = "fail";

    /// <summary>
    /// Sprite state to set when idle.
    /// </summary>
    [DataField("idleState")]
    public string IdleState = "idle";
}