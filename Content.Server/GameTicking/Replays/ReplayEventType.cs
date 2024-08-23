namespace Content.Server.GameTicking.Replays;

/// <summary>
/// Used to identify the type of a <see cref="ReplayEvent"/> for external tools to parse the replay
/// </summary>
/// <remarks>
/// Do not rename any of these values, as they are used to identify the type of event in the replay file and renaming them will break compatibility with external tools
/// Adding new values is fine.
/// </remarks>
public enum ReplayEventType : byte
{
    #region Out of character events

    PlayerJoin,
    PlayerLeave,

    #endregion

    #region Gameflow

    GameRuleStarted,
    RoundEnded,

    #endregion

    #region In character events

    CargoProductOrdered,
    CargoProductSold,

    MobCrit,
    MobDied,
    MobRevived,
    MobSlipped,
    MobStunned,

    NukeArmed,
    NukeDetonated,
    NukeDefused,

    PowerEngineSpawned, // Tesla or Singularity
    ContainmentFieldEngaged,
    ContainmentFieldDisengaged,

    ItemBoughtFromStore, // Item bought from an (for example) uplink.

    Explosion,

    AnnouncementSent, // Comms console
    ChatMessageSent,
    AlertLevelChanged,

    TechnologyUnlocked,

    EvacuationShuttleCalled,
    EvacuationShuttleDocked,
    EvacuationShuttleDeparted,
    EvacuationShuttleRecalled,
    #endregion
}
