using Content.Server.Objectives.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Objectives.Components.Targets;

/// <summary>
/// Allows an object to become the target of a StealCollection  objection
/// </summary>
[RegisterComponent]
public sealed partial class StealTargetComponent : Component
{
    /// <summary>
    /// The theft group to which this item belongs.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string StealGroup;
}
