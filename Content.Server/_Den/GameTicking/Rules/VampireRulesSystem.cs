using Content.Server._Den.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Roles;
using Content.Shared._Den.Vampire;


namespace Content.Server._Den.GameTicking.Rules;

/// <summary>
/// Game rule system for Vampire
/// </summary>
public sealed class VampireRuleSystem : GameRuleSystem<VampireRuleComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void OnGetBriefing(Entity<VampireRoleComponent> ent, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("vampire-briefing"));
    }
}
