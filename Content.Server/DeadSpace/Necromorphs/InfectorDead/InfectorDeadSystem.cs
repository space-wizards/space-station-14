// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DoAfter;
using Content.Shared.Mobs.Systems;
using Content.Server.DeadSpace.Necromorphs.InfectionDead;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.DeadSpace.Necromorphs.InfectorDead;
using Content.Shared.Body.Components;
using Content.Server.Popups;
using Content.Server.Administration.Systems;
using Content.Shared.Actions;
using Content.Shared.Verbs;
using Content.Shared.Hands.Components;
using Robust.Shared.Utility;
using Content.Shared.Storage;
using System.Linq;
using Content.Shared.Database;
using Content.Server.Kitchen.Components;
using Content.Shared.Kitchen.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Server.Hands.Systems;

namespace Content.Server.DeadSpace.InfectorDead;

public sealed partial class InfectorDeadSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NecromorfSystem _infection = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly HandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InfectorDeadComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<InfectorDeadComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<InfectorDeadComponent, InfectionNecroActionEvent>(OnInfect);
        SubscribeLocalEvent<InfectorDeadComponent, InfectorDeadDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<InfectorDeadComponent, GetVerbsEvent<Verb>>(DoSetVerbs);
    }
    private void OnComponentInit(EntityUid uid, InfectorDeadComponent component, ComponentInit args)
    {
        _actions.AddAction(uid, ref component.ActionInfectionNecroEntity, component.ActionInfectionNecro, uid);
    }
    private void OnShutdown(EntityUid uid, InfectorDeadComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionInfectionNecroEntity);
    }
    private void DoSetVerbs(EntityUid uid, InfectorDeadComponent component, GetVerbsEvent<Verb> args)
    {
        if (!_mobState.IsDead(uid))
            return;

        if (!TryComp<HandsComponent>(args.User, out var handsComp))
            return;

        if (_hands.GetActiveItem(args.User) is not { } item)
            return;

        if (HasComp<SharpComponent>(item))
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("Вырезать железы заразителя"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
                Act = () => SpawnGlands(uid, component),
                Impact = LogImpact.High
            });
        }
    }
    private void SpawnGlands(EntityUid uid, InfectorDeadComponent component)
    {
        if (!component.HasGland)
            return;

        if (!TryComp<NecromorfComponent>(uid, out var necro))
            return;

        var spawns = EntitySpawnCollection.GetSpawns(component.SpawnedEntities).Cast<string?>().ToList();

        var ents = EntityManager.SpawnEntities(_transform.GetMapCoordinates(uid), spawns);

        foreach (var ent in ents)
        {
            if (!TryComp<ExtractableComponent>(ent, out var extractableComp))
                continue;

            var juice = extractableComp.JuiceSolution;
            if (juice == null)
                continue;

            foreach (var reagent in juice.Contents)
            {
                if (reagent.Reagent.Prototype != "ExtractInfectorDead")
                    continue;

                List<ReagentData> reagentData = reagent.Reagent.EnsureReagentData();
                reagentData.RemoveAll(x => x is InfectionDeadStrainData);
                reagentData.Add(necro.StrainData);
            }

            if (!TryComp<SolutionContainerManagerComponent>(ent, out var container))
                continue;

            if (!_solutionContainer.TryGetSolution(ent, "food", out var entity, out var solution))
                continue;

            foreach (var reagent in solution.Contents)
            {
                if (reagent.Reagent.Prototype != "ExtractInfectorDead")
                    continue;

                List<ReagentData> reagentData = reagent.Reagent.EnsureReagentData();
                reagentData.RemoveAll(x => x is InfectionDeadStrainData);
                reagentData.Add(necro.StrainData);
            }
        }

        component.HasGland = false;
    }
    private void OnInfect(EntityUid uid, InfectorDeadComponent component, InfectionNecroActionEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == uid)
            return;

        var target = args.Target;

        if (!HasComp<BodyComponent>(target))
            return;

        if (HasComp<NecromorfComponent>(target))
        {
            component.Duration = component.HealDuration;
            BeginInfected(uid, target, component);

            args.Handled = true;
            return;
        }

        if (!component.HasGland)
        {
            _popup.PopupEntity(Loc.GetString("У вас нет желёз!"), uid, uid);
            return;
        }

        if (!_infection.IsInfectionPossible(target))
        {
            _popup.PopupEntity(Loc.GetString("Эту жертву невозможно заразить!"), uid, uid);
            return;
        }


        if (!_mobState.IsDead(target))
        {
            _popup.PopupEntity(Loc.GetString("Жертва должна быть мертва!"), uid, uid);
            return;
        }

        component.Duration = component.InfectedDuration;
        BeginInfected(uid, target, component);

        args.Handled = true;
    }

    private void BeginInfected(EntityUid uid, EntityUid target, InfectorDeadComponent component)
    {
        var searchDoAfter = new DoAfterArgs(EntityManager, uid, component.Duration, new InfectorDeadDoAfterEvent(), uid, target: target)
        {
            DistanceThreshold = 2
        };

        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;
    }

    private void OnDoAfter(EntityUid uid, InfectorDeadComponent component, InfectorDeadDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (HasComp<NecromorfComponent>(args.Args.Target.Value))
            _rejuvenate.PerformRejuvenate(args.Args.Target.Value);

        if (!_mobState.IsDead(args.Args.Target.Value))
            return;

        if (!TryComp<NecromorfComponent>(uid, out var necro))
            return;

        InfectionDeadComponent infection = new InfectionDeadComponent(necro.StrainData);
        AddComp(args.Args.Target.Value, infection);

        args.Handled = true;
    }
}
