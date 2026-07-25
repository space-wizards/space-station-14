// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Antag;
using Content.Server.DeadSpace.Hooligan.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Roles;
using Content.Shared.DeadSpace.Hooligan.Roles;

namespace Content.Server.DeadSpace.Hooligan;

/// <summary>
/// Логика роли Хулигана. 
/// Показывает брифинг.
/// </summary>
public sealed class HooliganRuleSystem : GameRuleSystem<HooliganRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HooliganRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);
        SubscribeLocalEvent<HooliganRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void AfterAntagSelected(Entity<HooliganRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        _antag.SendBriefing(args.EntityUid, MakeBriefing(), null, ent.Comp.GreetSound);
    }

    private void OnGetBriefing(Entity<HooliganRoleComponent> role, ref GetBriefingEvent args)
    {
        args.Append(MakeBriefing());
    }

    private string MakeBriefing()
    {
        return Loc.GetString("hooligan-role-greeting");
    }
}
