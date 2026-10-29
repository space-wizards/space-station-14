using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;

namespace Content.Shared.GameTicking.Rules;

/// <summary>
/// Where all the main stuff for Revolutionaries happens (Assigning Head Revs, Command on station, and checking for the game to end.)
/// </summary>
public abstract partial class RevolutionaryRuleSystem : GameRuleSystem<RevolutionaryRuleComponent>
{
    [SubscribeLocalEvent]
    private void OnGetBriefing(Entity<RevolutionaryRoleComponent> comp, ref GetBriefingEvent args)
    {
        var ent = args.Mind.Comp.OwnedEntity;
        var head = HasComp<HeadRevolutionaryComponent>(ent);
        args.Append(Loc.GetString(head ? "head-rev-briefing" : "rev-briefing"));
    }
}
