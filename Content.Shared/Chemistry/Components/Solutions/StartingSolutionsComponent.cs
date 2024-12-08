using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Chemistry.Components.Solutions;

[RegisterComponent]
public sealed partial class StartingSolutionsComponent : Component
{
    [DataField(required: true)]
    public Dictionary<string, SolutionSpecifier?> Solutions = new();
}
