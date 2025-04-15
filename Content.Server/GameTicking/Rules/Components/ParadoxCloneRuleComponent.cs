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
    public ProtoId<CloningSettingsPrototype> Settings = "BaseClone";

    /// <summary>
    ///     Visual effect spawned when gibbing at round end.
    /// </summary>
    [DataField]
    public EntProtoId GibProto = "MobParadoxTimed";

    /// <summary>
    ///     Mind entity of the original player.
    ///     Gets assigned when cloning.
    /// </summary>
    [DataField]
    public EntityUid? Original;

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
}
