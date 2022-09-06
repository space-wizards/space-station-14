using System.Diagnostics.CodeAnalysis;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntitySystems;

/// <summary>
/// This event alerts system that the solution was changed
/// </summary>
public sealed class SolutionChangedEvent : EntityEventArgs
{
}

/// <summary>
/// Part of Chemistry system deal with SolutionContainers
/// </summary>
[UsedImplicitly]
public sealed partial class SolutionContainerSystem : EntitySystem
{
    [Dependency]
    private readonly SharedChemicalReactionSystem _chemistrySystem = default!;

    [Dependency]
    private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionContainerManagerComponent, ComponentInit>(InitSolution);
        SubscribeLocalEvent<ExaminableSolutionComponent, ExaminedEvent>(OnExamineSolution);
    }

    private void InitSolution(EntityUid uid, SolutionContainerManagerComponent component, ComponentInit args)
    {
        foreach (var (name, solutionHolder) in component.Solutions)
        {
            solutionHolder.Name = name;
            if (solutionHolder.MaxVolume == FixedPoint2.Zero)
            {
                solutionHolder.MaxVolume = solutionHolder.TotalVolume > solutionHolder.InitialMaxVolume
                    ? solutionHolder.TotalVolume
                    : solutionHolder.InitialMaxVolume;
            }

            UpdateAppearance(uid, solutionHolder);
        }
    }

    private void OnExamineSolution(EntityUid uid, ExaminableSolutionComponent examinableComponent,
        ExaminedEvent args)
    {
        SolutionContainerManagerComponent? solutionsManager = null;
        if (!Resolve(args.Examined, ref solutionsManager)
            || !solutionsManager.Solutions.TryGetValue(examinableComponent.Solution, out var solutionHolder))
            return;

        if (solutionHolder.Contents.Count == 0)
        {
            args.PushText(Loc.GetString("shared-solution-container-component-on-examine-empty-container"));
            return;
        }

        var primaryReagent = solutionHolder.GetPrimaryReagentId();

        if (!_prototypeManager.TryIndex(primaryReagent, out ReagentPrototype? proto))
        {
            Logger.Error(
                $"{nameof(Solution)} could not find the prototype associated with {primaryReagent}.");
            return;
        }

        var colorHex = solutionHolder.Color
            .ToHexNoAlpha(); //TODO: If the chem has a dark color, the examine text becomes black on a black background, which is unreadable.
        var messageString = "shared-solution-container-component-on-examine-main-text";

        args.PushMarkup(Loc.GetString(messageString,
            ("color", colorHex),
            ("wordedAmount", Loc.GetString(solutionHolder.Contents.Count == 1
                ? "shared-solution-container-component-on-examine-worded-amount-one-reagent"
                : "shared-solution-container-component-on-examine-worded-amount-multiple-reagents")),
            ("desc", proto.LocalizedPhysicalDescription)));
    }

    public void UpdateAppearance(EntityUid uid, Solution solution,
        AppearanceComponent? appearanceComponent = null)
    {
        if (!EntityManager.EntityExists(uid)
            || !Resolve(uid, ref appearanceComponent, false))
            return;

        var filledVolumePercent = Math.Min(1.0f, solution.CurrentVolume.Float() / solution.MaxVolume.Float());
        appearanceComponent.SetData(SolutionContainerVisuals.VisualState,
            new SolutionContainerVisualState(solution.Color, filledVolumePercent));
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

    public void UpdateChemicals(EntityUid uid, Solution solutionHolder, bool needsReactionsProcessing = false)
    {
        // Process reactions
        if (needsReactionsProcessing && solutionHolder.CanReact)
        {
            _chemistrySystem.FullyReactSolution(solutionHolder, uid, solutionHolder.MaxVolume);
        }

        UpdateAppearance(uid, solutionHolder);
        RaiseLocalEvent(uid, new SolutionChangedEvent(), true);
    }

    public void RemoveAllSolution(EntityUid uid, Solution solutionHolder)
    {
        if (solutionHolder.CurrentVolume == 0)
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
        if (capacity < targetSolution.CurrentVolume)
            targetSolution.RemoveSolution(targetSolution.CurrentVolume - capacity);

        UpdateChemicals(targetUid, targetSolution);
    }

    /// <summary>
    ///     Adds reagent of an Id to the container.
    /// </summary>
    /// <param name="targetUid"></param>
    /// <param name="targetSolution">Container to which we are adding reagent</param>
    /// <param name="reagentId">The Id of the reagent to add.</param>
    /// <param name="quantity">The amount of reagent to add.</param>
    /// <param name="acceptedQuantity">The amount of reagent successfully added.</param>
    /// <returns>If all the reagent could be added.</returns>
    public bool TryAddReagent(EntityUid targetUid, Solution targetSolution, string reagentId, FixedPoint2 quantity,
        out FixedPoint2 acceptedQuantity, float? temperature = null)
    {
        acceptedQuantity = targetSolution.AvailableVolume > quantity ? quantity : targetSolution.AvailableVolume;
        targetSolution.AddReagent(reagentId, acceptedQuantity, temperature);

        if (acceptedQuantity > 0)
            UpdateChemicals(targetUid, targetSolution, true);

        return acceptedQuantity == quantity;
    }

    /// <summary>
    ///     Removes reagent of an Id to the container.
    /// </summary>
    /// <param name="targetUid"></param>
    /// <param name="container">Solution container from which we are removing reagent</param>
    /// <param name="reagentId">The Id of the reagent to remove.</param>
    /// <param name="quantity">The amount of reagent to remove.</param>
    /// <returns>If the reagent to remove was found in the container.</returns>
    public bool TryRemoveReagent(EntityUid targetUid, Solution? container, string reagentId, FixedPoint2 quantity)
    {
        if (container == null || !container.ContainsReagent(reagentId))
            return false;

        container.RemoveReagent(reagentId, quantity);
        UpdateChemicals(targetUid, container);
        return true;
    }

    /// <summary>
    ///     Adds a solution to the container, if it can fully fit.
    /// </summary>
    /// <param name="targetUid">entity holding targetSolution</param>
    ///  <param name="targetSolution">entity holding targetSolution</param>
    /// <param name="addedSolution">solution being added</param>
    /// <returns>If the solution could be added.</returns>
    public bool TryAddSolution(EntityUid targetUid, Solution? targetSolution, Solution addedSolution)
    {
        if (targetSolution == null
            || !targetSolution.CanAddSolution(addedSolution) || addedSolution.TotalVolume == 0)
            return false;

        targetSolution.AddSolution(addedSolution);
        UpdateChemicals(targetUid, targetSolution, true);
        return true;
    }

    /// <summary>
    ///     Adds a solution to the container, overflowing the rest.
    ///     It will
    ///     Unlike <see cref="TryAddSolution"/> it will ignore size limits.
    /// </summary>
    /// <param name="targetUid">entity holding targetSolution</param>
    /// <param name="targetSolution">The container to which we try to add.</param>
    /// <param name="addedSolution">solution being added</param>
    /// <param name="overflowThreshold">After addition this much will be left in targetSolution. Should be less
    /// than targetSolution.TotalVolume</param>
    /// <param name="overflowingSolution">Solution that exceeded overflowThreshold</param>
    /// <returns></returns>
    public bool TryMixAndOverflow(EntityUid targetUid, Solution targetSolution,
        Solution addedSolution,
        FixedPoint2 overflowThreshold,
        [NotNullWhen(true)] out Solution? overflowingSolution)
    {
        if (addedSolution.TotalVolume == 0 || overflowThreshold > targetSolution.MaxVolume)
        {
            overflowingSolution = null;
            return false;
        }

        targetSolution.AddSolution(addedSolution);
        UpdateChemicals(targetUid, targetSolution, true);
        overflowingSolution = targetSolution.SplitSolution(FixedPoint2.Max(FixedPoint2.Zero,
            targetSolution.CurrentVolume - overflowThreshold));
        return true;
    }

    public bool TryGetSolution(EntityUid uid, string name,
        [NotNullWhen(true)] out Solution? solution,
        SolutionContainerManagerComponent? solutionsMgr = null)
    {
        if (!Resolve(uid, ref solutionsMgr, false))
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
    /// <returns>solution</returns>
    public Solution EnsureSolution(EntityUid uid, string name,
        SolutionContainerManagerComponent? solutionsMgr = null)
    {
        if (!Resolve(uid, ref solutionsMgr, false))
        {
            solutionsMgr = EntityManager.EnsureComponent<SolutionContainerManagerComponent>(uid);
        }

        if (!solutionsMgr.Solutions.ContainsKey(name))
        {
            var newSolution = new Solution() { Name = name };
            solutionsMgr.Solutions.Add(name, newSolution);
        }

        return solutionsMgr.Solutions[name];
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
        for (var i = solution.Contents.Count-1; i >= 0; i--)
        {
            var (reagentId, _) = solution.Contents[i];

            var removedQuantity = solution.RemoveReagent(reagentId, quantity);

            if(removedQuantity > 0)
                removedSolution.AddReagent(reagentId, removedQuantity);
        }

        UpdateChemicals(uid, solution);
        return removedSolution;
    }

    public FixedPoint2 GetReagentQuantity(EntityUid owner, string reagentId)
    {
        var reagentQuantity = FixedPoint2.New(0);
        if (EntityManager.EntityExists(owner)
            && EntityManager.TryGetComponent(owner, out SolutionContainerManagerComponent? managerComponent))
        {
            foreach (var solution in managerComponent.Solutions.Values)
            {
                reagentQuantity += solution.GetReagentQuantity(reagentId);
            }
        }

        return reagentQuantity;
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
        if (thermalEnergy == solution.ThermalEnergy)
            return;

        solution.ThermalEnergy = thermalEnergy;
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

        solution.ThermalEnergy += thermalEnergy;
        UpdateChemicals(owner, solution, true);
    }

    #endregion Thermal Energy and Temperature
}
