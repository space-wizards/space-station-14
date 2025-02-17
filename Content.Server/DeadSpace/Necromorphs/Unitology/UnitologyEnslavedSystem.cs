// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Mind;
using Content.Server.Antag.Components;
using Content.Shared.DeadSpace.Necromorphs.Unitology.Components;
using Content.Server.Antag;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Roles;
using Content.Server.GameTicking.Rules.Components;

namespace Content.Server.DeadSpace.Necromorphs.Unitology;

public sealed class UnitologyEnslavedSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string UnitologyRule = "Unitology";

    [ValidatePrototypeId<AntagPrototype>]
    public const string UnitologyAntagRole = "UniEnslaved";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnitologyEnslavedComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, UnitologyEnslavedComponent comp, ComponentInit args)
    {
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return;

        var rule = _antag.ForceGetGameRuleEnt<UnitologyRuleComponent>(UnitologyRule);

        AntagSelectionDefinition? definition = rule.Comp.Definitions.FirstOrDefault(def =>
        def.PrefRoles.Contains(new ProtoId<AntagPrototype>(UnitologyAntagRole))
        );

        if (definition == null)
            definition = rule.Comp.Definitions.Last();

        _antag.MakeAntag(rule, mind.Session, definition.Value);

    }
}
