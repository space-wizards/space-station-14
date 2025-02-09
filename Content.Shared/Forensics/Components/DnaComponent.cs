namespace Content.Shared.Forensics.Components;

/// <summary>
/// This component is for mobs that have DNA.
/// </summary>
[RegisterComponent]
public sealed partial class DnaComponent : Component
{
    [DataField("dna"), ViewVariables(VVAccess.ReadWrite)]
    public string DNA = String.Empty;
}
