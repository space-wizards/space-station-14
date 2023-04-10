using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reaction;

[Prototype("reactiveGroup")]
public sealed class ReactiveGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;
}
