using Content.Server.UserInterface;
using Content.Shared.Botany.PlantAnalyzer;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;

namespace Content.Server.Botany.Components;

/// <summary>
///    After scanning, retrieves the target Uid to use with its related UI.
/// </summary>
[RegisterComponent]
public sealed class PlantAnalyzerComponent : Component
{
    /// <summary>
    /// How long it takes to scan seed.
    /// </summary>

    [DataField("scanDelay")] public float ScanDelay = 0.5f;

    /// <summary>
    ///   UI key to bind the required interface
    /// </summary>

    public BoundUserInterface? UserInterface => Owner.GetUIOrNull(PlantAnalyzerUiKey.Key);

    /// <summary>
    ///     Sound played on scanning seed begin
    /// </summary>
    [DataField("scanningBeginSound")]
    public SoundSpecifier? ScanningBeginSound;

    /// <summary>
    ///     Sound played on scanning seed end
    /// </summary>
    [DataField("scanningEndSound")]
    public SoundSpecifier? ScanningEndSound;



}
