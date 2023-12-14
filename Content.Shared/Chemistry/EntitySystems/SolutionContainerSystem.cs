using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// This event alerts system that the solution was changed
/// </summary>
public sealed class SolutionChangedEvent : EntityEventArgs
{
    public readonly Solution Solution;
    public readonly string SolutionId;

    public SolutionChangedEvent(Solution solution, string solutionId)
    {
        SolutionId = solutionId;
        Solution = solution;
    }
}

/// <summary>
/// An event raised when more reagents are added to a (managed) solution than it can hold.
/// </summary>
[ByRefEvent]
public record struct SolutionOverflowEvent(EntityUid SolutionEnt, Solution SolutionHolder, Solution Overflow)
{
    /// <summary>The entity which contains the solution that has overflowed.</summary>
    public readonly EntityUid SolutionEnt = SolutionEnt;
    /// <summary>The solution that has overflowed.</summary>
    public readonly Solution SolutionHolder = SolutionHolder;
    /// <summary>The reagents that have overflowed the solution.</summary>
    public readonly Solution Overflow = Overflow;
    /// <summary>The volume by which the solution has overflowed.</summary>
    public readonly FixedPoint2 OverflowVol = Overflow.Volume;
    /// <summary>Whether some subscriber has taken care of the effects of the overflow.</summary>
    public bool Handled = false;
}

