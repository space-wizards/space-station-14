namespace Content.Shared.Power.Components;

/// <summary>
/// Generic component for giving entities "idle" and "working" power states.
/// </summary>
/// <remarks><para>Entities that have more complex power draw
/// (ex. a thermomachine whose heating power is directly tied to its power consumption)
/// should just directly set their load on the <see cref="SharedApcPowerReceiverComponent"/>.</para>
///
/// <para>This is also applicable if you would like to add
/// more complex power behavior that is tied to a generic component.</para></remarks>
[RegisterComponent]
public sealed partial class PowerStateComponent : Component
{
    /// <summary>
    /// Whether the entity is currently in the working state.
    /// </summary>
    [DataField]
    public bool IsWorking;

    /// <summary>
    /// The idle power draw of this entity when not working, in watts.
    /// </summary>
    [DataField]
    public float IdlePowerDraw = 20f;

    /// <summary>
    /// The working power draw of this entity when working, in watts.
    /// </summary>
    [DataField]
    public float WorkingPowerDraw = 350f;
}
