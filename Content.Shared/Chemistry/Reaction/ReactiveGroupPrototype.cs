using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Chemistry.Reaction;

[Prototype("reactiveGroup")]
public sealed class ReactiveGroupPrototype : IPrototype
{
    [IdDataFieldAttribute]
    public string ID { get; } = default!;
}
