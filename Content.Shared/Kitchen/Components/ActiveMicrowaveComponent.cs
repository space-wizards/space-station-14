using Content.Shared.Kitchen;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Kitchen.Components;

/// <summary>
/// Attached to a microwave that is currently in the process of cooking
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveMicrowaveComponent : Component
{
    /// <summary>
    ///     How much time this microwave has left to cook.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float CookTimeRemaining;

    /// <summary>
    ///     The total cooking time of this operation.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float TotalTime;

    /// <summary>
    ///     The next time we attempt to roll a malfunction.
    /// </summary>
    /// <remarks>
    ///     Malfunctions are rolled on an interval and generate random effects, like lightning or microwave destruction.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan MalfunctionTime = TimeSpan.Zero;

    /// <summary>
    ///     The recipe we are currently cooking.
    /// </summary>
    [ViewVariables]
    public (FoodRecipePrototype? Recipe, uint Count) PortionedRecipe;
}
