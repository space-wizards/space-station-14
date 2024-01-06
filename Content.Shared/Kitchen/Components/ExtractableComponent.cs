using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Kitchen.Components;

/// <summary>
/// Tag component that denotes an entity as Extractable
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ExtractableComponent : Component
{
    [DataField("juiceSolution")]
    public Solution? JuiceSolution;

    [DataField("grindableSolutionName")]
    public string? GrindableSolution;
};
