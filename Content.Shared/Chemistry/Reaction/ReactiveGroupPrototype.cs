using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reaction;

[Prototype("reactiveGroup")]
public sealed class ReactiveGroupPrototype : IPrototype
{
    [IdDataFieldAttribute]
    public string ID { get; } = default!;
}
