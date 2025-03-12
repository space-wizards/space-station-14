using Content.Shared.Cloning;
using Content.Shared.Polymorph;
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
    ///     Visual effect to polymorph into on spawn.
    /// </summary>
    [DataField]
    public ProtoId<PolymorphPrototype>? Polymorph = "ParadoxClone";

    /// <summary>
    ///     Visual effect spawned when gibbing at round end.
    /// </summary>
    [DataField]
    public EntProtoId GibProto = "MobParadoxTimed";
}
