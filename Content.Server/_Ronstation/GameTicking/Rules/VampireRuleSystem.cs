using System.Diagnostics.CodeAnalysis;
using Content.Server._Ronstation.GameTicking.Rules.Components;
using Content.Server._Ronstation.Roles;
using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Popups;
using Content.Server.Preferences.Managers;
using Content.Server.Roles;
using Content.Server.Stunnable;
using Content.Shared._Ronstation.Vampire.Components;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Zombies;
using Robust.Server.Player;
using Robust.Shared.Utility;

namespace Content.Server._Ronstation.GameTicking.Rules;

public sealed class VampireRuleSystem : GameRuleSystem<VampireRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
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