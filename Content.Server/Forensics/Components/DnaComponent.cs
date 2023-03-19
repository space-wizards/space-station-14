namespace Content.Server.Forensics;

/// <summary>
/// This component is for mobs that have DNA.
/// </summary>
[RegisterComponent]
public sealed class DnaComponent : Component
{
    [DataField("dna")]
    public string? DNA = String.Empty;
}
