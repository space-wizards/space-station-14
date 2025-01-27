using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server._Goobstation.Objectives.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Objectives.Components;
using Content.Server.Roles;
using Content.Shared.Heretic;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using System.Text;

namespace Content.Server.GameTicking.Rules;

public sealed partial class HereticRuleSystem : GameRuleSystem<HereticRuleComponent>
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly ObjectivesSystem _objective = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;

    public readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/Ambience/Antag/Heretic/heretic_gain.ogg");

    [ValidatePrototypeId<NpcFactionPrototype>] public readonly ProtoId<NpcFactionPrototype> HereticFactionId = "Heretic";

    [ValidatePrototypeId<NpcFactionPrototype>] public readonly ProtoId<NpcFactionPrototype> NanotrasenFactionId = "NanoTrasen";

    [ValidatePrototypeId<CurrencyPrototype>] public readonly ProtoId<CurrencyPrototype> Currency = "KnowledgePoint";

    [ValidatePrototypeId<EntityPrototype>] static EntProtoId mindRole = "MindRoleHeretic";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelect);
        SubscribeLocalEvent<HereticRuleComponent, ObjectivesTextPrependEvent>(OnTextPrepend);
    }

    private void OnAntagSelect(Entity<HereticRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        TryMakeHeretic(args.EntityUid, ent.Comp);

        for (int i = 0; i < _rand.Next(6, 12); i++)
            if (TryFindRandomTile(out var _, out var _, out var _, out var coords))
                Spawn("EldritchInfluence", coords);
    }

    public bool TryMakeHeretic(EntityUid target, HereticRuleComponent rule)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return false;

        _role.MindAddRole(mindId, mindRole.Id, mind, true);

        // briefing
        if (HasComp<MetaDataComponent>(target))
        {
            var briefingShort = Loc.GetString("heretic-role-greeting-short");

            _antag.SendBriefing(target, Loc.GetString("heretic-role-greeting-fluff"), Color.MediumPurple, null);
            _antag.SendBriefing(target, Loc.GetString("heretic-role-greeting"), Color.Red, BriefingSound);

            if (_role.MindHasRole<HereticRoleComponent>(mindId, out var mr))
                AddComp(mr.Value, new RoleBriefingComponent { Briefing = briefingShort }, overwrite: true);
        }
        _npcFaction.RemoveFaction(target, NanotrasenFactionId, false);
        _npcFaction.AddFaction(target, HereticFactionId);

        EnsureComp<HereticComponent>(target);

        // add store
        var store = EnsureComp<StoreComponent>(target);
        foreach (var category in rule.StoreCategories)
            store.Categories.Add(category);
        store.CurrencyWhitelist.Add(Currency);
        store.Balance.Add(Currency, 2);

        rule.Minds.Add(mindId);

        return true;
    }

    public void OnTextPrepend(Entity<HereticRuleComponent> ent, ref ObjectivesTextPrependEvent args)
    {
        var sb = new StringBuilder();

        var mostKnowledge = 0f;
        var mostKnowledgeName = string.Empty;

        foreach (var heretic in EntityQuery<HereticComponent>())
        {
            if (!_mind.TryGetMind(heretic.Owner, out var mindId, out var mind))
                continue;

            var name = _objective.GetTitle((mindId, mind), Name(heretic.Owner));
            if (_mind.TryGetObjectiveComp<HereticKnowledgeConditionComponent>(mindId, out var objective, mind))
            {
                if (objective.Researched > mostKnowledge)
                    mostKnowledge = objective.Researched;
                mostKnowledgeName = name;
            }

            var str = Loc.GetString($"roundend-prepend-heretic-ascension-{(heretic.Ascended ? "success" : "fail")}", ("name", name));
            sb.AppendLine(str);
        }

        sb.AppendLine("\n" + Loc.GetString("roundend-prepend-heretic-knowledge-named", ("name", mostKnowledgeName), ("number", mostKnowledge)));

        args.Text = sb.ToString();
    }
}
