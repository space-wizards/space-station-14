using Content.Shared.Cloning;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
///     Gamerule component for spawning a paradox clone antagonist.
/// </summary>
[RegisterComponent]
public sealed partial class ParadoxCloneRuleComponent : Component
{
    /// <summary>
    ///     Cloning settings to be used.
    /// </summary>
    [DataField]
    public ProtoId<CloningSettingsPrototype> Settings = "ParadoxCloningSettings";

    /// <summary>
    ///     Visual effect spawned when gibbing at round end.
    /// </summary>
    [DataField]
    public EntProtoId GibProto = "MobParadoxTimed";

    /// <summary>
    ///     AI-eye-like entity spawned for the paradox clone to choose their spawn location.
    /// </summary>
    [DataField]
    public EntProtoId GhostProto = "MobParadoxCloneGhost";

    /// <summary>
    ///     The action that is given to the paradox clone ghost so that it can materialize into its "real" body
    /// </summary>
    [DataField]
    public EntProtoId MaterializeAction = "ActionParadoxCloneMaterialize";

    /// <summary>
    ///     Entity of the original player.
    ///     Gets randomly chosen from all alive players if not specified.
    /// </summary>
    [DataField]
    public EntityUid? OriginalBody;

    /// <summary>
    ///     Mind entity of the original player.
    ///     Gets assigned when cloning.
    /// </summary>
    [DataField]
    public EntityUid? OriginalMind;

    /// <summary>
    ///     Whitelist for Objectives to be copied to the clone.
    /// </summary>
    [DataField]
    public EntityWhitelist? ObjectiveWhitelist;

    /// <summary>
    ///     Blacklist for Objectives to be copied to the clone.
    /// </summary>
    [DataField]
    public EntityWhitelist? ObjectiveBlacklist;

    /// <summary>
    /// Amount of time the paradox clone can spend wandering before being forced to spawn
    /// </summary>
    [DataField]
    public float WanderingTime = 70f;

    /// <summary>
    /// Amount of time the paradox clone can spend listening before being forced to spawn
    /// </summary>
    [DataField]
    public float ListenTime = 240f;
}
