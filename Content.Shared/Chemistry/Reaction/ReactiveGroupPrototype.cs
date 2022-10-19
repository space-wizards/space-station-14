using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reaction;

[Prototype("reactiveGroup")]
public readonly record struct ReactiveGroupPrototype : IPrototype
{
    [IdDataFieldAttribute]
    public string ID { get; } = default!;
}
