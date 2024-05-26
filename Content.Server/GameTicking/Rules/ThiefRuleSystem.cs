using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class ThiefRuleSystem : GameRuleSystem<ThiefRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThiefRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);
    }

    private void AfterAntagSelected(Entity<ThiefRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!_mindSystem.TryGetMind(args.EntityUid, out var mindId, out var mind))
            return;

        var comp = Comp<ThiefRoleComponent>(mindId);
        comp.Briefing = MakeBriefing(args.EntityUid);
        Dirty(mindId, comp);
    }

    private string MakeBriefing(EntityUid thief)
    {
        var isHuman = HasComp<HumanoidAppearanceComponent>(thief);
        var briefing = isHuman
            ? Loc.GetString("thief-role-greeting-human")
            : Loc.GetString("thief-role-greeting-animal");

        briefing += "\n \n" + Loc.GetString("thief-role-greeting-equipment") + "\n";
        return briefing;
    }
}
