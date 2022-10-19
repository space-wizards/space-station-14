using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Prototypes
{
    [Prototype("metabolismGroup")]
    public readonly record struct MetabolismGroupPrototype : IPrototype
    {
        [IdDataFieldAttribute]
        public string ID { get; } = default!;
    }
}
