using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Solutions.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SolutionComponent : Component
{
    [DataField, AutoNetworkedField]
    public Solution Solution = new();
}
