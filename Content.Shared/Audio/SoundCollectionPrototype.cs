using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Audio
{
    [Prototype("soundCollection")]
    public sealed class SoundCollectionPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [DataField("files")]
        public List<ResourcePath> PickFiles { get; } = new();
    }
}
