using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Xenoborgs.Components;

namespace Content.Server.GameTicking.Rules;

public sealed class XenoborgsRuleSystem : GameRuleSystem<XenoborgsRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XenoborgsRuleComponent, AfterAntagEntitySelectedEvent>(OnAfterAntagEntSelected);
    }

    protected override void Started(EntityUid uid,
        XenoborgsRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {

    }

    private void OnAfterAntagEntSelected(Entity<XenoborgsRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (TryComp<XenoborgComponent>(args.EntityUid, out _))
        {
            _antag.SendBriefing(args.Session,
                Loc.GetString("xenoborgs-welcome"),
                Color.BlueViolet,
                ent.Comp.GreetSoundNotification);
        }
        else if (TryComp<MothershipCoreComponent>(args.EntityUid, out _))
        {
            _antag.SendBriefing(args.Session,
                Loc.GetString("mothership-welcome"),
                Color.BlueViolet,
                ent.Comp.GreetSoundNotification);
        }
    }

}
