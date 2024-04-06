namespace Content.Server.GameTicking.Replays;

/// <summary>
/// Used to identify the type of a replay event. <see cref="ReplayEvent"/>
/// </summary>
/// <remarks>
/// Generally, I would recommend not renaming any values, as the replay file contains string representations of these values.
/// Meaning that if you rename a value, it could brake compatibility with other tools. Adding new values is fine.
/// </remarks>
public enum ReplayEventType
{
    #region OOCPlayerRelated

    PlayerJoin,
    PlayerLeave,

    #endregion

    #region Gameflow

    GameRuleStarted,

    #endregion

    #region ICEvents

    CargoOrdered,
    CargoSold,
    MobCrit,
    MobDied,
    MobRevived,
    NukeArmed,
    NukeDetonated,
    NukeDefused,
    PowerEngineSpawned,
    ContainmentFieldDepowered,
    MobSlipped,
    MobStunned,
    StoreBought,
    Explosion,

    #endregion
}
