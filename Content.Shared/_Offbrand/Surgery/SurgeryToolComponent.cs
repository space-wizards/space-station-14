using Content.Shared.Inventory;
using Content.Shared.Whitelist;

namespace Content.Shared._Offbrand.Surgery;

[RegisterComponent]
[Access(typeof(SurgeryToolSystem))]
public sealed partial class SurgeryToolComponent : Component
{
    [DataField(required: true)]
    public SlotFlags SlotsToCheck;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField(required: true)]
    public LocId SlotsDenialPopup;

    [DataField(required: true)]
    public LocId DownDenialPopup;
}
