using Content.Server.Clothing.Systems;

namespace Content.Server.Clothing.Components;

/// <summary>
///     If somebody from another faction equip cloyhing, this detonate
/// </summary>
//[NetworkedComponent]
[RegisterComponent]
[Access(typeof(ClothingFactionExplosionSystem))]
public sealed partial class ClothingFactionExplosionComponent : Component
{
    /// <summary>
    ///     friendly faction
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("friendlyFaction")]
    public String Faction = "Syndicate";

    /// <summary>
    ///     max timer delay
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxRandomTime")]
    public float MaxRandomTime = 10f;

    /// <summary>
    ///     min timer delay
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("minRandomTime")]
    public float MinRandomTime = 40f;

    /// <summary>
    ///     VV timer duration
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("VVtimerDuration")]
    public int VVTimerDuration = 5;

    /// <summary>
    ///     chance of detonation
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("chance")]
    public float Chance = 0.90f;

    /// <summary>
    ///     the maximum time after which the detonation will start
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("timerEnabled")]
    public bool VVTimerEnabled = true;

    /// <summary>
    ///     duration timer
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("timerDuration")]
    public int TimerDuration = 0;

    /// <summary>
    ///     bool flag of main
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("timerOn")]
    public bool TimerOn = false;

    /// <summary>
    ///     time from start of timer to end
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("TimerDelay")]
    public float TimerDelay = 0f;

    /// <summary>
    ///     main timer
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("timer")]
    public float Timer = 0f;

    /// <summary>
    ///     bool flag of first warning
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("announcementWarnig")]
    public bool AnnouncementWarnig = false;

    /// <summary>
    ///     bool flag of second warning
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("announcementMessage")]
    public bool AnnouncementMessage = false;

    /// <summary>
    ///     counttdown timer
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("timerCountdown")]
    public float TimerCountdown = 0f;

    /// <summary>
    ///     delay of messages of countdown
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("countdownDelay")]
    public float CountdownDelay = 1.5f;
    /// <summary>
    ///     bool flag of countdown timer
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("countdownOn")]
    public bool CountdownOn = false;

    /// <summary>
    ///     how many times clothing was waer after start countdown
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("wearCount")]
    public int WearCount = 0;

    /// <summary>
    ///     how many times clothing you can wear after start countdown
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("wearCountMax")]
    public int WearCountMax = 1;

    /// <summary>
    ///     how many times clothing you can wear after start countdown before permanent explosion
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("wearCountMaxPermanentExplosion")]
    public int WearCountPermanentExplosion = 4;

    /// <summary>
    ///     bool flag of first announcement
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("announcementWas")]
    public bool AnnouncementWas = false;

    /// <summary>
    ///     bool flag countdown
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("countdownWas")]
    public bool CountdownWas = false;

    /// <summary>
    ///     last user
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("lastuser")]
    public EntityUid LastUser;

    /// <summary>
    ///     owner is friendly
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("ownerIsFriendly")]
    public bool OwnerIsFriendly = false;

}

