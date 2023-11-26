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
public sealed partial class SolutionContainerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SolutionSystem _solutionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionContainerManagerComponent, ComponentInit>(InitSolution);
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

    /// <summary>
    /// Will ensure a solution is added to given entity even if it's missing solutionContainerManager
    /// </summary>
    /// <param name="uid">EntityUid to which to add solution</param>
    /// <param name="name">name for the solution</param>
    /// <param name="solutionsMgr">solution components used in resolves</param>
    /// <param name="existed">true if the solution already existed</param>
    /// <returns>solution</returns>
    public Solution EnsureSolution(EntityUid uid, string name, out bool existed,
        SolutionContainerManagerComponent? solutionsMgr = null)
    {
        if (!Resolve(uid, ref solutionsMgr, false))
        {
            solutionsMgr = EntityManager.EnsureComponent<SolutionContainerManagerComponent>(uid);
        }

        if (!solutionsMgr.Solutions.TryGetValue(name, out var existing))
        {
            var newSolution = new Solution() { Name = name };
            solutionsMgr.Solutions.Add(name, newSolution);
            existed = false;
            return newSolution;
        }

        existed = true;
        return existing;
    }

    /// <summary>
    /// Will ensure a solution is added to given entity even if it's missing solutionContainerManager
    /// </summary>
    /// <param name="uid">EntityUid to which to add solution</param>
    /// <param name="name">name for the solution</param>
    /// <param name="solutionsMgr">solution components used in resolves</param>
    /// <returns>solution</returns>
    public Solution EnsureSolution(EntityUid uid, string name, SolutionContainerManagerComponent? solutionsMgr = null)
        => EnsureSolution(uid, name, out _, solutionsMgr);

    /// <summary>
    /// Will ensure a solution is added to given entity even if it's missing solutionContainerManager
    /// </summary>
    /// <param name="uid">EntityUid to which to add solution</param>
    /// <param name="name">name for the solution</param>
    /// <param name="minVol">Ensures that the solution's maximum volume is larger than this value.</param>
    /// <param name="solutionsMgr">solution components used in resolves</param>
    /// <returns>solution</returns>
    public Solution EnsureSolution(EntityUid uid, string name, FixedPoint2 minVol, out bool existed,
        SolutionContainerManagerComponent? solutionsMgr = null)
    {
        if (!Resolve(uid, ref solutionsMgr, false))
        {
            solutionsMgr = EntityManager.EnsureComponent<SolutionContainerManagerComponent>(uid);
        }

        if (!solutionsMgr.Solutions.TryGetValue(name, out var existing))
        {
            var newSolution = new Solution() { Name = name };
            solutionsMgr.Solutions.Add(name, newSolution);
            existed = false;
            newSolution.MaxVolume = minVol;
            return newSolution;
        }

        existed = true;
        existing.MaxVolume = FixedPoint2.Max(existing.MaxVolume, minVol);
        return existing;
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

    private void InitSolution(EntityUid uid, SolutionContainerManagerComponent component, ComponentInit args)
    {
        foreach (var (name, solutionHolder) in component.Solutions)
        {
            solutionHolder.Name = name;
            solutionHolder.ValidateSolution();
            _solutionSystem.UpdateAppearance(uid, solutionHolder);
        }
    }

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

        if (!_prototypeManager.TryIndex(primaryReagent.Value.Prototype, out ReagentPrototype? primary))
        {
            Log.Error($"{nameof(Solution)} could not find the prototype associated with {primaryReagent}.");
            return;
        }

        var colorHex = solution.GetColor(_prototypeManager)
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
        foreach (var proto in solution.GetReagentPrototypes(_prototypeManager).Keys)
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
                _examine.SendExamineTooltip(args.User, uid, markup, false, false);
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

        foreach (var (proto, quantity) in solution.GetReagentPrototypes(_prototypeManager))
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
