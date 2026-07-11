using Robust.Shared.Prototypes;

namespace Content.Shared.Construction.Components;

/// <summary>
/// Used in construction graphs for building wall-mounted electronic devices.
/// </summary>
[RegisterComponent]
public sealed partial class ElectronicsBoardComponent : Component
{
    /// <summary>
    /// The device that is produced when the construction is completed.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype;
}
