using Content.Server.GameTicking.Rules;
using Content.Server.Roles;

namespace Content.Server.Dragon;

// TODO qwerltaz: add station direction to briefing.
public sealed class DragonRuleSystem : GameRuleSystem<DragonRuleComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DragonRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void OnGetBriefing(Entity<DragonRoleComponent> dragon, ref GetBriefingEvent args)
    {
        args.Briefing = Loc.GetString("dragon-role-briefing", ("direction", DirectionToStation(dragon)));
    }

    private string DirectionToStation(Entity<DragonRoleComponent> dragon)
    {
        // TODO qwerltaz: implement this
        return "north";
    }
}
