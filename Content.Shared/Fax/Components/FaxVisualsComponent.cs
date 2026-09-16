using Robust.Shared.Serialization;

namespace Content.Shared.Fax.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class FaxVisualsComponent : Component
{
    /// <summary>
    /// Default sprite to use when inserting an object.
    /// </summary>
    [DataField]
    public string InsertingState = "inserting";

    /// <summary>
    /// Default sprite to use when inserting an object.
    /// </summary>
    [DataField]
    public string PrintingState = "printing";
}

[Serializable, NetSerializable]
public enum FaxMachineVisuals : byte
{
    VisualState,
    Inserting,
}
