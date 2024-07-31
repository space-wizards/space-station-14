using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class ThiefRuleSystem : GameRuleSystem<ThiefRuleComponent>
{
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

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
        _antag.SendBriefing(args.EntityUid, MakeBriefing(args.EntityUid), null, null);
    }

    //Add mind briefing
    private void OnGetBriefing(Entity<ThiefRoleComponent> thief, ref GetBriefingEvent args)
    {
        if (!TryComp<MindComponent>(thief.Owner, out var mind) || mind.OwnedEntity == null)
            return;

        args.Append(MakeBriefing(mind.OwnedEntity.Value));
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
