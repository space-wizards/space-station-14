using Content.Shared.DoAfter;
using Content.Shared.Eui;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.MMI;

[RegisterComponent]
public sealed partial class MMIExtractorComponent : Component
{
    [DataField]
    public float Delay = 30f;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public LocId NoMind = "mmi-extractor-no-mind";

    [DataField]
    public LocId Asking = "mmi-extractor-probing";

    [DataField]
    public LocId Accepted = "mmi-extractor-accepted";

    [DataField]
    public LocId Denied = "mmi-extractor-denied";

    [DataField]
    public LocId NoResponse = "mmi-extractor-inconclusive";

    [DataField]
    public LocId TooManyBrains = "mmi-extractor-too-many-brains";

    [DataField]
    public LocId Brainless = "mmi-extractor-brainless";
}

[Serializable, NetSerializable]
public sealed class MMIExtractorMessage : EuiMessageBase
{
    public readonly bool Accepted;

    public MMIExtractorMessage(bool accepted)
    {
        Accepted = accepted;
    }
}

[Serializable, NetSerializable]
public sealed partial class MMIExtractorDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public bool Accepted = false;
}
