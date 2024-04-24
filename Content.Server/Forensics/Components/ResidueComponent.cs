namespace Content.Server.Forensics;

/// <summary>
/// This controls residues left on items
/// which the forensics system uses.
/// </summary>
[RegisterComponent]
public sealed partial class ResidueComponent : Component
{
    [DataField]
    public LocId ResidueAdjective = "residue-unknown";

    [DataField]
    public string? ResidueColor;
}
