using Content.Server.GameTicking.Rules.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GenericAntag;

/// <summary>
/// Added to a mob to make it a generic antagonist where all its objectives are fixed.
/// This is unlike say traitor where it gets objectives picked randomly using difficulty.
/// </summary>
/// <remarks>
/// A GenericAntag is not necessarily an antagonist, that depends on the roles you do or do not add after.
/// </remarks>
[RegisterComponent, Access(typeof(GenericAntagSystem))]
public sealed partial class GenericAntagComponent : Component
{
    /// <summary>
    /// Gamerule to start when a mind is added.
    /// This must have <see cref="GenericAntagRuleComponent"/> or it will not work.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId Rule = string.Empty;

    /// <summary>
    /// The rule that's been spawned.
    /// Used to prevent spawning multiple rules.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? RuleEntity;
}
