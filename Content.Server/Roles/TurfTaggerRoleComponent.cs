using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Roles;

namespace Content.Server.Roles;

[RegisterComponent, ExclusiveAntagonist]
public sealed partial class TurfTaggerRoleComponent : AntagonistRoleComponent
{
    /// <summary>
    /// The turf war game rule that created this turf tagger.
    /// </summary>
    [DataField]
    public Entity<TurfWarRuleComponent> Rule;

    /// <summary>
    /// Department this player is tagging for.
    /// </summary>
    [DataField]
    public string Department = string.Empty;

    public TurfTaggerRoleComponent(Entity<TurfWarRuleComponent> rule, string department)
    {
        PrototypeId = rule.Comp.Antag;
        Rule = rule;
        Department = department;
    }
}
