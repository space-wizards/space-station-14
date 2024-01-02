using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Dependency = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// The event raised whenever a solution entity is modified.
/// </summary>
/// <remarks>
/// Raised after chemcial reactions and <see cref="SolutionOverflowEvent"/> are handled.
/// </remarks>
/// <param name="Solution">The solution entity that has been modified.</param>
[ByRefEvent]
public readonly partial record struct SolutionChangedEvent(Entity<SolutionComponent> Solution);

/// <summary>
/// The event raised whenever a solution entity is filled past its capacity.
/// </summary>
/// <param name="Solution">The solution entity that has been overfilled.</param>
/// <param name="Overflow">The amount by which the solution entity has been overfilled.</param>
[ByRefEvent]
public partial record struct SolutionOverflowEvent(Entity<SolutionComponent> Solution, FixedPoint2 Overflow)
{
    /// <summary>The solution entity that has been overfilled.</summary>
    public readonly Entity<SolutionComponent> Solution = Solution;
    /// <summary>The amount by which the solution entity has been overfilled.</summary>
    public readonly FixedPoint2 Overflow = Overflow;
    /// <summary>Whether any of the event handlers for this event have handled overflow behaviour.</summary>
    public bool Handled = false;
}

/// <summary>
/// Part of Chemistry system deal with SolutionContainers
/// </summary>
[UsedImplicitly]
public abstract partial class SharedSolutionContainerSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly ChemicalReactionSystem ChemicalReactionSystem = default!;
    [Dependency] protected readonly ExamineSystemShared ExamineSystem = default!;
    [Dependency] protected readonly SharedAppearanceSystem AppearanceSystem = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeRelays();

        SubscribeLocalEvent<SolutionComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SolutionComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SolutionComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<SolutionContainerManagerComponent, ComponentInit>(OnComponentInit);

        SubscribeLocalEvent<ExaminableSolutionComponent, ExaminedEvent>(OnExamineSolution);
        SubscribeLocalEvent<ExaminableSolutionComponent, GetVerbsEvent<ExamineVerb>>(OnSolutionExaminableVerb);
    }


    /// <summary>
    /// Attempts to resolve a solution associated with an entity.
    /// </summary>
    /// <param name="container">The entity that holdes the container the solution entity is in.</param>
    /// <param name="name">The name of the solution entities container.</param>
    /// <param name="entity">A reference to a solution entity to load the associated solution entity into. Will be unchanged if not null.</param>
    /// <param name="solution">Returns the solution state of the solution entity.</param>
    /// <returns>Whether the solution was successfully resolved.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ResolveSolution(Entity<SolutionContainerManagerComponent?> container, string? name, [NotNullWhen(true)] ref Entity<SolutionComponent>? entity, [NotNullWhen(true)] out Solution? solution)
    {
        if (!ResolveSolution(container, name, ref entity))
        {
            solution = null;
            return false;
        }

        solution = entity.Value.Comp.Solution;
        return true;
    }

    /// <inheritdoc cref="ResolveSolution"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ResolveSolution(Entity<SolutionContainerManagerComponent?> container, string? name, [NotNullWhen(true)] ref Entity<SolutionComponent>? entity)
    {
        if (entity is not null)
        {
            DebugTools.Assert(TryGetSolution(container, name, out var debugEnt)
                              && debugEnt.Value.Owner == entity.Value.Owner);
            return true;
        }

        return TryGetSolution(container, name, out entity);
    }

    /// <summary>
    /// Attempts to fetch a solution entity associated with an entity.
    /// </summary>
    /// <remarks>
    /// If the solution entity will be frequently accessed please use the equivalent <see cref="ResolveSolution"/> method and cache the result.
    /// </remarks>
    /// <param name="container">The entity the solution entity should be associated with.</param>
    /// <param name="name">The name of the solution entity to fetch.</param>
    /// <param name="entity">Returns the solution entity that was fetched.</param>
    /// <param name="solution">Returns the solution state of the solution entity that was fetched.</param>
    /// <returns></returns>
    public bool TryGetSolution(Entity<SolutionContainerManagerComponent?> container, string? name, [NotNullWhen(true)] out Entity<SolutionComponent>? entity, [NotNullWhen(true)] out Solution? solution)
    {
        if (!TryGetSolution(container, name, out entity))
        {
            solution = null;
            return false;
        }

        solution = entity.Value.Comp.Solution;
        return true;
    }

    /// <inheritdoc cref="TryGetSolution"/>
    public bool TryGetSolution(Entity<SolutionContainerManagerComponent?> container, string? name, [NotNullWhen(true)] out Entity<SolutionComponent>? entity)
    {
        EntityUid uid;
        if (name is null)
            uid = container;
        else if (
            ContainerSystem.TryGetContainer(container, $"solution@{name}", out var solutionContainer) &&
            solutionContainer is ContainerSlot solutionSlot &&
            solutionSlot.ContainedEntity is { } containedSolution
        )
            uid = containedSolution;
        else
        {
            entity = null;
            return false;
        }

        if (!TryComp(uid, out SolutionComponent? comp))
        {
            entity = null;
            return false;
        }

        entity = (uid, comp);
        return true;
    }

    /// <summary>
    /// Version of TryGetSolution that doesn't take or return an entity.
    /// Used for prototypes and with old code parity.
    public bool TryGetSolution(SolutionContainerManagerComponent container, string name, [NotNullWhen(true)] out Solution? solution)
    {
        solution = null;
        if (container.Solutions == null)
            return false;

        return container.Solutions.TryGetValue(name, out solution);
    }

    public IEnumerable<(string? Name, Entity<SolutionComponent> Solution)> EnumerateSolutions(Entity<SolutionContainerManagerComponent?> container, bool includeSelf = true)
    {
        if (includeSelf && TryComp(container, out SolutionComponent? solutionComp))
            yield return (null, (container.Owner, solutionComp));

        if (!Resolve(container, ref container.Comp, logMissing: false))
            yield break;

        foreach (var name in container.Comp.Containers)
        {
            if (ContainerSystem.GetContainer(container, $"solution@{name}") is ContainerSlot slot && slot.ContainedEntity is { } solutionId)
                yield return (name, (solutionId, Comp<SolutionComponent>(solutionId)));
        }
    }

    public IEnumerable<(string Name, Solution Solution)> EnumerateSolutions(SolutionContainerManagerComponent container)
    {
        if (container.Solutions is not { Count: > 0 } solutions)
            yield break;

        foreach (var (name, solution) in solutions)
        {
            yield return (name, solution);
        }
    }


    protected void UpdateAppearance(Entity<AppearanceComponent?> container, Entity<SolutionComponent, ContainedSolutionComponent> soln)
    {
        var (uid, appearanceComponent) = container;
        if (!HasComp<SolutionContainerVisualsComponent>(uid) || !Resolve(uid, ref appearanceComponent, logMissing: false))
            return;

        var (_, comp, relation) = soln;
        var solution = comp.Solution;

        AppearanceSystem.SetData(uid, SolutionContainerVisuals.FillFraction, solution.FillFraction, appearanceComponent);
        AppearanceSystem.SetData(uid, SolutionContainerVisuals.Color, solution.GetColor(PrototypeManager), appearanceComponent);
        AppearanceSystem.SetData(uid, SolutionContainerVisuals.SolutionName, relation.ContainerName, appearanceComponent);

        if (solution.GetPrimaryReagentId() is { } reagent)
            AppearanceSystem.SetData(uid, SolutionContainerVisuals.BaseOverride, reagent.ToString(), appearanceComponent);
        else
            AppearanceSystem.SetData(uid, SolutionContainerVisuals.BaseOverride, string.Empty, appearanceComponent);
    }


    public FixedPoint2 GetTotalPrototypeQuantity(EntityUid owner, string reagentId)
    {
        var reagentQuantity = FixedPoint2.New(0);
        if (EntityManager.EntityExists(owner)
            && EntityManager.TryGetComponent(owner, out SolutionContainerManagerComponent? managerComponent))
        {
            foreach (var (_, soln) in EnumerateSolutions((owner, managerComponent)))
            {
                var solution = soln.Comp.Solution;
                reagentQuantity += solution.GetTotalPrototypeQuantity(reagentId);
            }
        }

        return reagentQuantity;
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
            ChemicalReactionSystem.FullyReactSolution(soln, mixerComponent);

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

        AppearanceSystem.SetData(uid, SolutionContainerVisuals.FillFraction, solution.FillFraction, appearanceComponent);
        AppearanceSystem.SetData(uid, SolutionContainerVisuals.Color, solution.GetColor(PrototypeManager), appearanceComponent);

        if (solution.GetPrimaryReagentId() is { } reagent)
            AppearanceSystem.SetData(uid, SolutionContainerVisuals.BaseOverride, reagent.ToString(), appearanceComponent);
        else
            AppearanceSystem.SetData(uid, SolutionContainerVisuals.BaseOverride, string.Empty, appearanceComponent);
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
            var proto = PrototypeManager.Index<ReagentPrototype>(reagentQuantity.Reagent.Prototype);
            solution.AddReagent(proto, acceptedQuantity, temperature.Value, PrototypeManager);
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
    /// <returns>If all the reagent could be added.</returns>
    [PublicAPI]
    public bool TryAddReagent(Entity<SolutionComponent> soln, string prototype, FixedPoint2 quantity, float? temperature = null, ReagentData? data = null)
        => TryAddReagent(soln, new ReagentQuantity(prototype, quantity, data), out _, temperature);

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
        solution.AddSolution(source.SplitSolution(quantity), PrototypeManager);

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

        solution.AddSolution(toAdd, PrototypeManager);
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

        solution.AddSolution(toAdd, PrototypeManager);
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

        var heatCap = solution.GetHeatCapacity(PrototypeManager);
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

        var heatCap = solution.GetHeatCapacity(PrototypeManager);
        solution.Temperature += heatCap == 0 ? 0 : thermalEnergy / heatCap;
        UpdateChemicals(soln);
    }

    #endregion Thermal Energy and Temperature

    #region Event Handlers

    private void OnComponentInit(Entity<SolutionComponent> entity, ref ComponentInit args)
    {
        entity.Comp.Solution.ValidateSolution();
    }

    private void OnComponentStartup(Entity<SolutionComponent> entity, ref ComponentStartup args)
    {
        UpdateChemicals(entity);
    }

    private void OnComponentShutdown(Entity<SolutionComponent> entity, ref ComponentShutdown args)
    {
        RemoveAllSolution(entity);
    }

    private void OnComponentInit(Entity<SolutionContainerManagerComponent> entity, ref ComponentInit args)
    {
        if (entity.Comp.Containers is not { Count: > 0 } containers)
            return;

        var containerManager = EnsureComp<ContainerManagerComponent>(entity);
        foreach (var name in containers)
        {
            // The actual solution entity should be directly held within the corresponding slot.
            ContainerSystem.EnsureContainer<ContainerSlot>(entity.Owner, $"solution@{name}", containerManager);
        }
    }

    private void OnExamineSolution(Entity<ExaminableSolutionComponent> entity, ref ExaminedEvent args)
    {
        if (!TryGetSolution(entity.Owner, entity.Comp.Solution, out _, out var solution))
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

        var reagentPrototypes = solution.GetReagentPrototypes(PrototypeManager);

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

    private void OnSolutionExaminableVerb(Entity<ExaminableSolutionComponent> entity, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var scanEvent = new SolutionScanEvent();
        RaiseLocalEvent(args.User, scanEvent);
        if (!scanEvent.CanScan)
        {
            return;
        }

        if (!TryGetSolution(args.Target, entity.Comp.Solution, out _, out var solutionHolder))
        {
            return;
        }

        var target = args.Target;
        var user = args.User;
        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = GetSolutionExamine(solutionHolder);
                ExamineSystem.SendExamineTooltip(user, target, markup, false, false);
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

        var reagentPrototypes = solution.GetReagentPrototypes(PrototypeManager);

        // Sort the reagents by amount, descending then alphabetically
        var sortedReagentPrototypes = reagentPrototypes
            .OrderByDescending(pair => pair.Value.Value)
            .ThenBy(pair => pair.Key.LocalizedName);

        foreach (var (proto, quantity) in sortedReagentPrototypes)
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
