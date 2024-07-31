using Content.Shared.Chemistry.Types;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components.Solutions;

[RegisterComponent]
public sealed partial class InitialSolutionsComponent : Component
{
    [DataField(required: true)] public Dictionary<string, Dictionary<InitialSolutionReagentId, FixedPoint2>?> Contents = new();
}

[DataRecord, Serializable, NetSerializable]
public record struct InitialSolutionReagentId(string ReagentId, ReagentVariant? Variant);
