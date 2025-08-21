using Content.Server.Antag;
using Content.Server.Atmos.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Server.Vampire;
using Content.Shared.Alert;
using Content.Shared.Vampire.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Nutrition.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using System.Text;
using Content.Shared.Roles.Components;

namespace Content.Server.GameTicking.Rules;

public sealed partial class VampireRuleSystem : GameRuleSystem<VampireRuleComponent>
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly ObjectivesSystem _objective = default!;
    [Dependency] private readonly VampireSystem _vampire = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/Ambience/Antag/vampire_start.ogg");

    public readonly ProtoId<AntagPrototype> VampirePrototypeId = "Vampire";

    public readonly ProtoId<NpcFactionPrototype> ChangelingFactionId = "Vampire";

    public readonly ProtoId<NpcFactionPrototype> NanotrasenFactionId = "NanoTrasen";

    public readonly ProtoId<CurrencyPrototype> Currency = "BloodEssence";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireRuleComponent, AfterAntagEntitySelectedEvent>(OnSelectAntag);
        SubscribeLocalEvent<VampireRuleComponent, ObjectivesTextPrependEvent>(OnTextPrepend);
    }

    private void OnSelectAntag(EntityUid mindId, VampireRuleComponent comp, ref AfterAntagEntitySelectedEvent args)
    {
        MakeVampire(args.EntityUid, comp);
    }
    
    public bool MakeVampire(EntityUid target, VampireRuleComponent rule)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return false;

        // briefing
        if (TryComp<MetaDataComponent>(target, out var metaData))
        {
            var briefing = Loc.GetString("vampire-role-greeting", ("name", metaData?.EntityName ?? "Unknown"));

            _antag.SendBriefing(target, MakeBriefing(target), Color.Yellow, BriefingSound);
            _role.MindHasRole<VampireRoleComponent>(mindId, out var vampireRole);
            _role.MindHasRole<RoleBriefingComponent>(mindId, out var briefingComp);
            if (vampireRole is not null && briefingComp is null)
            {
                AddComp<RoleBriefingComponent>(vampireRole.Value.Owner);
                Comp<RoleBriefingComponent>(vampireRole.Value.Owner).Briefing = briefing;
            }
        }
        // vampire stuff
        _npcFaction.RemoveFaction(target, NanotrasenFactionId, false);
        _npcFaction.AddFaction(target, ChangelingFactionId);

        // make sure it's initial chems are set to max
        var vampireComponent = EnsureComp<VampireComponent>(target);
        EnsureComp<VampireIconComponent>(target);
        EnsureComp<VampireSpaceDamageComponent>(target);
        var vampireAlertComponent = EnsureComp<VampireAlertComponent>(target);
        var interfaceComponent = EnsureComp<UserInterfaceComponent>(target);
        
        if (HasComp<UserInterfaceComponent>(target))
            _uiSystem.SetUiState(target, VampireMutationUiKey.Key, new VampireMutationBoundUserInterfaceState(vampireComponent.VampireMutations, vampireComponent.CurrentMutation));
        
        var vampire = new Entity<VampireComponent>(target, vampireComponent);
        
        RemComp<PerishableComponent>(vampire);
        RemComp<BarotraumaComponent>(vampire);
        RemComp<ThirstComponent>(vampire);

        vampireComponent.Balance = new() { { VampireComponent.CurrencyProto, 0 } };

        rule.VampireMinds.Add(mindId);
        
        _vampire.AddStartingAbilities(vampire);
        _vampire.MakeVulnerableToHoly(vampire);
        _alerts.ShowAlert(vampire, vampireAlertComponent.BloodAlert);
        _alerts.ShowAlert(vampire, vampireAlertComponent.StellarWeaknessAlert);
        
        Random random = new Random();

        foreach (var objective in rule.BaseObjectives)
            _mind.TryAddObjective(mindId, mind, objective);
            
        if (rule.EscapeObjectives.Count > 0)
        {
            var randomEscapeObjective = rule.EscapeObjectives[random.Next(rule.EscapeObjectives.Count)];
            _mind.TryAddObjective(mindId, mind, randomEscapeObjective);
        }
        
        if (rule.StealObjectives.Count > 0)
        {
            var randomEscapeObjective = rule.StealObjectives[random.Next(rule.StealObjectives.Count)];
            _mind.TryAddObjective(mindId, mind, randomEscapeObjective);
        }

        return true;
    }
    
    private string MakeBriefing(EntityUid ent)
    {
        if (TryComp<MetaDataComponent>(ent, out var metaData))
        {
            var briefing = Loc.GetString("vampire-role-greeting", ("name", metaData?.EntityName ?? "Unknown"));
            
            return briefing;
        }
        
        return "";
    }

    private void OnTextPrepend(EntityUid uid, VampireRuleComponent comp, ref ObjectivesTextPrependEvent args)
    {
        var mostDrainedName = string.Empty;
        var mostDrained = 0f;

        foreach (var vamp in EntityQuery<VampireComponent>())
        {
            if (!_mind.TryGetMind(vamp.Owner, out var mindId, out var mind))
                continue;

            if (!TryComp<MetaDataComponent>(vamp.Owner, out var metaData))
                continue;

            if (vamp.TotalBloodDrank > mostDrained)
            {
                mostDrained = vamp.TotalBloodDrank;
                mostDrainedName = _objective.GetTitle((mindId, mind), metaData.EntityName);
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine(Loc.GetString($"roundend-prepend-vampire-drained{(!string.IsNullOrWhiteSpace(mostDrainedName) ? "-named" : "")}", ("name", mostDrainedName), ("number", mostDrained)));

        args.Text = sb.ToString();
    }
}
