namespace Content.Shared.Database;

// DO NOT CHANGE THE NUMERIC VALUES OF THESE
public enum LogType
{
    /// <summary>
    /// Test logs. DO NOT USE!!!
    /// </summary>
    Unknown = 0,
    // DamageChange = 1

    /// <summary>
    /// A player dealt damage to an entity.
    /// </summary>
    Damaged = 2,

    /// <summary>
    /// A player healed an entity.
    /// </summary>
    Healed = 3,

    /// <summary>
    /// A player slipped on an entity.
    /// </summary>
    Slip = 4,

    /// <summary>
    /// Station event was added or announced.
    /// </summary>
    EventAnnounced = 5,

    /// <summary>
    /// Game rule was added or started.
    /// </summary>
    EventStarted = 6,
    EventRan = 16,

    /// <summary>
    /// Game rule was stopped.
    /// </summary>
    EventStopped = 7,

    /// <summary>
    /// A player used a verb on an entity.
    /// </summary>
    Verb = 19,

    /// <summary>
    /// An evacuation shuttle was called.
    /// </summary>
    ShuttleCalled = 8,

    /// <summary>
    /// An evacuation shuttle was recalled.
    /// </summary>
    ShuttleRecalled = 9,

    /// <summary>
    /// Explosive depressurization related interactions.
    /// </summary>
    ExplosiveDepressurization = 10,

    /// <summary>
    /// A player or entity was respawned.
    /// </summary>
    Respawn = 13,

    /// <summary>
    /// A player joined station on round start.
    /// </summary>
    RoundStartJoin = 14,

    /// <summary>
    /// A player joined station after round start.
    /// </summary>
    LateJoin = 15,

    /// <summary>
    /// Chemical reactions related interactions.
    /// </summary>
    ChemicalReaction = 17,

    /// <summary>
    /// Reagent effects related interactions.
    /// </summary>
    ReagentEffect = 18,

    /// <summary>
    /// Canister valve was opened or closed.
    /// </summary>
    CanisterValve = 20,

    /// <summary>
    /// Release pressure on the canister was changed.
    /// </summary>
    CanisterPressure = 21,

    /// <summary>
    /// Canister purged its contents into the environment.
    /// </summary>
    CanisterPurged = 22,

    /// <summary>
    /// Tank was ejected from the canister.
    /// </summary>
    CanisterTankEjected = 23,

    /// <summary>
    /// Tank was inserted into the canister.
    /// </summary>
    CanisterTankInserted = 24,

    /// <summary>
    /// A player tried to disarm an entity.
    /// </summary>
    DisarmedAction = 25,

    /// <summary>
    /// A player knocked down an entity on the floor.
    /// </summary>
    DisarmedKnockdown = 26,
    AttackArmedClick = 27,
    AttackArmedWide = 28,
    AttackUnarmedClick = 29,
    AttackUnarmedWide = 30,

    /// <summary>
    /// A player interacted with an entity in his hand.
    /// </summary>
    InteractHand = 31,

    /// <summary>
    /// A player activated an entity.
    /// </summary>
    InteractActivate = 32,

    /// <summary>
    /// A player threw an entity.
    /// </summary>
    Throw = 33,

    /// <summary>
    /// Entity landed.
    /// </summary>
    Landed = 34,

    /// <summary>
    /// A thrown entity hit the other entity.
    /// </summary>
    ThrowHit = 35,

    /// <summary>
    /// A player picked up an entity.
    /// </summary>
    Pickup = 36,

    /// <summary>
    /// A player dropped an entity.
    /// </summary>
    Drop = 37,

    /// <summary>
    /// A bullet hit an entity.
    /// </summary>
    BulletHit = 38,

    /// <summary>
    /// A player force-feed an entity or injected it with a solution.
    /// </summary>
    ForceFeed = 40,

    /// <summary>
    /// A player ate an entity or injected themselves with a solution.
    /// </summary>
    Ingestion = 53,

    /// <summary>
    /// A melee attack hit an entity.
    /// </summary>
    MeleeHit = 41,

    /// <summary>
    /// A hitscan attack hit an entity.
    /// </summary>
    HitScanHit = 42,

    /// <summary>
    /// Suicides, ghosting, repossession, objectives, etc.
    /// </summary>
    Mind = 43,

    /// <summary>
    /// Explosions and explosives related interactions.
    /// </summary>
    Explosion = 44,
    Radiation = 45,

    /// <summary>
    /// Entity started or stopped taking pressure damage.
    /// </summary>
    Barotrauma = 46,

    /// <summary>
    /// Fire started or stopped.
    /// </summary>
    Flammable = 47,

    /// <summary>
    /// Entity started or stopped suffocating.
    /// </summary>
    Asphyxiation = 48,

    /// <summary>
    /// Entity started or stopped taking temperature damage.
    /// </summary>
    Temperature = 49,
    Hunger = 50,
    Thirst = 51,

    /// <summary>
    /// Entity received electrocution damage.
    /// </summary>
    Electrocution = 52,

    /// <summary>
    /// A player drew using a crayon.
    /// </summary>
    CrayonDraw = 39,

    /// <summary>
    /// A player changed pressure on atmos device.
    /// </summary>
    AtmosPressureChanged = 54,

