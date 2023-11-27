using System.Diagnostics.CodeAnalysis;
using System.Text;
using Content.Shared.Chemistry.Containers.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solutions;
using Content.Shared.Chemistry.Solutions.Components;
using Content.Shared.Chemistry.Solutions.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.Containers.EntitySystems;

/// <summary>
/// Part of Chemistry system deal with SolutionContainers
/// </summary>
[UsedImplicitly]
public abstract partial class SharedSolutionContainerSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly ExamineSystemShared ExamineSystem = default!;
    [Dependency] protected readonly SharedAppearanceSystem AppearanceSystem = default!;
    [Dependency] protected readonly SolutionSystem SolutionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeRelays();

        SubscribeLocalEvent<SolutionContainerComponent, ComponentGetState>(OnComponentGetState);
        SubscribeLocalEvent<SolutionContainerComponent, ComponentHandleState>(OnComponentHandleState);
        SubscribeLocalEvent<ContainerSolutionComponent, ComponentGetState>(OnComponentGetState);
        SubscribeLocalEvent<ContainerSolutionComponent, ComponentHandleState>(OnComponentHandleState);

        SubscribeLocalEvent<ExaminableSolutionComponent, ExaminedEvent>(OnExamineSolution);
        SubscribeLocalEvent<ExaminableSolutionComponent, GetVerbsEvent<ExamineVerb>>(OnSolutionExaminableVerb);
    }


    public bool TryGetSolution(Entity<SolutionContainerComponent?> container, string? name, [MaybeNullWhen(false)] out Entity<SolutionComponent> entity, [MaybeNullWhen(false)] out Solution solution)
    {
        entity = default!;
        if (name is null)
            entity.Owner = container;
        else if (!Resolve(container, ref container.Comp, logMissing: false) || !container.Comp.Solutions.TryGetValue(name, out entity.Owner))
        {
            solution = null;
            return false;
        }

        if (!TryComp(entity, out entity.Comp!))
        {
            solution = null;
            return false;
        }

        solution = entity.Comp.Solution;
        return true;
    }

    public IEnumerable<(string? Name, Entity<SolutionComponent> Solution)> EnumerateSolutions(Entity<SolutionContainerComponent?> container, bool includeSelf = true)
    {
        if (includeSelf && TryComp(container, out SolutionComponent? solutionComp))
            yield return (null, (container.Owner, solutionComp));

        if (!Resolve(container, ref container.Comp, logMissing: false))
            yield break;

        foreach (var (name, solution) in EnumerateSolutions(container.Comp))
        {
            yield return (name, solution);
        }
    }

    public IEnumerable<(string Name, Entity<SolutionComponent> Solution)> EnumerateSolutions(SolutionContainerComponent container)
    {
        foreach (var (name, solutionId) in container.Solutions)
        {
            if (TryComp(solutionId, out SolutionComponent? solutionComp))
                yield return (name, (solutionId, solutionComp));
        }
    }


    protected void UpdateAppearance(Entity<AppearanceComponent?> container, Entity<SolutionComponent, ContainerSolutionComponent> soln)
    {
        var (uid, appearanceComponent) = container;
        if (!Resolve(uid, ref appearanceComponent, logMissing: false))
            return;

        var (_, comp, relation) = soln;
        var solution = comp.Solution;

        AppearanceSystem.SetData(uid, SolutionContainerVisuals.FillFraction, solution.FillFraction, appearanceComponent);
        AppearanceSystem.SetData(uid, SolutionContainerVisuals.Color, solution.GetColor(PrototypeManager), appearanceComponent);
        AppearanceSystem.SetData(uid, SolutionContainerVisuals.SolutionName, relation.Name, appearanceComponent);

        if (solution.GetPrimaryReagentId() is { } reagent)
            AppearanceSystem.SetData(uid, SolutionContainerVisuals.BaseOverride, reagent.ToString(), appearanceComponent);
        else
            AppearanceSystem.SetData(uid, SolutionContainerVisuals.BaseOverride, string.Empty, appearanceComponent);
    }


    public FixedPoint2 GetTotalPrototypeQuantity(EntityUid owner, string reagentId)
    {
        var reagentQuantity = FixedPoint2.New(0);
        if (EntityManager.EntityExists(owner)
            && EntityManager.TryGetComponent(owner, out SolutionContainerComponent? managerComponent))
        {
            foreach (var (_, soln) in EnumerateSolutions((owner, managerComponent)))
            {
                var solution = soln.Comp.Solution;
                reagentQuantity += solution.GetTotalPrototypeQuantity(reagentId);
            }
        }

        return reagentQuantity;
    }

    #region Event Handlers

    private void OnComponentGetState(EntityUid uid, SolutionContainerComponent comp, ComponentGetState args)
    {
        if (comp.Solutions is not { Count: > 0 } solutions)
        {
            args.State = new SolutionContainerState(null);
            return;
        }

        var netSolutions = new List<(string Name, NetEntity Solution)>(solutions.Count);
        foreach (var (name, solution) in solutions)
        {
            if (TryGetNetEntity(solution, out var netSolution))
                netSolutions.Add((name, netSolution.Value));
        }
        args.State = new SolutionContainerState(netSolutions);
    }

    private void OnComponentHandleState(EntityUid uid, SolutionContainerComponent comp, ComponentHandleState args)
    {
        if (args.Current is not SolutionContainerState state)
            return;

        comp.Solutions.Clear();
        if (state.Solutions is not { Count: > 0 } solutions)
            return;

        foreach (var (name, netSolution) in state.Solutions)
        {
            if (TryGetEntity(netSolution, out var solution))
                comp.Solutions.Add(name, solution.Value);
        }
    }

    private void OnComponentGetState(EntityUid uid, ContainerSolutionComponent comp, ComponentGetState args)
    {
        args.State = new ContainerSolutionState(GetNetEntity(comp.Container), comp.Name);
    }

    private void OnComponentHandleState(EntityUid uid, ContainerSolutionComponent comp, ComponentHandleState args)
    {
        if (args.Current is not ContainerSolutionState state)
            return;

        comp.Container = GetEntity(state.Container);
        comp.Name = state.Name;
    }


    private void OnExamineSolution(EntityUid uid, ExaminableSolutionComponent examinableComponent, ExaminedEvent args)
    {
        if (!TryGetSolution(uid, examinableComponent.Solution, out _, out var solution))
        {
            return;
        }

        var primaryReagent = solution.GetPrimaryReagentId();

        if (string.IsNullOrEmpty(primaryReagent?.Prototype))
        {
            args.PushText(Loc.GetString("shared-solution-container-component-on-examine-empty-container"));
            return;
        }

        if (!PrototypeManager.TryIndex(primaryReagent.Value.Prototype, out ReagentPrototype? primary))
        {
            Log.Error($"{nameof(Solution)} could not find the prototype associated with {primaryReagent}.");
            return;
        }

        var colorHex = solution.GetColor(PrototypeManager)
            .ToHexNoAlpha(); //TODO: If the chem has a dark color, the examine text becomes black on a black background, which is unreadable.
        var messageString = "shared-solution-container-component-on-examine-main-text";

        args.PushMarkup(Loc.GetString(messageString,
            ("color", colorHex),
            ("wordedAmount", Loc.GetString(solution.Contents.Count == 1
                ? "shared-solution-container-component-on-examine-worded-amount-one-reagent"
                : "shared-solution-container-component-on-examine-worded-amount-multiple-reagents")),
            ("desc", primary.LocalizedPhysicalDescription)));

        // Add descriptions of immediately recognizable reagents, like water or beer
        var recognized = new List<ReagentPrototype>();
        foreach (var proto in solution.GetReagentPrototypes(PrototypeManager).Keys)
        {
            if (!proto.Recognizable)
            {
                continue;
            }

            recognized.Add(proto);
        }

        // Skip if there's nothing recognizable
        if (recognized.Count == 0)
            return;

        var msg = new StringBuilder();
        foreach (var reagent in recognized)
        {
            string part;
            if (reagent == recognized[0])
            {
                part = "examinable-solution-recognized-first";
            }
            else if (reagent == recognized[^1])
            {
                // this loc specifically  requires space to be appended, fluent doesnt support whitespace
                msg.Append(' ');
                part = "examinable-solution-recognized-last";
            }
            else
            {
                part = "examinable-solution-recognized-next";
            }

            msg.Append(Loc.GetString(part, ("color", reagent.SubstanceColor.ToHexNoAlpha()),
                ("chemical", reagent.LocalizedName)));
        }

        args.PushMarkup(Loc.GetString("examinable-solution-has-recognizable-chemicals", ("recognizedString", msg.ToString())));
    }

    private void OnSolutionExaminableVerb(EntityUid uid, ExaminableSolutionComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var scanEvent = new SolutionScanEvent();
        RaiseLocalEvent(args.User, scanEvent);
        if (!scanEvent.CanScan)
        {
            return;
        }

        if (!TryGetSolution(args.Target, component.Solution, out _, out var solutionHolder))
        {
            return;
        }

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = GetSolutionExamine(solutionHolder);
                ExamineSystem.SendExamineTooltip(args.User, uid, markup, false, false);
            },
            Text = Loc.GetString("scannable-solution-verb-text"),
            Message = Loc.GetString("scannable-solution-verb-message"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/drink.svg.192dpi.png")),
        };

        args.Verbs.Add(verb);
    }

    private FormattedMessage GetSolutionExamine(Solution solution)
    {
        var msg = new FormattedMessage();

        if (solution.Volume == 0)
        {
            msg.AddMarkup(Loc.GetString("scannable-solution-empty-container"));
            return msg;
        }

        msg.AddMarkup(Loc.GetString("scannable-solution-main-text"));

        foreach (var (proto, quantity) in solution.GetReagentPrototypes(PrototypeManager))
        {
            msg.PushNewline();
            msg.AddMarkup(Loc.GetString("scannable-solution-chemical"
                , ("type", proto.LocalizedName)
                , ("color", proto.SubstanceColor.ToHexNoAlpha())
                , ("amount", quantity)));
        }

        return msg;
    }

    #endregion Event Handlers
}
