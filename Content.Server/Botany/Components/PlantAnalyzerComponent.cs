using Robust.Shared.Audio;

namespace Content.Server.Botany.Components;

/// <summary>
///    After scanning, retrieves the target Uid to use with its related UI.
/// </summary>
[RegisterComponent]
public sealed partial class PlantAnalyzerComponent : Component
{
    /// <summary>
    ///     How long it takes to scan.
    /// </summary>
    [DataField("scanDelay")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ScanDelay = 20f;

    [DataField("advScanDelay")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float AdvScanDelay = 40f;

    /// <summary>
    ///     Which scan mode to use
    /// </summary>
    [DataField("advancedScan")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool AdvancedScan = false;

    /// <summary>
    ///     Sound played on scanning begin.
    /// </summary>
    [DataField("scanningBeginSound")]
    public SoundSpecifier? ScanningBeginSound;

    /// <summary>
    ///     Sound played on scanning end.
    /// </summary>
    [DataField("scanningEndSound")]
    public SoundSpecifier? ScanningEndSound;
}
