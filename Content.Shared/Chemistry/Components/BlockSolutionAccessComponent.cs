using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Blocks all attempts to access the specified solution contained by this entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class BlockSolutionAccessComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Solution = "default";
}
