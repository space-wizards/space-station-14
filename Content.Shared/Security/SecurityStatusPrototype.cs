using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Security;

/// <summary>
/// A prototype that defines a security status like `Wanted` or `Detained`.
/// The old `SecurityStatus.None` is gone and instead `null` is used for places storing a status.
/// </summary>
[Prototype]
public sealed class SecurityStatusPrototype: IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The localised name for this status.
    /// </summary>
    [DataField]
    public string Name = string.Empty;

    /// <summary>
    /// The icon that someone with this status will display to someone looking at them with a sec hud.
    /// </summary>
    [DataField]
    public ProtoId<SecurityIconPrototype> Icon;

    /// <summary>
    /// A localised string which is announced on sec radio when someone has their status set to this.
    /// </summary>
    [DataField]
    public string StatusSetAnnouncement = string.Empty;

    /// <summary>
    /// A localised string which is announced on sec radio when someone had this status but it was set to `null`.
    /// </summary>
    [DataField]
    public string StatusUnSetAnnouncement = string.Empty;

    /// <summary>
    /// Does this status need an accompanying reason?
    /// For example, `Wanted` has a reason to justify the arrest.
    /// </summary>
    [DataField]
    public bool NeedsReason = false;

    /// <summary>
    /// A little localised string which is display in the criminal records console alongside the reason associated with the status.
    /// </summary>
    [DataField]
    public string ReasonText = string.Empty;

    /// <summary>
    /// Should this status automatically update the person's criminal record?
    /// For example, setting someone to `Wanted` with a reason and then setting them to `Detained` will automatically add the wanted reason to their criminal record.
    /// </summary>
    [DataField]
    public bool StoreHistory = false;

    /// <summary>
    /// A little localised string which is prepended to any automatically generated crime history.
    /// For example, `Detained` will prepend the string `DETAINED: ...` to any automatically generated crime history.
    /// </summary>
    [DataField]
    public string HistoryText = string.Empty;

    /// <summary>
    /// What order should all the statuses appear in the drop down in the criminal records console?
    /// Smaller means higher.
    /// If two statuses have the same order there won't be an error but their relative order will be decided arbitrarily.
    /// </summary>
    [DataField]
    public int Order = 0;
}
