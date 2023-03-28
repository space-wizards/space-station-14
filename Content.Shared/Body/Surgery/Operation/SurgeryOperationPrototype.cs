using System.Collections.Immutable;
using Content.Shared.Body.Part;
using Content.Shared.Body.Surgery.Operation.Effect;
using Content.Shared.Body.Surgery.Operation.Step;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Surgery.Operation;

[Prototype("surgeryOperation")]
public sealed class SurgeryOperationPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; } = string.Empty;

    [DataField("name", required: true)]
    public string Name = string.Empty;

    [DataField("description")]
    public string Description = string.Empty;

    [DataField("steps", required: true)]
    public ImmutableList<OperationStep> Steps = ImmutableList<OperationStep>.Empty;

    [DataField("effect")]
    public IOperationEffect? Effect;

    /// <summary>
    /// Valid bodyparts that this operation can be done on
    /// </summary>
    [DataField("parts", required: true)]
    public HashSet<BodyPartType> Parts = new();

    [DataField("hidden")]
    public bool Hidden;
}
