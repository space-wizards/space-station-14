using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Prototypes
{
    [Prototype]
    public sealed partial class MetabolismGroupPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("name", required: true)]
        private LocId Name { get; set; }

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedName => Loc.GetString(Name);
    }
}
