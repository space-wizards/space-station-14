using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Prototypes
{
    [Prototype("metabolizerType")]
    public sealed class MetabolizerTypePrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("name", required: true)]
        public string Name { get; private set; } = default!;
    }
}
