using Content.Shared.Kitchen.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Kitchen.Components;

/// <summary>
/// Attached to a microwave that is currently in the process of cooking
/// </summary>
[RegisterComponent, Access(typeof(SharedMicrowaveSystem))]
[NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class ActiveMicrowaveComponent : Component
{
    /// <summary>
    ///     How frequently an active microwave will update its cooking cycle, so we aren't running this every tick.
    /// </summary>
    [DataField]
    public TimeSpan CookUpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     The recipe we are currently cooking.
    /// </summary>
    [DataField]
    public (FoodRecipePrototype? Recipe, uint Count) PortionedRecipe;

    /// <summary>
    ///     The total cooking time of this operation.
    /// </summary>
    [DataField]
    public float TotalTime;

    /// <summary>
    ///     The time that this microwave will finish cooking.
    /// </summary>
    [DataField]
    public TimeSpan CookTimeEnd = TimeSpan.Zero;

    /// <summary>
    ///     The last time that this microwave updated its cooking cycle..
    /// </summary>
    /// <remarks>
    ///     This is used to calculate how much heat to add to the microwave.
    /// </remarks>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan LastCookUpdated = TimeSpan.Zero;

    /// <summary>
    ///     The next time this microwave will update its cooking cycle.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextCookUpdate = TimeSpan.Zero;

    /// <summary>
    ///     The next time we attempt to roll a malfunction.
    /// </summary>
    /// <remarks>
    ///     Malfunctions are rolled on an interval and generate random effects, like lightning or microwave destruction.
    /// </remarks>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextMalfunction = TimeSpan.Zero;

    /// <summary>
    ///     Whether or not this microwave cooking process is malfunctioning.
    /// </summary>
    [DataField]
    public bool Malfunctioning = false;
}
