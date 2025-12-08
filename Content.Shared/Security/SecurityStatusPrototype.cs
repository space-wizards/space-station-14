using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Security;

[Prototype]
public sealed class SecurityStatusPrototype: IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = default!;

    [DataField]
    public ProtoId<SecurityIconPrototype> Icon;

    [DataField]
    public string StatusSetAnnouncement = default!;

    [DataField]
    public string StatusUnSetAnnouncement = default!;

    [DataField]
    public bool NeedsReason = false;

    [DataField]
    public string ReasonText = default!;

    [DataField]
    public bool StoreHistory = false;
}
