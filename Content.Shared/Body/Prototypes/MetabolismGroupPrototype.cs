using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Prototypes
{
    [Prototype("metabolismGroup")]
    public sealed partial class MetabolismGroupPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;
    }
}
