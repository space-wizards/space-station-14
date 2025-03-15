using Robust.Shared.Configuration;

namespace Content.Shared._DV.CCVars;

/// <summary>
/// DeltaV specific cvars.
/// </summary>
[CVarDefs]
// ReSharper disable once InconsistentNaming - Shush you
public sealed class DCCVars
{
    /// <summary>
    /// Whether the Shipyard is enabled.
    /// </summary>
    public static readonly CVarDef<bool> Shipyard =
        CVarDef.Create("shuttle.shipyard", true, CVar.SERVERONLY);

    /// <summary>
    /// How many mail candidates do we need per actual delivery sent when
    /// the mail goes out? The number of candidates is divided by this number
    /// to determine how many deliveries will be teleported in.
    /// It does not determine unique recipients. That is random.
    /// </summary>
    public static readonly CVarDef<int> MailCandidatesPerDelivery =
        CVarDef.Create("mail.candidatesperdelivery", 6, CVar.SERVERONLY);

    /// <summary>
    /// Do not teleport any more mail in, if there are at least this many
    /// undelivered parcels.
    /// </summary>
    /// <remarks>
    /// Currently this works by checking how many MailComponent entities
    /// are sitting on the teleporter's tile.
    ///
    /// It should be noted that if the number of actual deliveries to be
    /// made based on the number of candidates divided by candidates per
    /// delivery exceeds this number, the teleporter will spawn more mail
    /// than this number.
    ///
    /// This is just a simple check to see if anyone's been picking up the
    /// mail lately to prevent entity bloat for the sake of performance.
    /// </remarks>
    public static readonly CVarDef<int> MailMaximumUndeliveredParcels =
        CVarDef.Create("mail.maximumundeliveredparcels", 12, CVar.SERVERONLY);

    /// <summary>
    /// What's the base bonus for delivering a package intact?
    /// </summary>
    public static readonly CVarDef<int> MailDefaultBounty =
        CVarDef.Create("mail.defaultbounty", 300, CVar.SERVERONLY);

    /// <summary>
    /// What's the base malus for delivering a package intact?
    /// </summary>
    public static readonly CVarDef<int> MailDefaultPenelty =
        CVarDef.Create("mail.defaultpenelty", -50, CVar.SERVERONLY);

    /// <summary>
    /// Any item that breaks or is destroyed in less than this amount of
    /// damage is one of the types of items considered fragile.
    /// </summary>
    public static readonly CVarDef<int> MailFragileDamageThreshold =
        CVarDef.Create("mail.fragiledamagethreshold", 10, CVar.SERVERONLY);

    /// <summary>
    /// What's the bonus for delivering a fragile package intact?
    /// </summary>
    public static readonly CVarDef<int> MailFragileBonus =
        CVarDef.Create("mail.fragilebonus", 300, CVar.SERVERONLY);

    /// <summary>
    /// What's the malus for delivering a fragile package intact?
    /// </summary>
    public static readonly CVarDef<int> MailFragileMalus =
        CVarDef.Create("mail.fragilemalus", -250, CVar.SERVERONLY);

    /// <summary>
    /// What's the chance for any one delivery to be marked as priority mail?
    /// </summary>
    public static readonly CVarDef<float> MailPriorityChances =
        CVarDef.Create("mail.prioritychance", 0.1f, CVar.SERVERONLY);

    /// <summary>
    /// How long until a priority delivery is considered as having failed
    /// if not delivered?
    /// </summary>
    public static readonly CVarDef<double> MailPriorityDuration =
        CVarDef.Create("mail.priorityduration", 5.0d, CVar.SERVERONLY);

    /// <summary>
    /// What's the bonus for delivering a priority package intact?
    /// </summary>
    public static readonly CVarDef<int> MailPriorityBonus =
        CVarDef.Create("mail.prioritybonus", 500, CVar.SERVERONLY);

    /// <summary>
    /// What's the malus for delivering a priority package intact?
    /// </summary>
    public static readonly CVarDef<int> MailPriorityMalus =
        CVarDef.Create("mail.prioritymalus", -150, CVar.SERVERONLY);

    /// <summary>
    /// What's the bonus for delivering a large package intact?
    /// </summary>
    public static readonly CVarDef<int> MailLargeBonus =
        CVarDef.Create("mail.largebonus", 500, CVar.SERVERONLY);

    /// <summary>
    /// What's the malus for delivering a large package intact?
    /// </summary>
    public static readonly CVarDef<int> MailLargeMalus =
        CVarDef.Create("mail.largemalus", -250, CVar.SERVERONLY);

    /// <summary>
    /// Disables all vision filters for species like Vulpkanin or Harpies. There are good reasons someone might want to disable these.
    /// </summary>
    public static readonly CVarDef<bool> NoVisionFilters =
        CVarDef.Create("accessibility.no_vision_filters", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
