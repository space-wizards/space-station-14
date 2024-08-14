using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components.Solutions;

[RegisterComponent]
public sealed partial class InitialSolutionsComponent : Component
{
    [DataField(required: true)]
    public Dictionary<string, SolutionSpecifier?> Solutions = new();
}
