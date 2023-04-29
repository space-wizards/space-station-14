
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges
{
    [RegisterComponent]
    public sealed class SpaceVendorsCartridgeComponent : Component
    {
        [DataField("appraisedItems")]
        public List<AppraisedItem> AppraisedItems = new();
    }
}
