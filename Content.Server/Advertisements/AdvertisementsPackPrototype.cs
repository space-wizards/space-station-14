using Robust.Shared.Prototypes;

namespace Content.Server.Advertisements
{
    [Serializable, Prototype("advertisementsPack")]
    public sealed partial class AdvertisementsPackPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("advertisements")]
        public List<string> Advertisements { get; private set; } = new();

        [DataField("thankyous")]
        public List<string> ThankYous { get; private set; } = new();
    }
}
