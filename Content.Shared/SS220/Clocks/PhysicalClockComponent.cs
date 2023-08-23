// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Clocks;

/// <summary>
/// Allows to check station time by examining the entity with this component.
/// </summary>
[RegisterComponent]
public sealed partial class PhysicalClockComponent : Component
{
    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;
}
