using Robust.Shared.Serialization;

namespace Content.Shared.Fax.Components;

/// <summary>
/// Controls the appearance of a <see cref="FaxMachineComponent"/>.
/// </summary>
/// <seealso cref="FaxMachineVisuals"/>
[RegisterComponent]
public sealed partial class FaxVisualsComponent : Component
{
    /// <summary>
    /// Default RSI state to use when inserting an object.
    /// </summary>
    [DataField]
    public string InsertingState = "inserting";

    /// <summary>
    /// Default RSI state to use when inserting an object.
    /// </summary>
    [DataField]
    public string PrintingState = "printing";
}

/// <summary>
/// <see cref="AppearanceComponent.AppearanceData"/> keys for a fax machine's appearance.
/// </summary>
[Serializable, NetSerializable]
public enum FaxMachineVisuals : byte
{
    VisualState,
    Inserting,
}
