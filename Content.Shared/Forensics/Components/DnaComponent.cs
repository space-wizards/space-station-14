using Robust.Shared.GameStates;

namespace Content.Shared.Forensics.Components;

/// <summary>
/// This component is for mobs that have DNA.
/// </summary>
[RegisterComponent]
public sealed partial class DnaComponent : Component
{
    [DataField]
    public string? DNA;
}