/// <summary>
/// Part of Chemistry system deal with SolutionContainers
/// </summary>
[UsedImplicitly]
public sealed partial class SolutionContainerSystem : EntitySystem
{
    [Dependency] private readonly ChemicalReactionSystem _chemistrySystem = default!;

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionContainerManagerComponent, ComponentInit>(InitSolution);
        SubscribeLocalEvent<ExaminableSolutionComponent, ExaminedEvent>(OnExamineSolution);
        SubscribeLocalEvent<ExaminableSolutionComponent, GetVerbsEvent<ExamineVerb>>(OnSolutionExaminableVerb);
    }

    private void InitSolution(EntityUid uid, SolutionContainerManagerComponent component, ComponentInit args)
    {
        foreach (var (name, solutionHolder) in component.Solutions)
        {
            solutionHolder.Name = name;
            solutionHolder.ValidateSolution();
            UpdateAppearance(uid, solutionHolder);
        }
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
            || !solutionsManager.Solutions.TryGetValue(component.Solution, out var solutionHolder))
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


        var reagentPrototypes = solution.GetReagentPrototypes(_prototypeManager);

        // Sort the reagents by amount, descending then alphabetically
        var sortedReagentPrototypes = reagentPrototypes
            .OrderByDescending(pair => pair.Value.Value)
            .ThenBy(pair => pair.Key.LocalizedName);

        // Add descriptions of immediately recognizable reagents, like water or beer
        var recognized = new List<ReagentPrototype>();
        foreach (var keyValuePair in sortedReagentPrototypes)
        {
            var proto = keyValuePair.Key;
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

    public void UpdateAppearance(EntityUid uid, Solution solution,
        AppearanceComponent? appearanceComponent = null)
    {
        if (!EntityManager.EntityExists(uid)
            || !Resolve(uid, ref appearanceComponent, false))
            return;

        _appearance.SetData(uid, SolutionContainerVisuals.FillFraction, solution.FillFraction, appearanceComponent);
        _appearance.SetData(uid, SolutionContainerVisuals.Color, solution.GetColor(_prototypeManager), appearanceComponent);
        if (solution.Name != null)
        {
            _appearance.SetData(uid, SolutionContainerVisuals.SolutionName, solution.Name, appearanceComponent);
        }

        if (solution.GetPrimaryReagentId() is { } reagent)
        {
            _appearance.SetData(uid, SolutionContainerVisuals.BaseOverride, reagent.ToString(), appearanceComponent);
        }
        else
        {
            _appearance.SetData(uid, SolutionContainerVisuals.BaseOverride, string.Empty, appearanceComponent);
        }
    }

    /// <summary>
    ///     Removes part of the solution in the container.
    /// </summary>
    /// <param name="targetUid"></param>
    /// <param name="solutionHolder"></param>
    /// <param name="quantity">the volume of solution to remove.</param>
    /// <returns>The solution that was removed.</returns>
    public Solution SplitSolution(EntityUid targetUid, Solution solutionHolder, FixedPoint2 quantity)
    {
        var splitSol = solutionHolder.SplitSolution(quantity);
        UpdateChemicals(targetUid, solutionHolder);
        return splitSol;
    }

    public Solution SplitStackSolution(EntityUid targetUid, Solution solutionHolder, FixedPoint2 quantity, int stackCount)
    {
        var splitSol = solutionHolder.SplitSolution(quantity / stackCount);
        solutionHolder.SplitSolution(quantity - splitSol.Volume);
        UpdateChemicals(targetUid, solutionHolder);
        return splitSol;
    }

    /// <summary>
    /// Splits a solution without the specified reagent(s).
    /// </summary>
    public Solution SplitSolutionWithout(EntityUid targetUid, Solution solutionHolder, FixedPoint2 quantity,
        params string[] reagents)
    {
        var splitSol = solutionHolder.SplitSolutionWithout(quantity, reagents);
        UpdateChemicals(targetUid, solutionHolder);
        return splitSol;
    }

    public void UpdateChemicals(EntityUid uid, Solution solutionHolder, bool needsReactionsProcessing = false, ReactionMixerComponent? mixerComponent = null)
    {
        DebugTools.Assert(solutionHolder.Name != null && TryGetSolution(uid, solutionHolder.Name, out var tmp) && tmp == solutionHolder);

        // Process reactions
        if (needsReactionsProcessing && solutionHolder.CanReact)
        {
            _chemistrySystem.FullyReactSolution(solutionHolder, uid, solutionHolder.MaxVolume, mixerComponent);
        }

        var overflowVol = solutionHolder.Volume - solutionHolder.MaxVolume;
        if (overflowVol > FixedPoint2.Zero)
        {
            var overflow = solutionHolder.SplitSolution(overflowVol);
            var overflowEv = new SolutionOverflowEvent(uid, solutionHolder, overflow);
            RaiseLocalEvent(uid, ref overflowEv);
        }

        UpdateAppearance(uid, solutionHolder);
        RaiseLocalEvent(uid, new SolutionChangedEvent(solutionHolder, solutionHolder.Name));
    }

    public void RemoveAllSolution(EntityUid uid, Solution solutionHolder)
    {
        if (solutionHolder.Volume == 0)
            return;

        solutionHolder.RemoveAllSolution();
        UpdateChemicals(uid, solutionHolder);
    }

    public void RemoveAllSolution(EntityUid uid, SolutionContainerManagerComponent? solutionContainerManager = null)
    {
        if (!Resolve(uid, ref solutionContainerManager))
            return;

        foreach (var solution in solutionContainerManager.Solutions.Values)
        {
            RemoveAllSolution(uid, solution);
        }
    }

    /// <summary>
    ///     Sets the capacity (maximum volume) of a solution to a new value.
    /// </summary>
    /// <param name="targetUid">The entity containing the solution.</param>
    /// <param name="targetSolution">The solution to set the capacity of.</param>
    /// <param name="capacity">The value to set the capacity of the solution to.</param>
    public void SetCapacity(EntityUid targetUid, Solution targetSolution, FixedPoint2 capacity)
    {
        if (targetSolution.MaxVolume == capacity)
            return;

        targetSolution.MaxVolume = capacity;
        if (capacity < targetSolution.Volume)
            targetSolution.RemoveSolution(targetSolution.Volume - capacity);

        UpdateChemicals(targetUid, targetSolution);
    }

    /// <summary>
    ///     Adds reagent of an Id to the container.
    /// </summary>
    /// <param name="targetUid"></param>
    /// <param name="targetSolution">Container to which we are adding reagent</param>
    /// <param name="reagentQuantity">The reagent to add.</param>
    /// <param name="acceptedQuantity">The amount of reagent successfully added.</param>
    /// <returns>If all the reagent could be added.</returns>
    public bool TryAddReagent(EntityUid targetUid, Solution targetSolution, ReagentQuantity reagentQuantity,
        out FixedPoint2 acceptedQuantity, float? temperature = null)
    {
        acceptedQuantity = targetSolution.AvailableVolume > reagentQuantity.Quantity
            ? reagentQuantity.Quantity
            : targetSolution.AvailableVolume;

        if (acceptedQuantity <= 0)
            return reagentQuantity.Quantity == 0;

        if (temperature == null)
        {
            targetSolution.AddReagent(reagentQuantity.Reagent, acceptedQuantity);
        }
        else
        {
            var proto = _prototypeManager.Index<ReagentPrototype>(reagentQuantity.Reagent.Prototype);
            targetSolution.AddReagent(proto, acceptedQuantity, temperature.Value, _prototypeManager);
        }

        UpdateChemicals(targetUid, targetSolution, true);
        return acceptedQuantity == reagentQuantity.Quantity;
    }

    /// <summary>
    ///     Adds reagent of an Id to the container.
    /// </summary>
    /// <param name="targetUid"></param>
    /// <param name="targetSolution">Container to which we are adding reagent</param>
    /// <param name="prototype">The Id of the reagent to add.</param>
    /// <param name="quantity">The amount of reagent to add.</param>
    /// <param name="acceptedQuantity">The amount of reagent successfully added.</param>
    /// <returns>If all the reagent could be added.</returns>
    public bool TryAddReagent(EntityUid targetUid, Solution targetSolution, string prototype, FixedPoint2 quantity,
        out FixedPoint2 acceptedQuantity, float? temperature = null, ReagentData? data = null)
    {
        var reagent = new ReagentQuantity(prototype, quantity, data);
        return TryAddReagent(targetUid, targetSolution, reagent, out acceptedQuantity, temperature);
    }

    /// <summary>
    ///     Adds reagent of an Id to the container.
    /// </summary>
    /// <param name="targetUid"></param>
    /// <param name="targetSolution">Container to which we are adding reagent</param>
    /// <param name="reagentId">The reagent to add.</param>
    /// <param name="quantity">The amount of reagent to add.</param>
    /// <param name="acceptedQuantity">The amount of reagent successfully added.</param>
    /// <returns>If all the reagent could be added.</returns>
    public bool TryAddReagent(EntityUid targetUid, Solution targetSolution, ReagentId reagentId, FixedPoint2 quantity,
        out FixedPoint2 acceptedQuantity, float? temperature = null)
    {
        var quant = new ReagentQuantity(reagentId, quantity);
        return TryAddReagent(targetUid, targetSolution, quant, out acceptedQuantity, temperature);
    }

    /// <summary>
    ///     Removes reagent from a container.
    /// </summary>
    /// <param name="targetUid"></param>
    /// <param name="container">Solution container from which we are removing reagent</param>
    /// <param name="reagentQuantity">The reagent to remove.</param>
    /// <returns>If the reagent to remove was found in the container.</returns>
    public bool RemoveReagent(EntityUid targetUid, Solution? container, ReagentQuantity reagentQuantity)
    {
        if (container == null)
            return false;

        var quant = container.RemoveReagent(reagentQuantity);
        if (quant <= FixedPoint2.Zero)
            return false;

        UpdateChemicals(targetUid, container);
        return true;
    }

    /// <summary>
    ///     Removes reagent from a container.
    /// </summary>
    /// <param name="targetUid"></param>
    /// <param name="container">Solution container from which we are removing reagent</param>
    /// <param name="prototype">The Id of the reagent to remove.</param>
    /// <param name="quantity">The amount of reagent to remove.</param>
    /// <returns>If the reagent to remove was found in the container.</returns>
    public bool RemoveReagent(EntityUid targetUid, Solution? container, string prototype, FixedPoint2 quantity, ReagentData? data = null)
    {
        return RemoveReagent(targetUid, container, new ReagentQuantity(prototype, quantity, data));
    }

    /// <summary>
    ///     Removes reagent from a container.
    /// </summary>
    /// <param name="targetUid"></param>
    /// <param name="container">Solution container from which we are removing reagent</param>
    /// <param name="reagentId">The reagent to remove.</param>
    /// <param name="quantity">The amount of reagent to remove.</param>
    /// <returns>If the reagent to remove was found in the container.</returns>
    public bool RemoveReagent(EntityUid targetUid, Solution? container, ReagentId reagentId, FixedPoint2 quantity)
    {
        return RemoveReagent(targetUid, container, new ReagentQuantity(reagentId, quantity));
    }

    /// <summary>
    ///     Moves some quantity of a solution from one solution to another.
    /// </summary>
    /// <param name="sourceUid">entity holding the source solution</param>
    /// <param name="targetUid">entity holding the target solution</param>
    /// <param name="source">source solution</param>
    /// <param name="target">target solution</param>
    /// <param name="quantity">quantity of solution to move from source to target. If this is a negative number, the source & target roles are reversed.</param>
    public bool TryTransferSolution(EntityUid sourceUid, EntityUid targetUid, Solution source, Solution target, FixedPoint2 quantity)
    {
        if (!TryTransferSolution(targetUid, target, source, quantity))
            return false;

        UpdateChemicals(sourceUid, source, false);
        return true;
    }

    /// <summary>
    ///     Moves some quantity of a solution from one solution to another.
    /// </summary>
    /// <param name="sourceUid">entity holding the source solution</param>
    /// <param name="targetUid">entity holding the target solution</param>
    /// <param name="source">source solution</param>
    /// <param name="target">target solution</param>
    /// <param name="quantity">quantity of solution to move from source to target. If this is a negative number, the source & target roles are reversed.</param>
    public bool TryTransferSolution(EntityUid targetUid, Solution target, Solution source, FixedPoint2 quantity)
    {
        if (quantity < 0)
            throw new InvalidOperationException("Quantity must be positive");

        quantity = FixedPoint2.Min(quantity, target.AvailableVolume, source.Volume);
        if (quantity == 0)
            return false;

        // TODO This should be made into a function that directly transfers reagents.
        // Currently this is quite inefficient.
        target.AddSolution(source.SplitSolution(quantity), _prototypeManager);

        UpdateChemicals(targetUid, target, true);
        return true;
    }

    /// <summary>
    ///     Moves some quantity of a solution from one solution to another.
    /// </summary>
    /// <param name="sourceUid">entity holding the source solution</param>
    /// <param name="targetUid">entity holding the target solution</param>
    /// <param name="source">source solution</param>
    /// <param name="target">target solution</param>
    /// <param name="quantity">quantity of solution to move from source to target. If this is a negative number, the source & target roles are reversed.</param>
    public bool TryTransferSolution(EntityUid sourceUid, EntityUid targetUid, string source, string target, FixedPoint2 quantity)
    {
        if (!TryGetSolution(sourceUid, source, out var sourceSoln))
            return false;

        if (!TryGetSolution(targetUid, target, out var targetSoln))
            return false;

        return TryTransferSolution(sourceUid, targetUid, sourceSoln, targetSoln, quantity);
    }

    /// <summary>
    ///     Adds a solution to the container, if it can fully fit.
    /// </summary>
    /// <param name="targetUid">entity holding targetSolution</param>
    ///  <param name="targetSolution">entity holding targetSolution</param>
    /// <param name="toAdd">solution being added</param>
    /// <returns>If the solution could be added.</returns>
    public bool TryAddSolution(EntityUid targetUid, Solution targetSolution, Solution toAdd)
    {
        if (toAdd.Volume == FixedPoint2.Zero)
            return true;
        if (toAdd.Volume > targetSolution.AvailableVolume)
            return false;

        ForceAddSolution(targetUid, targetSolution, toAdd);
        return true;
    }

    /// <summary>
    ///     Adds as much of a solution to a container as can fit.
    /// </summary>
    /// <param name="targetUid">The entity containing <paramref cref="targetSolution"/></param>
    /// <param name="targetSolution">The solution being added to.</param>
    /// <param name="toAdd">The solution being added to <paramref cref="targetSolution"/></param>
    /// <returns>The quantity of the solution actually added.</returns>
    public FixedPoint2 AddSolution(EntityUid targetUid, Solution targetSolution, Solution toAdd)
    {
        if (toAdd.Volume == FixedPoint2.Zero)
            return FixedPoint2.Zero;

        var quantity = FixedPoint2.Max(FixedPoint2.Zero, FixedPoint2.Min(toAdd.Volume, targetSolution.AvailableVolume));
        if (quantity < toAdd.Volume)
            TryTransferSolution(targetUid, targetSolution, toAdd, quantity);
        else
            ForceAddSolution(targetUid, targetSolution, toAdd);

        return quantity;
    }

    /// <summary>
    ///     Adds a solution to a container and updates the container.
    /// </summary>
    /// <param name="targetUid">The entity containing <paramref cref="targetSolution"/></param>
    /// <param name="targetSolution">The solution being added to.</param>
    /// <param name="toAdd">The solution being added to <paramref cref="targetSolution"/></param>
    /// <returns>Whether any reagents were added to the solution.</returns>
    public bool ForceAddSolution(EntityUid targetUid, Solution targetSolution, Solution toAdd)
    {
        if (toAdd.Volume == FixedPoint2.Zero)
            return false;

        targetSolution.AddSolution(toAdd, _prototypeManager);
        UpdateChemicals(targetUid, targetSolution, needsReactionsProcessing: true);
        return true;
    }

    /// <summary>
    ///     Adds a solution to the container, removing the overflow.
    ///     Unlike <see cref="TryAddSolution"/> it will ignore size limits.
    /// </summary>
    /// <param name="targetUid">The entity containing <paramref cref="targetSolution"/></param>
    /// <param name="targetSolution">The solution being added to.</param>
    /// <param name="toAdd">The solution being added to <paramref cref="targetSolution"/></param>
    /// <param name="overflowThreshold">The combined volume above which the overflow will be returned.
    /// If the combined volume is below this an empty solution is returned.</param>
    /// <param name="overflowingSolution">Solution that exceeded overflowThreshold</param>
    /// <returns>Whether any reagents were added to <paramref cref="targetSolution"/>.</returns>
    public bool TryMixAndOverflow(EntityUid targetUid, Solution targetSolution,
        Solution toAdd,
        FixedPoint2 overflowThreshold,
        [NotNullWhen(true)] out Solution? overflowingSolution)
    {
        if (toAdd.Volume == 0 || overflowThreshold > targetSolution.MaxVolume)
        {
            overflowingSolution = null;
            return false;
        }

        targetSolution.AddSolution(toAdd, _prototypeManager);
        overflowingSolution = targetSolution.SplitSolution(FixedPoint2.Max(FixedPoint2.Zero, targetSolution.Volume - overflowThreshold));
        UpdateChemicals(targetUid, targetSolution, true);
        return true;
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

    public Solution EnsureSolution(EntityUid uid, string name,
        IEnumerable<ReagentQuantity> reagents,
        bool setMaxVol = true,
        SolutionContainerManagerComponent? solutionsMgr = null)
    {
        if (!Resolve(uid, ref solutionsMgr, false))
            solutionsMgr = EntityManager.EnsureComponent<SolutionContainerManagerComponent>(uid);

        if (!solutionsMgr.Solutions.TryGetValue(name, out var existing))
        {
            var newSolution = new Solution(reagents, setMaxVol);
            solutionsMgr.Solutions.Add(name, newSolution);
            return newSolution;
        }

        existing.SetContents(reagents, setMaxVol);
        return existing;
    }
    /// <summary>
    ///     Removes an amount from all reagents in a solution, adding it to a new solution.
    /// </summary>
    /// <param name="uid">The entity containing the solution.</param>
    /// <param name="solution">The solution to remove reagents from.</param>
    /// <param name="quantity">The amount to remove from every reagent in the solution.</param>
    /// <returns>A new solution containing every removed reagent from the original solution.</returns>
    public Solution RemoveEachReagent(EntityUid uid, Solution solution, FixedPoint2 quantity)
    {
        if (quantity <= 0)
            return new Solution();

        var removedSolution = new Solution();

        // RemoveReagent does a RemoveSwap, meaning we don't have to copy the list if we iterate it backwards.
        for (var i = solution.Contents.Count - 1; i >= 0; i--)
        {
            var (reagent, _) = solution.Contents[i];
            var removedQuantity = solution.RemoveReagent(reagent, quantity);
            removedSolution.AddReagent(reagent, removedQuantity);
        }

        UpdateChemicals(uid, solution);
        return removedSolution;
    }

    public FixedPoint2 GetTotalPrototypeQuantity(EntityUid owner, string reagentId)
    {
        var reagentQuantity = FixedPoint2.New(0);
        if (EntityManager.EntityExists(owner)
            && EntityManager.TryGetComponent(owner, out SolutionContainerManagerComponent? managerComponent))
        {
            foreach (var solution in managerComponent.Solutions.Values)
            {
                reagentQuantity += solution.GetTotalPrototypeQuantity(reagentId);
            }
        }

        return reagentQuantity;
    }

    public bool TryGetMixableSolution(EntityUid uid,
        [NotNullWhen(true)] out Solution? solution,
        SolutionContainerManagerComponent? solutionsMgr = null)
    {

        if (!Resolve(uid, ref solutionsMgr, false))
        {
            solution = null;
            return false;
        }

        var getMixableSolutionAttempt = new GetMixableSolutionAttemptEvent(uid);
        RaiseLocalEvent(uid, ref getMixableSolutionAttempt);
        if (getMixableSolutionAttempt.MixedSolution != null)
        {
            solution = getMixableSolutionAttempt.MixedSolution;
            return true;
        }

        var tryGetSolution = solutionsMgr.Solutions.FirstOrNull(x => x.Value.CanMix);
        if (tryGetSolution.HasValue)
        {
            solution = tryGetSolution.Value.Value;
            return true;
        }

        solution = null;
        return false;
    }

    /// <summary>
    /// Gets the most common reagent across all solutions by volume.
    /// </summary>
    /// <param name="component"></param>
    public ReagentPrototype? GetMaxReagent(SolutionContainerManagerComponent component)
    {
        if (component.Solutions.Count == 0)
            return null;

        var reagentCounts = new Dictionary<ReagentId, FixedPoint2>();

        foreach (var solution in component.Solutions.Values)
        {
            foreach (var (reagent, quantity) in solution.Contents)
            {
                reagentCounts.TryGetValue(reagent, out var existing);
                existing += quantity;
                reagentCounts[reagent] = existing;
            }
        }

        var max = reagentCounts.Max();

        return _prototypeManager.Index<ReagentPrototype>(max.Key.Prototype);
    }

    public SoundSpecifier? GetSound(SolutionContainerManagerComponent component)
    {
        var max = GetMaxReagent(component);
        return max?.FootstepSound;
    }

    // Thermal energy and temperature management.

    #region Thermal Energy and Temperature

    /// <summary>
    ///     Sets the temperature of a solution to a new value and then checks for reaction processing.
    /// </summary>
    /// <param name="owner">The entity in which the solution is located.</param>
    /// <param name="solution">The solution to set the temperature of.</param>
    /// <param name="temperature">The new value to set the temperature to.</param>
    public void SetTemperature(EntityUid owner, Solution solution, float temperature)
    {
        if (temperature == solution.Temperature)
            return;

        solution.Temperature = temperature;
        UpdateChemicals(owner, solution, true);
    }

    /// <summary>
    ///     Sets the thermal energy of a solution to a new value and then checks for reaction processing.
    /// </summary>
    /// <param name="owner">The entity in which the solution is located.</param>
    /// <param name="solution">The solution to set the thermal energy of.</param>
    /// <param name="thermalEnergy">The new value to set the thermal energy to.</param>
    public void SetThermalEnergy(EntityUid owner, Solution solution, float thermalEnergy)
    {
        var heatCap = solution.GetHeatCapacity(_prototypeManager);
        solution.Temperature = heatCap == 0 ? 0 : thermalEnergy / heatCap;
        UpdateChemicals(owner, solution, true);
    }

    /// <summary>
    ///     Adds some thermal energy to a solution and then checks for reaction processing.
    /// </summary>
    /// <param name="owner">The entity in which the solution is located.</param>
    /// <param name="solution">The solution to set the thermal energy of.</param>
    /// <param name="thermalEnergy">The new value to set the thermal energy to.</param>
    public void AddThermalEnergy(EntityUid owner, Solution solution, float thermalEnergy)
    {
        if (thermalEnergy == 0.0f)
            return;

        var heatCap = solution.GetHeatCapacity(_prototypeManager);
        solution.Temperature += heatCap == 0 ? 0 : thermalEnergy / heatCap;
        UpdateChemicals(owner, solution, true);
    }

    #endregion Thermal Energy and Temperature

    #region Event Handlers

    #endregion Event Handlers
}
