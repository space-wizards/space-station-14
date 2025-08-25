using Robust.Shared.GameStates;

namespace Content.Shared.Forensics.Components;

/// <summary>
/// This controls residues left on items
/// which the forensics system uses.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ResidueComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId ResidueAdjective = "residue-unknown";

    [DataField, AutoNetworkedField]
    public string? ResidueColor;
}
