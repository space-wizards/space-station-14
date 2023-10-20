using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Prototypes
{
    [Prototype("metabolismGroup")]
    public sealed class MetabolismGroupPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;
    }
}
