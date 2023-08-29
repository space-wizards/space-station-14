using Content.Server.GameTicking.Rules;

namespace Content.Server.Revolutionary.Components;
/// <summary>
/// Given to heads at round start for Revs. Used for tracking if heads died or not.
/// </summary>
[RegisterComponent, Access(typeof(RevolutionaryRuleSystem))]
public sealed partial class CommandStaffComponent : Component
{
    /// <summary>
    /// Bool for making sure CheckCommandLose doesn't repeat over and over.
    /// </summary>
    [DataField("headsDied")]
    public bool HeadsDied = false;
}
