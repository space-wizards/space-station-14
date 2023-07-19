using Content.Server.Clothing.Systems;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Server.Clothing.Components;

/// <summary>
///     If somebody from another faction equip cloyhing, this detonate
/// </summary>
//[NetworkedComponent]
[RegisterComponent]
[Access(typeof(ClothingFactionExplosionSystem))]
public sealed class ClothingFactionExplosionComponent : Component
{
    /// <summary>
    ///     friendly faction
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite),DataField("friendlyFaction")]
    public String Faction = "Syndicate";

    /// <summary>
    ///     max timer delay
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite),DataField("maxRandomTime")]
    public float MaxRandomTime = 30f;

    /// <summary>
    ///     min timer delay
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite),DataField("minRandomTime")]
    public float MinRandomTime = 60f;

    /// <summary>
    ///     VV timer duration
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite),DataField("VVtimerDuration")]
    public int VVTimerDuration = 5;

    /// <summary>
    ///     chance of detonation
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite),DataField("chance")]
    public float Chance = 0.95f;

    /// <summary>
    ///     the maximum time after which the detonation will start
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite),DataField("timerEnabled")]
    public bool VVTimerEnabled = true;

    /// <summary>
    ///     duration timer
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),DataField("timerDuration")]
    public int TimerDuration = 0; //Время до срабатывания таймера

    /// <summary>
    ///     bool flag of main
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),DataField("timerOn")]
    public bool TimerOn = false; // Включился ли таймер

    /// <summary>
    ///     time from start of timer to end
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),DataField("TimerDelay")]
    public float TimerDelay = 0f; // Время от стратра таймера до конца

    /// <summary>
    ///     main timer
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),DataField("timer")]
    public float Timer = 0f; // Сам таймер

    /// <summary>
    ///     bool flag of first warning
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),DataField("announcementWarnig")]
    public bool AnnouncementWarnig = false; //Стартовое оповещение

    /// <summary>
    ///     bool flag of second warning
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),DataField("announcementMessage")]
    public bool AnnouncementMessage = false; //Уточняющее оповещение

    /// <summary>
    ///     counttdown timer
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),DataField("timerCountdown")]
    public float TimerCountdown = 0f; // таймер обратного отсчёта

    /// <summary>
    ///     delay of messages of countdown
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),DataField("countdownDelay")]
    public float CountdownDelay = 1.5f; // Задержка обратного отсчёта

    /// <summary>
    ///     bool flag of countdown timer
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),DataField("countdownOn")]
    public bool CountdownOn = false; // Включён ли обратный отсчёт

    /// <summary>
    ///     how many times clothing was waer after start countdown
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),DataField("wearCount")]
    public int WearCount = 0; // как много раз одевали одежду после страта обратного отсчёта

    /// <summary>
    ///     how many times clothing you can wear after start countdown
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),DataField("wearCountMax")]
    public int WearCountMax = 1; // как много раз можно одеть вещь после стратра обратного отсчёта

    /// <summary>
    ///     how many times clothing you can wear after start countdown before permanent explosion
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),DataField("wearCountMaxPermanentExplosion")]
    public int WearCountPermanentExplosion = 4; // как много раз можно одеть вещь после стратра обратного отсчёта до перманентного взрыва

    /// <summary>
    ///     bool flag of first announcement
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),DataField("announcementWas")]
    public bool AnnouncementWas = false; // было ли первое предупреждение

    /// <summary>
    ///     bool flag countdown
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),DataField("countdownWas")]
    public bool CountdownWas = false; // начинался ли обратный отсчёт

}

