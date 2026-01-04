using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// This component is used for entities whose sprite and light color
/// should match the color of a solution on them.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReagentColorComponent : Component
{
    /// <summary>
    /// The name of the solution that determines the color.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string? Solution;
}
