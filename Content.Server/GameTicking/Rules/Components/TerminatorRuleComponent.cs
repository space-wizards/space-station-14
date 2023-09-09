using Content.Server.GameTicking.Rules;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Keeps track of who the terminator/s is/are.
/// </summary>
[RegisterComponent, Access(typeof(TerminatorRuleSystem))]
public sealed partial class TerminatorRuleComponent : Component
{
    /// <summary>
    /// Minds of every terminator after this target.
    /// </summary>
    [DataField("minds")]
    public List<EntityUid> Minds = new();
}
