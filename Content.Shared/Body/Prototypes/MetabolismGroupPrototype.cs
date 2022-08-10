using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Prototypes
{
    [Prototype("metabolismGroup")]
    public sealed class MetabolismGroupPrototype : IPrototype
    {
        [IdDataFieldAttribute]
        public string ID { get; } = default!;
    }
}
