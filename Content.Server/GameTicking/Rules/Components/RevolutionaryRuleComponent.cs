using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Component for the RevolutionaryRuleSystem that stores info about winning/losing, player counts required for starting, as well as prototypes for Revolutionaries and their gear.
/// </summary>
[RegisterComponent, Access(typeof(RevolutionaryRuleSystem))]
public sealed partial class RevolutionaryRuleComponent : Component
{
    /// <summary>
    /// Было ли уже запущено голосование за завершение раунда.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool VoteStarted = false;

    /// <summary>
    /// When will the percentage of revolutionaries and the living command be checked.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Check;

    /// <summary>
    /// The amount of time between each check.
    /// </summary>
    [DataField]
    public TimeSpan TimerWait = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The time it takes after the last head is killed for the shuttle to arrive.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ShuttleCallTime = TimeSpan.FromMinutes(5);

    // DS14-start
    /// <summary>
    /// Current living revolutionary body for each mind. Bodies without a current mind are never counted.
    /// </summary>
    public readonly Dictionary<EntityUid, EntityUid> RevolutionaryMinds = new();

    /// <summary>
    /// Reverse lookup used to remove a body without scanning the full roster.
    /// </summary>
    public readonly Dictionary<EntityUid, EntityUid> RevolutionaryBodies = new();

    /// <summary>
    /// Living head revolutionary minds. A body transfer keeps one entry for the same mind.
    /// </summary>
    public readonly HashSet<EntityUid> HeadRevolutionaryMinds = new();

    /// <summary>
    /// Current command body for each command mind.
    /// </summary>
    public readonly Dictionary<EntityUid, EntityUid> CommandMinds = new();

    /// <summary>
    /// Reverse command lookup used to prevent corpse/clone duplication.
    /// </summary>
    public readonly Dictionary<EntityUid, EntityUid> CommandBodies = new();

    /// <summary>
    /// Command identities whose current or last tracked body is dead.
    /// </summary>
    public readonly HashSet<EntityUid> DeadCommandMinds = new();

    /// <summary>
    /// Regular revolutionaries awaiting batched deconversion after the last head dies.
    /// </summary>
    public readonly Queue<EntityUid> PendingDeconversions = new();

    public readonly HashSet<EntityUid> PendingDeconversionSet = new();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int DeconversionBatchSize = 8;

    [ViewVariables(VVAccess.ReadOnly)]
    public float CommandDeadFraction;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MassacreCommandFraction = 0.375f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float VictoryCommandFraction = 0.75f;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool HadHeadRevolutionaries;

    public bool HeadCheckPending;
    public bool ProgressCheckPending;
    public bool DefeatHandled;
    public bool SupplyRequested;

    public RevolutionaryStage Stage = RevolutionaryStage.Initial;
    // DS14-end
}

public enum RevolutionaryStage : byte
{
    Initial,
    Massacre
}
