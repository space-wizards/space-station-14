using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Chemistry.Reaction;

[Prototype("reactiveGroup")]
public class ReactiveGroupPrototype : IPrototype
{
    [DataField("id", required: true)]
    public string ID { get; } = default!;
}
