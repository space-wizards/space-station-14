using Robust.Shared.GameStates;

namespace Content.Shared.Forensics.Components;

/// <summary>
/// This component is for mobs that have DNA.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DnaComponent : Component
{
    [DataField("dna"), AutoNetworkedField]
    public string DNA = String.Empty;
}
