using Robust.Shared.Prototypes;

namespace Content.Server.Advertisements
{
    [Serializable, Prototype("advertisementsPack")]
    public readonly record struct AdvertisementsPackPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [DataField("advertisements")]
        public List<string> Advertisements { get; } = new();
    }
}
