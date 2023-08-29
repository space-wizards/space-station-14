using Content.Server.Medical.Components;
using Content.Server.UserInterface;
using Content.Shared.MedicalScanner;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;

namespace Content.Server.CartridgeLoader.Cartridges;

/// <summary>
///    Attaches the <see cref="HealthAnalyzerComponent" /> to the PDA when cartridge with this component is installed
/// </summary>
[RegisterComponent]
public sealed partial class HealthAnalyzerCartridgeComponent : Component
{
    /// <summary>
    /// How long it takes to scan someone.
    /// </summary>
    [DataField("scanDelay")]
    public float ScanDelay = 0.8f;

    /// <summary>
    ///     Sound played on scanning begin
    /// </summary>
    [DataField("scanningBeginSound")]
    public SoundSpecifier? ScanningBeginSound;

    /// <summary>
    ///     Sound played on scanning end
    /// </summary>
    [DataField("scanningEndSound")]
    public SoundSpecifier? ScanningEndSound;
}
