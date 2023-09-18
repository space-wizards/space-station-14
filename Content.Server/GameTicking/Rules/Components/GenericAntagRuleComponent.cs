using Content.Server.GameTicking.Rules;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Gamerule for simple antagonists that have fixed objectives.
/// </summary>
[RegisterComponent, Access(typeof(GenericAntagRuleSystem))]
public sealed partial class GenericAntagRuleComponent : Component
{
    /// <summary>
    /// All antag minds that are using this rule.
    /// </summary>
    [DataField]
    public List<EntityUid> Minds = new();

    /// <summary>
    /// Locale id for the name of the antag used by the roundend summary.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string AgentName = string.Empty;

    /// <summary>
    /// List of objective entity prototypes to add to the antag when a mind is added.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Objectives = new();
}
