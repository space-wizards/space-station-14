using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Prototypes
{
    [Prototype("metabolizerType")]
    public sealed class MetabolizerTypePrototype : IPrototype
    {
        [IdDataFieldAttribute]
        public string ID { get; } = default!;
    }
}
