using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reaction;

[Prototype]
public sealed partial class ReactiveGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
