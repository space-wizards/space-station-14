using Content.Shared.Kitchen;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Kitchen.Components;

/// <summary>
/// Attached to a microwave that is currently in the process of cooking
/// </summary>
[RegisterComponent]
public sealed partial class ActiveMicrowaveComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float CookTimeRemaining;

    [ViewVariables(VVAccess.ReadWrite)]
    public float TotalTime;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan MalfunctionTime = TimeSpan.Zero;

    [ViewVariables]
    public (FoodRecipePrototype?, int) PortionedRecipe;
}
