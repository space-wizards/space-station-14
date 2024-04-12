using Robust.Shared.GameStates;
namespace Content.Shared.Medical.Metabolism.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MetabolismComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string LinkedSolutionName;
}
