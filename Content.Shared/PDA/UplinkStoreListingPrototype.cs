using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Robust.Shared.Utility;

namespace Content.Shared.PDA
{
    [Prototype("uplinkListing")]
    public sealed class UplinkStoreListingPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [DataField("itemId", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ItemId { get; } = string.Empty;

        [DataField("price")]
        public int Price { get; } = 5;

        [DataField("category")]
        public UplinkCategory Category { get; } = UplinkCategory.Utility;

        [DataField("description")]
        public string Description { get; } = string.Empty;

        [DataField("listingName")]
        public string ListingName { get; } = string.Empty;

        [DataField("icon")]
        public SpriteSpecifier? Icon { get; } = null;

        [DataField("jobWhitelist", customTypeSerializer:typeof(PrototypeIdHashSetSerializer<JobPrototype>))]
        public HashSet<string>? JobWhitelist;

        [DataField("surplus")]
        public bool CanSurplus = true;
    }
}
