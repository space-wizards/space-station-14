using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.MedicalScanner;

[RegisterComponent, NetworkedComponent]
public sealed partial class MedicalScannerCanDropComponent : Component
{
    [DataField("enabled")]
    public bool Enabled = true;
}
