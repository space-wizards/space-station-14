using Content.Server.GameTicking.Rules;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Keeps track of who is being terminated and who the terminator/s is/are.
/// </summary>
[RegisterComponent, Access(typeof(TerminatorRuleSystem))]
public sealed partial class TerminatorRuleComponent : Component
{
    /// <summary>
    /// The target mind being terminated.
    /// </summary>
    [DataField("target")]
    public EntityUid Target;

    /// <summary>
    /// Minds of every terminator after this target.
    /// </summary>
    [DataField("minds")]
    public List<EntityUid> Minds;
}
