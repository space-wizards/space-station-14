using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class ThiefRuleSystem : GameRuleSystem<ThiefRuleComponent>
{
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThiefRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);

        SubscribeLocalEvent<ThiefRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void AfterAntagSelected(Entity<ThiefRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!_mindSystem.TryGetMind(args.EntityUid, out var mindId, out var mind))
            return;

        //Generate objectives
        _antag.SendBriefing(args.EntityUid, MakeBriefing(mind, mindId, args.EntityUid), null, null);
    }

    //Add mind briefing
    private void OnGetBriefing(Entity<ThiefRoleComponent> thief, ref GetBriefingEvent args)
    {
        if (!_mindSystem.TryGetMind(thief.Owner, out var mindId, out var mind))
            return;

        args.Append(MakeBriefing(mind,mindId,thief.Owner));
    }

    private string MakeBriefing(MindComponent mind, EntityUid mindId, EntityUid thief)
    {
        var isHuman = HasComp<HumanoidAppearanceComponent>(thief);
        var briefing = isHuman
            ? Loc.GetString("thief-role-greeting-human")
            : Loc.GetString("thief-role-greeting-animal");

        // Get a summary of their objectives
        List<string> objectives = new List<string>();

        foreach (var objective in mind.Objectives)
        {
            var info = _objectives.GetInfo(objective, mindId, mind);
            if (info == null)
                continue;

            objectives.Add("- " + info.Value.Title);
        }

        briefing += "\n" + Loc.GetString("generic-role-objectives", ("objectives", string.Join("\n", objectives)));

        briefing += "\n \n" + Loc.GetString("thief-role-greeting-equipment") + "\n";
        return briefing;
    }
}
