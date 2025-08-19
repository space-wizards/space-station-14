using Content.Shared.Hands.EntitySystems;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Roles;
using Content.Shared.Humanoid;
using Content.Shared.Roles.Components;

namespace Content.Server.GameTicking.Rules;

public sealed class ThiefRuleSystem : GameRuleSystem<ThiefRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThiefRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);

        SubscribeLocalEvent<ThiefRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    // Greeting upon thief activation
    private void AfterAntagSelected(Entity<ThiefRuleComponent> mindId, ref AfterAntagEntitySelectedEvent args)
    {
        var ent = args.EntityUid;
        
        _antag.SendBriefing(ent, MakeBriefing(ent), null, null);
    }

    // Character screen briefing
    private void OnGetBriefing(Entity<ThiefRoleComponent> role, ref GetBriefingEvent args)
    {
        var ent = args.Mind.Comp.OwnedEntity;
        
        int hands = 0;
        
        if (ent == null)
            return;
        
        foreach (var hand in _hands.EnumerateHands(ent.Value))
            hands += 1;
            
        if (hands == 0)
            return;
        
        Logger.Warning($"Hands count: {hands}");
            
        args.Append(MakeBriefing(ent.Value));
    }

    private string MakeBriefing(EntityUid ent)
    {
        var isHuman = HasComp<HumanoidAppearanceComponent>(ent);
        var briefing = isHuman
            ? Loc.GetString("thief-role-greeting-human")
            : Loc.GetString("thief-role-greeting-animal");

        if (isHuman)
            briefing += "\n \n" + Loc.GetString("thief-role-greeting-equipment") + "\n";

        return briefing;
    }
}
