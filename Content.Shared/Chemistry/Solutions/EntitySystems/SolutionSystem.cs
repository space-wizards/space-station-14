using Content.Shared.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solutions.Components;
using Content.Shared.Chemistry.Solutions.Events;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Chemistry.Solutions.EntitySystems;

public sealed partial class SolutionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChemicalReactionSystem _chemicalReactionSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SolutionComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SolutionComponent, ComponentShutdown>(OnComponentShutdown);
    }


    /// <summary>
    /// Dirties a solution entity that has been modified and prompts updates to chemical reactions and overflow state.
    /// Should be invoked whenever a solution entity is modified.
    /// </summary>
    /// <remarks>
    /// 90% of this system is ensuring that this proc is invoked whenever a solution entity is changed. The other 10% <i>is</i> this proc.
    /// </remarks>
    /// <param name="soln"></param>
    /// <param name="needsReactionsProcessing"></param>
    /// <param name="mixerComponent"></param>
    public void UpdateChemicals(Entity<SolutionComponent> soln, bool needsReactionsProcessing = true, ReactionMixerComponent? mixerComponent = null)
    {
        Dirty(soln);

        var (uid, comp) = soln;
        var solution = comp.Solution;

        // Process reactions
        if (needsReactionsProcessing && solution.CanReact)
            _chemicalReactionSystem.FullyReactSolution(solution, uid, solution.MaxVolume, mixerComponent);

        var overflow = solution.Volume - solution.MaxVolume;
        if (overflow > FixedPoint2.Zero)
        {
            var overflowEv = new SolutionOverflowEvent(soln, overflow);
            RaiseLocalEvent(uid, ref overflowEv);
        }

        UpdateAppearance((uid, comp, null));

        var changedEv = new SolutionChangedEvent(soln);
        RaiseLocalEvent(uid, ref changedEv);
    }

    public void UpdateAppearance(Entity<SolutionComponent, AppearanceComponent?> soln)
    {
        var (uid, comp, appearanceComponent) = soln;
        var solution = comp.Solution;

        if (!EntityManager.EntityExists(uid) || !Resolve(uid, ref appearanceComponent, false))
            return;

        _appearanceSystem.SetData(uid, SolutionContainerVisuals.FillFraction, solution.FillFraction, appearanceComponent);
        _appearanceSystem.SetData(uid, SolutionContainerVisuals.Color, solution.GetColor(_prototypeManager), appearanceComponent);

        if (solution.GetPrimaryReagentId() is { } reagent)
            _appearanceSystem.SetData(uid, SolutionContainerVisuals.BaseOverride, reagent.ToString(), appearanceComponent);
        else
            _appearanceSystem.SetData(uid, SolutionContainerVisuals.BaseOverride, string.Empty, appearanceComponent);
    }

    /// <summary>
    ///     Removes part of the solution in the container.
    /// </summary>
    /// <param name="targetUid"></param>
    /// <param name="solutionHolder"></param>
    /// <param name="quantity">the volume of solution to remove.</param>
    /// <returns>The solution that was removed.</returns>
    public Solution SplitSolution(Entity<SolutionComponent> soln, FixedPoint2 quantity)
    {
        var (uid, comp) = soln;
        var solution = comp.Solution;

        var splitSol = solution.SplitSolution(quantity);
        UpdateChemicals(soln);
        return splitSol;
    }

    public Solution SplitStackSolution(Entity<SolutionComponent> soln, FixedPoint2 quantity, int stackCount)
    {
        var (uid, comp) = soln;
        var solution = comp.Solution;

        var splitSol = solution.SplitSolution(quantity / stackCount);
        solution.SplitSolution(quantity - splitSol.Volume);
        UpdateChemicals(soln);
        return splitSol;
    }

    /// <summary>
    /// Splits a solution without the specified reagent(s).
    /// </summary>
    public Solution SplitSolutionWithout(Entity<SolutionComponent> soln, FixedPoint2 quantity, params string[] reagents)
    {
        var (uid, comp) = soln;
        var solution = comp.Solution;

        var splitSol = solution.SplitSolutionWithout(quantity, reagents);
        UpdateChemicals(soln);
        return splitSol;
    }

    public void RemoveAllSolution(Entity<SolutionComponent> soln)
    {
        var (uid, comp) = soln;
        var solution = comp.Solution;

        if (solution.Volume == 0)
            return;

        solution.RemoveAllSolution();
        UpdateChemicals(soln);
    }

    /// <summary>
    ///     Sets the capacity (maximum volume) of a solution to a new value.
    /// </summary>
    /// <param name="targetUid">The entity containing the solution.</param>
    /// <param name="targetSolution">The solution to set the capacity of.</param>
    /// <param name="capacity">The value to set the capacity of the solution to.</param>
    public void SetCapacity(Entity<SolutionComponent> soln, FixedPoint2 capacity)
    {
        var (uid, comp) = soln;
        var solution = comp.Solution;

        if (solution.MaxVolume == capacity)
            return;

        solution.MaxVolume = capacity;
        UpdateChemicals(soln);
    }

    /// <summary>
    ///     Adds reagent of an Id to the container.
    /// </summary>
    /// <param name="targetUid"></param>
    /// <param name="targetSolution">Container to which we are adding reagent</param>
    /// <param name="reagentQuantity">The reagent to add.</param>
    /// <param name="acceptedQuantity">The amount of reagent successfully added.</param>
    /// <returns>If all the reagent could be added.</returns>
    public bool TryAddReagent(Entity<SolutionComponent> soln, ReagentQuantity reagentQuantity, out FixedPoint2 acceptedQuantity, float? temperature = null)
    {
        var (uid, comp) = soln;
        var solution = comp.Solution;

        acceptedQuantity = solution.AvailableVolume > reagentQuantity.Quantity
            ? reagentQuantity.Quantity
            : solution.AvailableVolume;

        if (acceptedQuantity <= 0)
            return reagentQuantity.Quantity == 0;

        if (temperature == null)
        {
            solution.AddReagent(reagentQuantity.Reagent, acceptedQuantity);
        }
        else
        {
            var proto = _prototypeManager.Index<ReagentPrototype>(reagentQuantity.Reagent.Prototype);
            solution.AddReagent(proto, acceptedQuantity, temperature.Value, _prototypeManager);
        }

        UpdateChemicals(soln);
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
    public bool TryAddReagent(Entity<SolutionComponent> soln, string prototype, FixedPoint2 quantity, out FixedPoint2 acceptedQuantity, float? temperature = null, ReagentData? data = null)
    {
        var reagent = new ReagentQuantity(prototype, quantity, data);
        return TryAddReagent(soln, reagent, out acceptedQuantity, temperature);
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
    public bool TryAddReagent(Entity<SolutionComponent> soln, ReagentId reagentId, FixedPoint2 quantity, out FixedPoint2 acceptedQuantity, float? temperature = null)
    {
        var quant = new ReagentQuantity(reagentId, quantity);
        return TryAddReagent(soln, quant, out acceptedQuantity, temperature);
    }

    /// <summary>
    ///     Removes reagent from a container.
    /// </summary>
    /// <param name="targetUid"></param>
    /// <param name="container">Solution container from which we are removing reagent</param>
    /// <param name="reagentQuantity">The reagent to remove.</param>
    /// <returns>If the reagent to remove was found in the container.</returns>
    public bool RemoveReagent(Entity<SolutionComponent> soln, ReagentQuantity reagentQuantity)
    {
        var (uid, comp) = soln;
        var solution = comp.Solution;

        var quant = solution.RemoveReagent(reagentQuantity);
        if (quant <= FixedPoint2.Zero)
            return false;

        UpdateChemicals(soln);
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
    public bool RemoveReagent(Entity<SolutionComponent> soln, string prototype, FixedPoint2 quantity, ReagentData? data = null)
    {
        return RemoveReagent(soln, new ReagentQuantity(prototype, quantity, data));
    }

    /// <summary>
    ///     Removes reagent from a container.
    /// </summary>
    /// <param name="targetUid"></param>
    /// <param name="container">Solution container from which we are removing reagent</param>
    /// <param name="reagentId">The reagent to remove.</param>
    /// <param name="quantity">The amount of reagent to remove.</param>
    /// <returns>If the reagent to remove was found in the container.</returns>
    public bool RemoveReagent(Entity<SolutionComponent> soln, ReagentId reagentId, FixedPoint2 quantity)
    {
        return RemoveReagent(soln, new ReagentQuantity(reagentId, quantity));
    }

    /// <summary>
    ///     Moves some quantity of a solution from one solution to another.
    /// </summary>
    /// <param name="sourceUid">entity holding the source solution</param>
    /// <param name="targetUid">entity holding the target solution</param>
    /// <param name="source">source solution</param>
    /// <param name="target">target solution</param>
    /// <param name="quantity">quantity of solution to move from source to target. If this is a negative number, the source & target roles are reversed.</param>
    public bool TryTransferSolution(Entity<SolutionComponent> soln, Solution source, FixedPoint2 quantity)
    {
        var (uid, comp) = soln;
        var solution = comp.Solution;

        if (quantity < 0)
            throw new InvalidOperationException("Quantity must be positive");

        quantity = FixedPoint2.Min(quantity, solution.AvailableVolume, source.Volume);
        if (quantity == 0)
            return false;

        // TODO This should be made into a function that directly transfers reagents.
        // Currently this is quite inefficient.
        solution.AddSolution(source.SplitSolution(quantity), _prototypeManager);

        UpdateChemicals(soln);
        return true;
    }

    /// <summary>
    ///     Adds a solution to the container, if it can fully fit.
    /// </summary>
    /// <param name="targetUid">entity holding targetSolution</param>
    ///  <param name="targetSolution">entity holding targetSolution</param>
    /// <param name="toAdd">solution being added</param>
    /// <returns>If the solution could be added.</returns>
    public bool TryAddSolution(Entity<SolutionComponent> soln, Solution toAdd)
    {
        var (uid, comp) = soln;
        var solution = comp.Solution;

        if (toAdd.Volume == FixedPoint2.Zero)
            return true;
        if (toAdd.Volume > solution.AvailableVolume)
            return false;

        ForceAddSolution(soln, toAdd);
        return true;
    }

    /// <summary>
    ///     Adds as much of a solution to a container as can fit.
    /// </summary>
    /// <param name="targetUid">The entity containing <paramref cref="targetSolution"/></param>
    /// <param name="targetSolution">The solution being added to.</param>
    /// <param name="toAdd">The solution being added to <paramref cref="targetSolution"/></param>
    /// <returns>The quantity of the solution actually added.</returns>
    public FixedPoint2 AddSolution(Entity<SolutionComponent> soln, Solution toAdd)
    {
        var (uid, comp) = soln;
        var solution = comp.Solution;

        if (toAdd.Volume == FixedPoint2.Zero)
            return FixedPoint2.Zero;

        var quantity = FixedPoint2.Max(FixedPoint2.Zero, FixedPoint2.Min(toAdd.Volume, solution.AvailableVolume));
        if (quantity < toAdd.Volume)
            TryTransferSolution(soln, toAdd, quantity);
        else
            ForceAddSolution(soln, toAdd);

        return quantity;
    }

    /// <summary>
    ///     Adds a solution to a container and updates the container.
    /// </summary>
    /// <param name="targetUid">The entity containing <paramref cref="targetSolution"/></param>
    /// <param name="targetSolution">The solution being added to.</param>
    /// <param name="toAdd">The solution being added to <paramref cref="targetSolution"/></param>
    /// <returns>Whether any reagents were added to the solution.</returns>
    public bool ForceAddSolution(Entity<SolutionComponent> soln, Solution toAdd)
    {
        var (uid, comp) = soln;
        var solution = comp.Solution;

        if (toAdd.Volume == FixedPoint2.Zero)
            return false;

        solution.AddSolution(toAdd, _prototypeManager);
        UpdateChemicals(soln);
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
    public bool TryMixAndOverflow(Entity<SolutionComponent> soln, Solution toAdd, FixedPoint2 overflowThreshold, [MaybeNullWhen(false)] out Solution overflowingSolution)
    {
        var (uid, comp) = soln;
        var solution = comp.Solution;

        if (toAdd.Volume == 0 || overflowThreshold > solution.MaxVolume)
        {
            overflowingSolution = null;
            return false;
        }

        solution.AddSolution(toAdd, _prototypeManager);
        overflowingSolution = solution.SplitSolution(FixedPoint2.Max(FixedPoint2.Zero, solution.Volume - overflowThreshold));
        UpdateChemicals(soln);
        return true;
    }

    /// <summary>
    ///     Removes an amount from all reagents in a solution, adding it to a new solution.
    /// </summary>
    /// <param name="uid">The entity containing the solution.</param>
    /// <param name="solution">The solution to remove reagents from.</param>
    /// <param name="quantity">The amount to remove from every reagent in the solution.</param>
    /// <returns>A new solution containing every removed reagent from the original solution.</returns>
    public Solution RemoveEachReagent(Entity<SolutionComponent> soln, FixedPoint2 quantity)
    {
        var (uid, comp) = soln;
        var solution = comp.Solution;

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

        UpdateChemicals(soln);
        return removedSolution;
    }

    // Thermal energy and temperature management.

    #region Thermal Energy and Temperature

    /// <summary>
    ///     Sets the temperature of a solution to a new value and then checks for reaction processing.
    /// </summary>
    /// <param name="owner">The entity in which the solution is located.</param>
    /// <param name="solution">The solution to set the temperature of.</param>
    /// <param name="temperature">The new value to set the temperature to.</param>
    public void SetTemperature(Entity<SolutionComponent> soln, float temperature)
    {
        var (_, comp) = soln;
        var solution = comp.Solution;

        if (temperature == solution.Temperature)
            return;

        solution.Temperature = temperature;
        UpdateChemicals(soln);
    }

    /// <summary>
    ///     Sets the thermal energy of a solution to a new value and then checks for reaction processing.
    /// </summary>
    /// <param name="owner">The entity in which the solution is located.</param>
    /// <param name="solution">The solution to set the thermal energy of.</param>
    /// <param name="thermalEnergy">The new value to set the thermal energy to.</param>
    public void SetThermalEnergy(Entity<SolutionComponent> soln, float thermalEnergy)
    {
        var (_, comp) = soln;
        var solution = comp.Solution;

        var heatCap = solution.GetHeatCapacity(_prototypeManager);
        solution.Temperature = heatCap == 0 ? 0 : thermalEnergy / heatCap;
        UpdateChemicals(soln);
    }

    /// <summary>
    ///     Adds some thermal energy to a solution and then checks for reaction processing.
    /// </summary>
    /// <param name="owner">The entity in which the solution is located.</param>
    /// <param name="solution">The solution to set the thermal energy of.</param>
    /// <param name="thermalEnergy">The new value to set the thermal energy to.</param>
    public void AddThermalEnergy(Entity<SolutionComponent> soln, float thermalEnergy)
    {
        var (_, comp) = soln;
        var solution = comp.Solution;

        if (thermalEnergy == 0.0f)
            return;

        var heatCap = solution.GetHeatCapacity(_prototypeManager);
        solution.Temperature += heatCap == 0 ? 0 : thermalEnergy / heatCap;
        UpdateChemicals(soln);
    }

    #endregion Thermal Energy and Temperature

    #region Event Handlers

    private void OnComponentInit(EntityUid uid, SolutionComponent comp, ComponentInit args)
    {
        comp.Solution.ValidateSolution();
    }

    private void OnComponentStartup(EntityUid uid, SolutionComponent comp, ComponentStartup args)
    {
        UpdateChemicals((uid, comp));
    }

    private void OnComponentShutdown(EntityUid uid, SolutionComponent comp, ComponentShutdown args)
    {
        RemoveAllSolution((uid, comp));
    }

    #endregion Event Handlers
}
