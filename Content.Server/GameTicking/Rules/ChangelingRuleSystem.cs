using Content.Server.GameTicking.Rules.Components;
using Content.Server.Roles;
using Content.Shared.Changeling;

namespace Content.Server.GameTicking.Rules;

public sealed class ChangelingRuleSystem : GameRuleSystem<ChangelingRuleComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void OnGetBriefing(Entity<ChangelingRoleComponent> ent, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("changeling-briefing"));
    }

}
