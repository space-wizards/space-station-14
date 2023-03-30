using Content.Shared.Kitchen;

namespace Content.Server.Kitchen.Components;

/// <summary>
/// Attached to a microwave that is currently in the process of cooking
/// </summary>
[RegisterComponent]
public sealed class ActiveMicrowaveComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float CookTimeRemaining;

    [ViewVariables(VVAccess.ReadWrite)]
    public float TotalTime;

    [ViewVariables]
    public (FoodRecipePrototype?, int) PortionedRecipe;

    [ViewVariables]
    public bool Haywire;

    [ViewVariables]
    public float HaywireTimeRemaining;
}
