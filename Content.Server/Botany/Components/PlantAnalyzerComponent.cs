using Robust.Shared.Audio;
using Content.Shared.DoAfter;

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
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ScanDelay = 20f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float AdvScanDelay = 40f;

    /// <summary>
    ///     Which scan mode to use.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool AdvancedScan = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public DoAfterId? DoAfter;

    /// <summary>
    ///     Sound played on scanning end.
    /// </summary>
    [DataField]
    public SoundSpecifier? ScanningEndSound;
}