    /// <summary>
    /// A player changed power on atmos device.
    /// </summary>
    AtmosPowerChanged = 55,

    /// <summary>
    /// A player changed transfer rate on atmos device.
    /// </summary>
    AtmosVolumeChanged = 56,

    /// <summary>
    /// A player changed filter on atmos device.
    /// </summary>
    AtmosFilterChanged = 57,

    /// <summary>
    /// A player changed ratio on atmos device.
    /// </summary>
    AtmosRatioChanged = 58,

    /// <summary>
    /// Field generator was toggled or lost field connections.
    /// </summary>
    FieldGeneration = 59,

    /// <summary>
    /// A player took ghost role.
    /// </summary>
    GhostRoleTaken = 60,

    /// <summary>
    /// OOC, IC, LOOC, etc.
    /// </summary>
    Chat = 61,

    /// <summary>
    /// A player performed some action. Like pressing eject and flash buttons on a trash bin, etc.
    /// </summary>
    Action = 62,

    /// <summary>
    /// A player used RCD.
    /// </summary>
    RCD = 63,

    /// <summary>
    /// Construction related interactions.
    /// </summary>
    Construction = 64,

    /// <summary>
    /// Triggers related interactions.
    /// </summary>
    Trigger = 65,

    /// <summary>
    /// A player tries to anchor or anchored an entity.
    /// </summary>
    Anchor = 66,

    /// <summary>
    /// A player unanchored an entity.
    /// </summary>
    Unanchor = 67,

    /// <summary>
    /// Emergency shuttle related interactions.
    /// </summary>
    EmergencyShuttle = 68,

    /// <summary>
    /// A player emagged an entity.
    /// </summary>
    Emag = 69,

    /// <summary>
    /// A player was gibbed.
    /// </summary>
    Gib = 70,

    /// <summary>
    /// Identity related interactions.
    /// </summary>
    Identity = 71,

    /// <summary>
    /// A player cut a cable.
    /// </summary>
    CableCut = 72,

    /// <summary>
    /// A player purchased something from the "store".
    /// </summary>
    StorePurchase = 73,

    /// <summary>
    /// A player edited a tile using some tool.
    /// </summary>
    LatticeCut = 74,

    /// <summary>
    /// A player is equipping something on an entity or stripping it from it.
    /// </summary>
    Stripping = 75,

    /// <summary>
    /// A player caused stamina damage or entered stamina crit.
    /// </summary>
    Stamina = 76,

    /// <summary>
    /// A player's actions caused an entity spawn.
    /// </summary>
    EntitySpawn = 77,

    /// <summary>
    /// Prayers and subtle messages.
    /// </summary>
    AdminMessage = 78,

    /// <summary>
    /// Anomaly related interactions.
    /// </summary>
    Anomaly = 79,

    /// <summary>
    /// Cutting, mending and pulsing of wires.
    /// </summary>
    WireHacking = 80,

    /// <summary>
    /// Entity was teleported.
    /// </summary>
    Teleport = 81,

    /// <summary>
    /// Entity was removed in a result of something.
    /// </summary>
    EntityDelete = 82,

    /// <summary>
    /// Voting related interactions.
    /// </summary>
    Vote = 83,

    /// <summary>
    /// Entity was configured.
    /// </summary>
    ItemConfigure = 84,

    /// <summary>
    /// Device linking related interactions.
    /// </summary>
    DeviceLinking = 85,

    /// <summary>
    /// Tiles related interactions.
    /// </summary>
    Tile = 86,

    /// <summary>
    /// A client has sent too many chat messages recently and is temporarily blocked from sending more.
    /// </summary>
    ChatRateLimited = 87,

    /// <summary>
    /// A player changed temperature on atmos device.
    /// </summary>
    AtmosTemperatureChanged = 88,

    /// <summary>
    /// Something was sent over device network. Like broadcast.
    /// </summary>
    DeviceNetwork = 89,

    /// <summary>
    /// A player had a refund at the "store".
    /// </summary>
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

    /// <summary>
    /// Storage & entity-storage related interactions
    /// </summary>
    Storage = 93,

    /// <summary>
    /// A player got hit by an explosion and was dealt damage.
    /// </summary>
    ExplosionHit = 94,

    /// <summary>
    /// A ghost warped to an entity through the ghost warp menu.
    /// </summary>
    GhostWarp = 95,

    /// <summary>
    /// A player interacted with a PDA or its cartridge component
    /// </summary>
    PdaInteract = 96,

    /// <summary>
    /// An atmos networked device (such as a vent or pump) has had its settings changed, usually through an air alarm
    /// </summary>
    AtmosDeviceSetting = 97,

    /// <summary>
    /// Commands related to admemes. Stuff like config changes, etc.
    /// </summary>
    AdminCommands = 98,

    /// <summary>
    /// A player was selected or assigned antag status
    /// </summary>
    AntagSelection = 99,

    /// <summary>
    /// Logs related to botany, such as planting and harvesting crops
    /// </summary>
    Botany = 100,
    /// <summary>
    /// Artifact node got activated.
    /// </summary>
    ArtifactNode = 101,

    /// <summary>
    /// Damaging grid collision has occurred.
    /// </summary>
    ShuttleImpact = 102
}
