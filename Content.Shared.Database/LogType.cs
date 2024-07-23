namespace Content.Shared.Database;

// DO NOT CHANGE THE NUMERIC VALUES OF THESE
public enum LogType
{
    Unknown = 0, // do not use
    // DamageChange = 1
    Damaged = 2,
    Healed = 3,
    Slip = 4,
    EventAnnounced = 5,
    EventStarted = 6,
    EventRan = 16,
    EventStopped = 7,
    Verb = 19,
    ShuttleCalled = 8,
    ShuttleRecalled = 9,
    ExplosiveDepressurization = 10,
    Respawn = 13,
    RoundStartJoin = 14,
    LateJoin = 15,
    ChemicalReaction = 17,
    ReagentEffect = 18,
    CanisterValve = 20,
    CanisterPressure = 21,
    CanisterPurged = 22,
    CanisterTankEjected = 23,
    CanisterTankInserted = 24,
    DisarmedAction = 25,
    DisarmedKnockdown = 26,
    AttackArmedClick = 27,
    AttackArmedWide = 28,
    AttackUnarmedClick = 29,
    AttackUnarmedWide = 30,
    InteractHand = 31,
    InteractActivate = 32,
    Throw = 33,
    Landed = 34,
    ThrowHit = 35,
    Pickup = 36,
    Drop = 37,
    BulletHit = 38,
    ForceFeed = 40, // involuntary
    Ingestion = 53, // voluntary
    MeleeHit = 41,
    HitScanHit = 42,
    Mind = 43, // Suicides, ghosting, repossession, objectives, etc.
    Explosion = 44,
    Radiation = 45, // Unused
    Barotrauma = 46,
    Flammable = 47,
    Asphyxiation = 48,
    Temperature = 49,
    Hunger = 50,
    Thirst = 51,
    Electrocution = 52,
    CrayonDraw = 39,
    AtmosPressureChanged = 54,
    AtmosPowerChanged = 55,
    AtmosVolumeChanged = 56,
    AtmosFilterChanged = 57,
    AtmosRatioChanged = 58,
    FieldGeneration = 59,
    GhostRoleTaken = 60,
    Chat = 61,
    Action = 62,
    RCD = 63,
    Construction = 64,
    Trigger = 65,
    Anchor = 66,
    Unanchor = 67,
    EmergencyShuttle = 68,
    // haha so funny
    Emag = 69,
    Gib = 70,
    Identity = 71,
    CableCut = 72,
    StorePurchase = 73,
    LatticeCut = 74,
    Stripping = 75,
    Stamina = 76,
    EntitySpawn = 77,
    AdminMessage = 78,
    Anomaly = 79,
    WireHacking = 80,
    Teleport = 81,
    EntityDelete = 82,
    Vote = 83,
    ItemConfigure = 84,
    DeviceLinking = 85,
    Tile = 86,

    /// <summary>
    /// A client has sent too many chat messages recently and is temporarily blocked from sending more.
    /// </summary>
    ChatRateLimited = 87,
    AtmosTemperatureChanged = 88,
    DeviceNetwork = 89,
    StoreRefund = 90,

    /// <summary>
    /// User was rate-limited for some spam action.
    /// </summary>
    /// <remarks>
    /// This is a default value used by <c>PlayerRateLimitManager</c>, though users can use different log types.
    /// </remarks>
    RateLimited = 91,

    /// <summary>
    /// A player did an item-use interaction of an item they were holding onto another object.
    /// </summary>
    InteractUsing = 92,
}
