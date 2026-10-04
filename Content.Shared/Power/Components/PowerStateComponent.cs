using Content.Shared.Power.EntitySystems;
using Robust.Shared.Serialization;

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
[Access(typeof(SharedPowerStateSystem))]
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

    /// <summary>
    /// Needs to ensure ApcPowerReceiverComponent is attached to entity on startup.
    /// Entities which work with PowerConsumerComponent do not need that.
    /// </summary>
    [DataField]
    public bool EnsureApc = true;

    /// <summary>
    /// The visual state that is set when device is turned on.
    /// </summary>
    [DataField]
    public string? WorkingState = "beam";

    /// <summary>
    /// The visual state that is set when device doesn't have enough power.
    /// </summary>
    [DataField]
    public string? UnderpoweredState = "underpowered";

}

[NetSerializable, Serializable]
public enum PowerStateDeviceVisuals : byte
{
    VisualState
}

[Serializable, NetSerializable]
public enum PowerStateDeviceVisualLayers : byte
{
    Lights
}

[NetSerializable, Serializable]
public enum PowerStateDeviceVisualState
{
    On,
    Underpowered,
    Off
}
