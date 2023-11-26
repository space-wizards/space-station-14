using System.Diagnostics.CodeAnalysis;
using System.Text;
using Content.Shared.Chemistry.Containers.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solutions;
using Content.Shared.Chemistry.Solutions.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Verbs;
using JetBrains.Annotations;
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
    [Dependency] protected readonly SolutionSystem SolutionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExaminableSolutionComponent, ExaminedEvent>(OnExamineSolution);
        SubscribeLocalEvent<ExaminableSolutionComponent, GetVerbsEvent<ExamineVerb>>(OnSolutionExaminableVerb);
    }

    public bool TryGetSolution([NotNullWhen(true)] EntityUid? uid, string name,
        [NotNullWhen(true)] out Solution? solution,
        SolutionContainerManagerComponent? solutionsMgr = null)
    {
        if (uid == null || !Resolve(uid.Value, ref solutionsMgr, false))
        {
            solution = null;
            return false;
        }

        return solutionsMgr.Solutions.TryGetValue(name, out solution);
    }

    public IEnumerable<(string Name, Solution Solution)> EnumerateSolutions(Entity<SolutionContainerManagerComponent?> container)
    {
        if (!Resolve(container, ref container.Comp, logMissing: false))
            yield break;

        foreach (var (name, solution) in EnumerateSolutions(container.Comp))
        {
            yield return (name, solution);
        }
    }

    public IEnumerable<(string Name, Solution Solution)> EnumerateSolutions(SolutionContainerManagerComponent container)
    {
        foreach (var (name, solution) in container.Solutions)
        {
            yield return (name, solution);
        }
    }

    public FixedPoint2 GetTotalPrototypeQuantity(EntityUid owner, string reagentId)
    {
        var reagentQuantity = FixedPoint2.New(0);
        if (EntityManager.EntityExists(owner)
            && EntityManager.TryGetComponent(owner, out SolutionContainerManagerComponent? managerComponent))
        {
            foreach (var (_, solution) in EnumerateSolutions((owner, managerComponent)))
            {
                reagentQuantity += solution.GetTotalPrototypeQuantity(reagentId);
            }
        }

        return reagentQuantity;
    }

    #region Event Handlers

    private void OnExamineSolution(EntityUid uid, ExaminableSolutionComponent examinableComponent,
        ExaminedEvent args)
    {
        SolutionContainerManagerComponent? solutionsManager = null;
        if (!Resolve(args.Examined, ref solutionsManager)
            || !solutionsManager.Solutions.TryGetValue(examinableComponent.Solution, out var solution))
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

        SolutionContainerManagerComponent? solutionsManager = null;
        if (!Resolve(args.Target, ref solutionsManager)
            || !TryGetSolution(args.Target, component.Solution, out var solutionHolder, solutionsManager))
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
