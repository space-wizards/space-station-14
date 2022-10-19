using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Prototypes
{
    [Prototype("metabolizerType")]
    public readonly record struct MetabolizerTypePrototype : IPrototype
    {
        [IdDataFieldAttribute]
        public string ID { get; } = default!;
    }
}
