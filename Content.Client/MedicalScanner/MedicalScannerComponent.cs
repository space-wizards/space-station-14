using Content.Shared.MedicalScanner;

namespace Content.Client.MedicalScanner;

[RegisterComponent]
public sealed class MedicalScannerComponent : SharedMedicalScannerComponent
{
    /// <summary>
    /// A map of the base machine sprite states indexed by which machine states they correspond to.
    /// </summary>
    public readonly Dictionary<MedicalScannerStatus, string> MachineStateStatusMap = new()
    {
        [MedicalScannerStatus.Off] = "closed",
        [MedicalScannerStatus.Open] = "open",
        [MedicalScannerStatus.Red] = "occupied",
        [MedicalScannerStatus.Death] = "occupied",
        [MedicalScannerStatus.Green] = "occupied",
        [MedicalScannerStatus.Yellow] = "occupied",
    };

    /// <summary>
    /// A map of the machine terminal sprite states indexed by which machine states they correspond to.
    /// </summary>
    public readonly Dictionary<MedicalScannerStatus, string> TerminalStateStatusMap = new()
    {
        [MedicalScannerStatus.Off] = "off_unlit",
        [MedicalScannerStatus.Open] = "idle_unlit",
        [MedicalScannerStatus.Red] = "red_unlit",
        [MedicalScannerStatus.Death] = "off_unlit",
        [MedicalScannerStatus.Green] = "idle_unlit",
        [MedicalScannerStatus.Yellow] = "maint_unlit",
    };
}
