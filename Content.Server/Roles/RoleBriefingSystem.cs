namespace Content.Server.Roles;

public sealed class RoleBriefingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoleBriefingComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void OnGetBriefing(EntityUid uid, RoleBriefingComponent comp, ref GetBriefingEvent args)
    {
        if (args.Briefing == null)
        {
            // no previous briefing so just set it
            args.Briefing = comp.Briefing;
        }
        else
        {
            // there is a previous briefing so append to it
            args.Briefing += "\n" + comp.Briefing;
        }
    }
}
