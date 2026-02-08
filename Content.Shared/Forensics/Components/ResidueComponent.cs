using Robust.Shared.GameStates;

namespace Content.Shared.Forensics.Components;

/// <summary>
/// This controls residues left on items
/// which the forensics system uses.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ResidueComponent : Component
{
    [DataField]
    public LocId ResidueAdjective = "residue-unknown";

    [DataField]
    public string? ResidueColor;
}
