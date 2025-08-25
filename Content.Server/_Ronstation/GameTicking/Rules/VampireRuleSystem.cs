using Content.Server.Antag;
using Content.Server._Ronstation.GameTicking.Rules.Components;
using Content.Server._Ronstation.Roles;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Roles;
using Content.Shared.Humanoid;
using Content.Shared.Roles.Components;
using Content.Shared._Ronstation.Vampire.Components;


namespace Content.Server._Ronstation.GameTicking.Rules;

public sealed class VampireRuleSystem : GameRuleSystem<VampireRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antagSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagEntitySelected);
        SubscribeLocalEvent<VampireRuleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void AfterAntagEntitySelected(Entity<VampireRuleComponent> mindId, ref AfterAntagEntitySelectedEvent args)
    {
        var ent = args.EntityUid;
        _antagSystem.SendBriefing(ent, MakeBriefing(ent), null, null);
    }

    // Character screen briefing
    private void OnGetBriefing(Entity<VampireRuleComponent> role, ref GetBriefingEvent args)
    {
        var ent = args.Mind.Comp.OwnedEntity;

        if (ent is null)
            return;
        args.Append(MakeBriefing(ent.Value));
    }

    private string MakeBriefing(EntityUid ent)
    {
        var briefing = Loc.GetString("vampire-role-greeting");

        return briefing;
    }
}