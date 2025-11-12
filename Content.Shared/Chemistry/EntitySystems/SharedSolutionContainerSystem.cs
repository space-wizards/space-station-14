using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Localizations;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
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

[ByRefEvent]
public partial record struct SolutionAccessAttemptEvent(string SolutionName)
{
    public bool Cancelled;
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
    [Dependency] protected readonly OpenableSystem Openable = default!;
    [Dependency] protected readonly SharedAppearanceSystem AppearanceSystem = default!;
    [Dependency] protected readonly SharedHandsSystem Hands = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] protected readonly MetaDataSystem MetaDataSys = default!;
    [Dependency] protected readonly INetManager NetManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeRelays();

        SubscribeLocalEvent<SolutionComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SolutionComponent, ComponentStartup>(OnSolutionStartup);
        SubscribeLocalEvent<SolutionComponent, ComponentShutdown>(OnSolutionShutdown);
        SubscribeLocalEvent<SolutionContainerManagerComponent, ComponentInit>(OnContainerManagerInit);
        SubscribeLocalEvent<ExaminableSolutionComponent, ExaminedEvent>(OnExamineSolution);
        SubscribeLocalEvent<ExaminableSolutionComponent, GetVerbsEvent<ExamineVerb>>(OnSolutionExaminableVerb);
        SubscribeLocalEvent<SolutionContainerManagerComponent, MapInitEvent>(OnMapInit);

        if (NetManager.IsServer)
        {
            SubscribeLocalEvent<SolutionContainerManagerComponent, ComponentShutdown>(OnContainerManagerShutdown);
            SubscribeLocalEvent<ContainedSolutionComponent, ComponentShutdown>(OnContainedSolutionShutdown);
        }
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
    /// /// <param name="errorOnMissing">Should we print an error if the solution specified by name is missing</param>
    /// <returns></returns>
    public bool TryGetSolution(
        Entity<SolutionContainerManagerComponent?> container,
        string? name,
        [NotNullWhen(true)] out Entity<SolutionComponent>? entity,
        [NotNullWhen(true)] out Solution? solution,
        bool errorOnMissing = false)
    {
        if (!TryGetSolution(container, name, out entity, errorOnMissing: errorOnMissing))
        {
            solution = null;
            return false;
        }

        solution = entity.Value.Comp.Solution;
        return true;
    }

    /// <inheritdoc cref="TryGetSolution"/>
    public bool TryGetSolution(
        Entity<SolutionContainerManagerComponent?> container,
        string? name,
        [NotNullWhen(true)] out Entity<SolutionComponent>? entity,
        bool errorOnMissing = false)
    {
        // use connected container instead of entity from arguments, if it exists.
        var ev = new GetConnectedContainerEvent();
        RaiseLocalEvent(container, ref ev);
        if (ev.ContainerEntity.HasValue)
            container = ev.ContainerEntity.Value;

        EntityUid uid;
        if (name is null)
            uid = container;
        else if (
            ContainerSystem.TryGetContainer(container, $"solution@{name}", out var solutionContainer) &&
            solutionContainer is ContainerSlot solutionSlot &&
            solutionSlot.ContainedEntity is { } containedSolution
        )
        {
            var attemptEv = new SolutionAccessAttemptEvent(name);
            RaiseLocalEvent(container, ref attemptEv);

            if (attemptEv.Cancelled)
            {
                entity = null;
                return false;
            }

            uid = containedSolution;
        }
        else
        {
            entity = null;
            if (!errorOnMissing)
                return false;
            Log.Error($"{ToPrettyString(container)} does not have a solution with ID: {name}");
            return false;
        }

        if (!TryComp(uid, out SolutionComponent? comp))
        {
            entity = null;
            if (!errorOnMissing)
                return false;
            Log.Error($"{ToPrettyString(container)} does not have a solution with ID: {name}");
            return false;
        }

        entity = (uid, comp);
        return true;
    }

    /// <summary>
    /// Version of TryGetSolution that doesn't take or return an entity.
    /// Used for prototypes and with old code parity.
    public bool TryGetSolution(SolutionContainerManagerComponent container,
        string name,
        [NotNullWhen(true)] out Solution? solution,
        bool errorOnMissing = false)
    {
        solution = null;
        if (container.Solutions != null)
            return container.Solutions.TryGetValue(name, out solution);
        if (!errorOnMissing)
            return false;
        Log.Error($"{container} does not have a solution with ID: {name}");
        return false;
    }

    public IEnumerable<(string? Name, Entity<SolutionComponent> Solution)> EnumerateSolutions(Entity<SolutionContainerManagerComponent?> container, bool includeSelf = true)
    {
        if (includeSelf && TryComp(container, out SolutionComponent? solutionComp))
            yield return (null, (container.Owner, solutionComp));

        if (!Resolve(container, ref container.Comp, logMissing: false))
            yield break;

        foreach (var name in container.Comp.Containers)
        {
            var attemptEv = new SolutionAccessAttemptEvent(name);
            RaiseLocalEvent(container, ref attemptEv);

            if (attemptEv.Cancelled)
                continue;

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
    }


    public FixedPoint2 GetTotalPrototypeQuantity(EntityUid owner, string reagentId)
    {
        var reagentQuantity = FixedPoint2.New(0);
        if (Exists(owner)
            && TryComp(owner, out SolutionContainerManagerComponent? managerComponent))
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

        if (!Exists(uid) || !Resolve(uid, ref appearanceComponent, false))
            return;

        AppearanceSystem.SetData(uid, SolutionContainerVisuals.FillFraction, solution.FillFraction, appearanceComponent);
        AppearanceSystem.SetData(uid, SolutionContainerVisuals.Color, solution.GetColor(PrototypeManager), appearanceComponent);

        if (solution.GetPrimaryReagentId() is { } reagent)
            AppearanceSystem.SetData(uid, SolutionContainerVisuals.BaseOverride, reagent.ToString(), appearanceComponent);
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
    [Obsolete("Use SplitSolutionWithout with params ProtoId<ReagentPrototype>")]
    public Solution SplitSolutionWithout(Entity<SolutionComponent> soln, FixedPoint2 quantity, params string[] reagents)
    {
        var (uid, comp) = soln;
        var solution = comp.Solution;

        var splitSol = solution.SplitSolutionWithout(quantity, reagents);
        UpdateChemicals(soln);
        return splitSol;
    }

    /// <summary>
    /// Splits a solution without the specified reagent(s).
    /// </summary>
    public Solution SplitSolutionWithout(Entity<SolutionComponent> soln, FixedPoint2 quantity, params ProtoId<ReagentPrototype>[] reagents)
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
    public bool TryAddReagent(Entity<SolutionComponent> soln, string prototype, FixedPoint2 quantity, float? temperature = null, List<ReagentData>? data = null)
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
    public bool TryAddReagent(Entity<SolutionComponent> soln, string prototype, FixedPoint2 quantity, out FixedPoint2 acceptedQuantity, float? temperature = null, List<ReagentData>? data = null)
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
    /// <param name="container">Solution container from which we are removing reagent.</param>
    /// <param name="reagentQuantity">The reagent to remove.</param>
    /// <returns>The amount of reagent that was removed.</returns>
    public FixedPoint2 RemoveReagent(Entity<SolutionComponent> soln, ReagentQuantity reagentQuantity)
    {
        var (uid, comp) = soln;
        var solution = comp.Solution;

        var quant = solution.RemoveReagent(reagentQuantity);
        if (quant <= FixedPoint2.Zero)
            return FixedPoint2.Zero;

        UpdateChemicals(soln);
        return quant;
    }

    /// <summary>
    ///     Removes reagent from a container.
    /// </summary>
    /// <param name="targetUid"></param>
    /// <param name="container">Solution container from which we are removing reagent</param>
    /// <param name="prototype">The Id of the reagent to remove.</param>
    /// <param name="quantity">The amount of reagent to remove.</param>
    /// <returns>The amount of reagent that was removed.</returns>
    public FixedPoint2 RemoveReagent(Entity<SolutionComponent> soln, string prototype, FixedPoint2 quantity, List<ReagentData>? data = null)
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
    /// <returns>The amount of reagent that was removed.</returns>
    public FixedPoint2 RemoveReagent(Entity<SolutionComponent> soln, ReagentId reagentId, FixedPoint2 quantity)
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
    /// <param name="targetSolution">entity holding targetSolution</param>
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
    ///     Adds as much of a solution to a container as can fit and updates the container.
    /// </summary>
    /// <param name="targetUid">The entity containing <paramref cref="targetSolution"/></param>
    /// <param name="targetSolution">The solution being added to.</param>
    /// <param name="toAdd">The solution being added to <paramref cref="targetSolution"/>. This solution is not modified.</param>
    /// <returns>The quantity of the solution actually added.</returns>
    public FixedPoint2 AddSolution(Entity<SolutionComponent> soln, Solution toAdd)
    {
        var solution = soln.Comp.Solution;

        if (toAdd.Volume == FixedPoint2.Zero)
            return FixedPoint2.Zero;

        var quantity = FixedPoint2.Max(FixedPoint2.Zero, FixedPoint2.Min(toAdd.Volume, solution.AvailableVolume));
        if (quantity < toAdd.Volume)
        {
            // TODO: This should be made into a function that directly transfers reagents.
            // Currently this is quite inefficient.
            solution.AddSolution(toAdd.Clone().SplitSolution(quantity), PrototypeManager);
        }
        else
            solution.AddSolution(toAdd, PrototypeManager);

        UpdateChemicals(soln);
        return quantity;
    }

    /// <summary>
    ///     Adds a solution to a container and updates the container.
    ///     This can exceed the maximum volume of the solution added to.
    /// </summary>
    /// <param name="targetUid">The entity containing <paramref cref="targetSolution"/></param>
    /// <param name="targetSolution">The solution being added to.</param>
    /// <param name="toAdd">The solution being added to <paramref cref="targetSolution"/>. This solution is not modified.</param>
    /// <returns>Whether any reagents were added to the solution.</returns>
    public bool ForceAddSolution(Entity<SolutionComponent> soln, Solution toAdd)
    {
        var solution = soln.Comp.Solution;

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
    // TODO: ENERGY CONSERVATION!!! Nuke this once we have HeatContainers and use methods which properly conserve energy and model heat transfer correctly!

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

    /// <summary>
    /// Same as <see cref="AddThermalEnergy"/> but clamps the value between two temperature values.
    /// </summary>
    /// <param name="soln">Solution we're adjusting the energy of</param>
    /// <param name="thermalEnergy">Thermal energy we're adding or removing</param>
    /// <param name="min">Min desired temperature</param>
    /// <param name="max">Max desired temperature</param>
    public void AddThermalEnergyClamped(Entity<SolutionComponent> soln, float thermalEnergy, float min, float max)
    {
        var solution = soln.Comp.Solution;

        if (thermalEnergy == 0.0f)
            return;

        var heatCap = solution.GetHeatCapacity(PrototypeManager);
        var deltaT = thermalEnergy / heatCap;
        solution.Temperature = Math.Clamp(solution.Temperature + deltaT, min, max);
        UpdateChemicals(soln);
    }

    #endregion Thermal Energy and Temperature

    #region Event Handlers

    private void OnComponentInit(Entity<SolutionComponent> entity, ref ComponentInit args)
    {
        entity.Comp.Solution.ValidateSolution();
    }

    private void OnSolutionStartup(Entity<SolutionComponent> entity, ref ComponentStartup args)
    {
        UpdateChemicals(entity);
    }

    private void OnSolutionShutdown(Entity<SolutionComponent> entity, ref ComponentShutdown args)
    {
        RemoveAllSolution(entity);
    }

    private void OnContainerManagerInit(Entity<SolutionContainerManagerComponent> entity, ref ComponentInit args)
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

    /// <summary>
    ///     Shift click examine.
    /// </summary>
    private void OnExamineSolution(Entity<ExaminableSolutionComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange ||
            !CanSeeHiddenSolution(entity, args.Examiner) ||
            !TryGetSolution(entity.Owner, entity.Comp.Solution, out _, out var solution))
            return;

        using (args.PushGroup(nameof(ExaminableSolutionComponent)))
        {

            var primaryReagent = solution.GetPrimaryReagentId();

            // If there's no primary reagent, assume the solution is empty and exit early
            if (string.IsNullOrEmpty(primaryReagent?.Prototype) ||
                !PrototypeManager.Resolve<ReagentPrototype>(primaryReagent.Value.Prototype, out var primary))
            {
                args.PushMarkup(Loc.GetString(entity.Comp.LocVolume, ("fillLevel", ExaminedVolumeDisplay.Empty)));
                return;
            }

            // Push amount of reagent

            args.PushMarkup(Loc.GetString(entity.Comp.LocVolume,
                                ("fillLevel", ExaminedVolume(entity, solution, args.Examiner)),
                                ("current", solution.Volume),
                                ("max", solution.MaxVolume)));

            // Push the physical description of the primary reagent

            var colorHex = solution.GetColor(PrototypeManager)
                .ToHexNoAlpha(); //TODO: If the chem has a dark color, the examine text becomes black on a black background, which is unreadable.

            args.PushMarkup(Loc.GetString(entity.Comp.LocPhysicalQuality,
                                        ("color", colorHex),
                                        ("desc", primary.LocalizedPhysicalDescription),
                                        ("chemCount", solution.Contents.Count)));

            // Push the recognizable reagents

            // Sort the reagents by amount, descending then alphabetically
            var sortedReagentPrototypes = solution.GetReagentPrototypes(PrototypeManager)
                .OrderByDescending(pair => pair.Value.Value)
                .ThenBy(pair => pair.Key.LocalizedName);

            // Collect recognizable reagents, like water or beer
            var recognized = new List<string>();
            foreach (var keyValuePair in sortedReagentPrototypes)
            {
                var proto = keyValuePair.Key;
                if (!proto.Recognizable)
                {
                    continue;
                }

                recognized.Add(Loc.GetString("examinable-solution-recognized",
                                            ("color", proto.SubstanceColor.ToHexNoAlpha()),
                                            ("chemical", proto.LocalizedName)));
            }

            if (recognized.Count == 0)
                return;

            var msg = ContentLocalizationManager.FormatList(recognized);

            // Finally push the full message
            args.PushMarkup(Loc.GetString(entity.Comp.LocRecognizableReagents,
                ("recognizedString", msg)));
        }
    }

    /// <returns>An enum for how to display the solution.</returns>
    public ExaminedVolumeDisplay ExaminedVolume(Entity<ExaminableSolutionComponent> ent, Solution sol, EntityUid? examiner = null)
    {
        // Exact measurement
        if (ent.Comp.ExactVolume)
            return ExaminedVolumeDisplay.Exact;

        // General approximation
        return (int)PercentFull(sol) switch
        {
            100 => ExaminedVolumeDisplay.Full,
            > 66 => ExaminedVolumeDisplay.MostlyFull,
            > 33 => HalfEmptyOrHalfFull(examiner),
            > 0 => ExaminedVolumeDisplay.MostlyEmpty,
            _ => ExaminedVolumeDisplay.Empty,
        };
    }

    // Some spessmen see half full, some see half empty, but always the same one.
    private ExaminedVolumeDisplay HalfEmptyOrHalfFull(EntityUid? examiner = null)
    {
        // Optimistic when un-observed
        if (examiner == null)
            return ExaminedVolumeDisplay.HalfFull;

        var meta = MetaData(examiner.Value);
        if (meta.EntityName.Length > 0 &&
            string.Compare(meta.EntityName.Substring(0, 1), "m", StringComparison.InvariantCultureIgnoreCase) > 0)
            return ExaminedVolumeDisplay.HalfFull;

        return ExaminedVolumeDisplay.HalfEmpty;
    }

    /// <summary>
    ///     Full reagent scan, such as with chemical analysis goggles.
    /// </summary>
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

        if (!CanSeeHiddenSolution(entity, args.User))
            return;

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
            msg.AddMarkupOrThrow(Loc.GetString("scannable-solution-empty-container"));
            return msg;
        }

        msg.AddMarkupOrThrow(Loc.GetString("scannable-solution-main-text"));

        var reagentPrototypes = solution.GetReagentPrototypes(PrototypeManager);

        // Sort the reagents by amount, descending then alphabetically
        var sortedReagentPrototypes = reagentPrototypes
            .OrderByDescending(pair => pair.Value.Value)
            .ThenBy(pair => pair.Key.LocalizedName);

        foreach (var (proto, quantity) in sortedReagentPrototypes)
        {
            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("scannable-solution-chemical"
                , ("type", proto.LocalizedName)
                , ("color", proto.SubstanceColor.ToHexNoAlpha())
                , ("amount", quantity)));
        }

        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("scannable-solution-temperature", ("temperature", Math.Round(solution.Temperature))));

        return msg;
    }

    /// <summary>
    ///     Check if an examinable solution is hidden by something.
    /// </summary>
    private bool CanSeeHiddenSolution(Entity<ExaminableSolutionComponent> entity, EntityUid examiner)
    {
        // If not held-only then it's always visible.
        if (entity.Comp.HeldOnly && !Hands.IsHolding(examiner, entity, out _))
            return false;

        if (!entity.Comp.ExaminableWhileClosed && Openable.IsClosed(entity.Owner, predicted: true))
            return false;

        return true;
    }

    private void OnMapInit(Entity<SolutionContainerManagerComponent> entity, ref MapInitEvent args)
    {
        EnsureAllSolutions(entity);
    }

    private void OnContainerManagerShutdown(Entity<SolutionContainerManagerComponent> entity, ref ComponentShutdown args)
    {
        foreach (var name in entity.Comp.Containers)
        {
            if (ContainerSystem.TryGetContainer(entity, $"solution@{name}", out var solutionContainer))
                ContainerSystem.ShutdownContainer(solutionContainer);
        }
        entity.Comp.Containers.Clear();
    }

    private void OnContainedSolutionShutdown(Entity<ContainedSolutionComponent> entity, ref ComponentShutdown args)
    {
        if (TryComp(entity.Comp.Container, out SolutionContainerManagerComponent? container))
        {
            container.Containers.Remove(entity.Comp.ContainerName);
            Dirty(entity.Comp.Container, container);
        }

        if (ContainerSystem.TryGetContainer(entity, $"solution@{entity.Comp.ContainerName}", out var solutionContainer))
            ContainerSystem.ShutdownContainer(solutionContainer);
    }

    #endregion Event Handlers

    public bool EnsureSolution(
        Entity<MetaDataComponent?> entity,
        string name,
        [NotNullWhen(true)] out Solution? solution,
        FixedPoint2 maxVol = default)
    {
        return EnsureSolution(entity, name, maxVol, null, out _, out solution);
    }

    public bool EnsureSolution(
        Entity<MetaDataComponent?> entity,
        string name,
        out bool existed,
        [NotNullWhen(true)] out Solution? solution,
        FixedPoint2 maxVol = default)
    {
        return EnsureSolution(entity, name, maxVol, null, out existed, out solution);
    }

    public bool EnsureSolution(
        Entity<MetaDataComponent?> entity,
        string name,
        FixedPoint2 maxVol,
        Solution? prototype,
        out bool existed,
        [NotNullWhen(true)] out Solution? solution)
    {
        solution = null;
        existed = false;

        var (uid, meta) = entity;
        if (!Resolve(uid, ref meta))
            throw new InvalidOperationException("Attempted to ensure solution on invalid entity.");
        var manager = EnsureComp<SolutionContainerManagerComponent>(uid);
        if (meta.EntityLifeStage >= EntityLifeStage.MapInitialized)
        {
            EnsureSolutionEntity((uid, manager), name, out existed,
                out var solEnt, maxVol, prototype);
            solution = solEnt!.Value.Comp.Solution;
            return true;
        }
        solution = EnsureSolutionPrototype((uid, manager), name, maxVol, prototype, out existed);
        return true;
    }

    public void EnsureAllSolutions(Entity<SolutionContainerManagerComponent> entity)
    {
        if (NetManager.IsClient)
            return;

        if (entity.Comp.Solutions is not { } prototypes)
            return;

        foreach (var (name, prototype) in prototypes)
        {
            EnsureSolutionEntity((entity.Owner, entity.Comp), name, out _, out _, prototype.MaxVolume, prototype);
        }

        entity.Comp.Solutions = null;
        Dirty(entity);
    }

    public bool EnsureSolutionEntity(
        Entity<SolutionContainerManagerComponent?> entity,
        string name,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solutionEntity,
        FixedPoint2 maxVol = default) =>
        EnsureSolutionEntity(entity, name, out _, out solutionEntity, maxVol);

    public bool EnsureSolutionEntity(
        Entity<SolutionContainerManagerComponent?> entity,
        string name,
        out bool existed,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solutionEntity,
        FixedPoint2 maxVol = default,
        Solution? prototype = null
        )
    {
        existed = true;
        solutionEntity = null;

        var (uid, container) = entity;

        var solutionSlot = ContainerSystem.EnsureContainer<ContainerSlot>(uid, $"solution@{name}", out existed);
        if (!Resolve(uid, ref container, logMissing: false))
        {
            existed = false;
            container = AddComp<SolutionContainerManagerComponent>(uid);
            container.Containers.Add(name);
            if (NetManager.IsClient)
                return false;
        }
        else if (!existed)
        {
            container.Containers.Add(name);
            Dirty(uid, container);
        }

        var needsInit = false;
        SolutionComponent solutionComp;
        if (solutionSlot.ContainedEntity is not { } solutionId)
        {
            if (NetManager.IsClient)
                return false;
            prototype ??= new() { MaxVolume = maxVol };
            prototype.Name = name;
            (solutionId, solutionComp, _) = SpawnSolutionUninitialized(solutionSlot, name, maxVol, prototype);
            existed = false;
            needsInit = true;
            Dirty(uid, container);
        }
        else
        {
            solutionComp = Comp<SolutionComponent>(solutionId);
            DebugTools.Assert(TryComp(solutionId, out ContainedSolutionComponent? relation) && relation.Container == uid && relation.ContainerName == name);
            DebugTools.Assert(solutionComp.Solution.Name == name);

            var solution = solutionComp.Solution;
            solution.MaxVolume = FixedPoint2.Max(solution.MaxVolume, maxVol);

            // Depending on MapInitEvent order some systems can ensure solution empty solutions and conflict with the prototype solutions.
            // We want the reagents from the prototype to exist even if something else already created the solution.
            if (prototype is { Volume.Value: > 0 })
                solution.AddSolution(prototype, PrototypeManager);

            Dirty(solutionId, solutionComp);
        }

        if (needsInit)
            EntityManager.InitializeAndStartEntity(solutionId, Transform(solutionId).MapID);
        solutionEntity = (solutionId, solutionComp);
        return true;
    }

    private Solution EnsureSolutionPrototype(Entity<SolutionContainerManagerComponent?> entity, string name, FixedPoint2 maxVol, Solution? prototype, out bool existed)
    {
        existed = true;

        var (uid, container) = entity;
        if (!Resolve(uid, ref container, logMissing: false))
        {
            container = AddComp<SolutionContainerManagerComponent>(uid);
            existed = false;
        }

        if (container.Solutions is null)
            container.Solutions = new(SolutionContainerManagerComponent.DefaultCapacity);

        if (!container.Solutions.TryGetValue(name, out var solution))
        {
            solution = prototype ?? new() { Name = name, MaxVolume = maxVol };
            container.Solutions.Add(name, solution);
            existed = false;
        }
        else
            solution.MaxVolume = FixedPoint2.Max(solution.MaxVolume, maxVol);

        Dirty(uid, container);
        return solution;
    }

    private Entity<SolutionComponent, ContainedSolutionComponent> SpawnSolutionUninitialized(ContainerSlot container, string name, FixedPoint2 maxVol, Solution prototype)
    {
        var coords = new EntityCoordinates(container.Owner, Vector2.Zero);
        var uid = EntityManager.CreateEntityUninitialized(null, coords, null);

        var solution = new SolutionComponent() { Solution = prototype };
        AddComp(uid, solution);

        var relation = new ContainedSolutionComponent() { Container = container.Owner, ContainerName = name };
        AddComp(uid, relation);

        MetaDataSys.SetEntityName(uid, $"solution - {name}", raiseEvents: false);
        ContainerSystem.Insert(uid, container, force: true);

        return (uid, solution, relation);
    }

    public void AdjustDissolvedReagent(
        Entity<SolutionComponent> dissolvedSolution,
        FixedPoint2 volume,
        ReagentId reagent,
        float concentrationChange)
    {
        if (concentrationChange == 0)
            return;
        var dissolvedSol = dissolvedSolution.Comp.Solution;
        var amtChange =
            GetReagentQuantityFromConcentration(dissolvedSolution, volume, MathF.Abs(concentrationChange));
        if (concentrationChange > 0)
        {
            dissolvedSol.AddReagent(reagent, amtChange);
        }
        else
        {
            dissolvedSol.RemoveReagent(reagent, amtChange);
        }
        UpdateChemicals(dissolvedSolution);
    }

    public FixedPoint2 GetReagentQuantityFromConcentration(Entity<SolutionComponent> dissolvedSolution,
        FixedPoint2 volume, float concentration)
    {
        var dissolvedSol = dissolvedSolution.Comp.Solution;
        if (volume == 0
            || dissolvedSol.Volume == 0)
            return 0;
        return concentration * volume;
    }

    public float GetReagentConcentration(Entity<SolutionComponent> dissolvedSolution,
        FixedPoint2 volume, ReagentId dissolvedReagent)
    {
        var dissolvedSol = dissolvedSolution.Comp.Solution;
        if (volume == 0
            || dissolvedSol.Volume == 0
            || !dissolvedSol.TryGetReagentQuantity(dissolvedReagent, out var dissolvedVol))
            return 0;
        return (float)dissolvedVol / volume.Float();
    }

    public FixedPoint2 ClampReagentAmountByConcentration(
        Entity<SolutionComponent> dissolvedSolution,
        FixedPoint2 volume,
        ReagentId dissolvedReagent,
        FixedPoint2 dissolvedReagentAmount,
        float maxConcentration = 1f)
    {
        var dissolvedSol = dissolvedSolution.Comp.Solution;
        if (volume == 0
            || dissolvedSol.Volume == 0
            || !dissolvedSol.TryGetReagentQuantity(dissolvedReagent, out var dissolvedVol))
            return 0;
        volume *= maxConcentration;
        dissolvedVol += dissolvedReagentAmount;
        var overflow = volume - dissolvedVol;
        if (overflow < 0)
            dissolvedReagentAmount += overflow;
        return dissolvedReagentAmount;
    }
}
