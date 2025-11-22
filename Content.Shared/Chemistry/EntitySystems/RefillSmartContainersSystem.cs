using System.Linq;
using Content.Shared.Access.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.EntitySystems;

public sealed class RefillSmartContainersSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmartSolutionContainerComponent, SolutionTransferredEvent>(OnSmartSolutionTransferred);
        SubscribeLocalEvent<RefillSmartContainersComponent, EntInsertedIntoContainerMessage>(OnStorageInsertion);
        SubscribeLocalEvent<RefillSmartContainersComponent, EntRemovedFromContainerMessage>(OnStorageRemoval);
        SubscribeLocalEvent<RefillSmartContainersComponent, GetVerbsEvent<AlternativeVerb>>(AddRefillVerb);
    }

    private void OnSmartSolutionTransferred(Entity<SmartSolutionContainerComponent> container, ref SolutionTransferredEvent args)
    {
        if (!TryComp<SolutionContainerManagerComponent>(container, out var solutions))
            return;

        var entity = new Entity<SolutionContainerManagerComponent>(container.Owner, solutions).AsNullable();
        var comp = container.Comp;

        if (_solution.TryGetSolution(entity, comp.SolutionName, out var solution))
        {
            comp.PreviousContents = new List<ReagentQuantity>(solution.Value.Comp.Solution.Contents);
            comp.PreviousContents.Sort((p, q) =>
                // Sort the list for later to make the code more efficient.
                string.Compare(p.Reagent.ToString(), q.Reagent.ToString(), StringComparison.Ordinal));
        }
        Dirty(container, comp);
    }

    private void OnStorageInsertion(Entity<RefillSmartContainersComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (HasComp<StackComponent>(args.Entity) // Stop basic materials like steel getting added on init.
            || !TryComp<SolutionContainerManagerComponent>(args.Entity, out var solutions))
            return;

        var entity = new Entity<SolutionContainerManagerComponent>(args.Entity, solutions).AsNullable();
        // I really shouldn't be using SolutionContainerManager.Containers.FirstOrDefault(), oh lord help me
        if (!_solution.TryGetSolution(entity,  solutions.Containers.FirstOrDefault(), out var solution))
            return;

        List<ReagentId> reagentIds = [];
        reagentIds.AddRange(solution.Value.Comp.Solution.Contents.Select(reagent => reagent.Reagent));
        reagentIds.Sort((p, q) =>
            // Sort the list for later to make the code more efficient.
            string.Compare(p.ToString(), q.ToString(), StringComparison.Ordinal));

        // If the reagent mix is already present, add the jug Uid to the value list.
        if (ent.Comp.SolutionContents.TryGetValue(reagentIds, out var value))
            value.Add(solution);
        else if (reagentIds.Count > 0)
        {
            var list = new List<Entity<SolutionComponent>?>();
            list.Add(solution);
            ent.Comp.SolutionContents.Add(reagentIds, list);
        }
    }

    private void OnStorageRemoval(Entity<RefillSmartContainersComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!TryComp<SolutionContainerManagerComponent>(args.Entity, out var container)
            || !_solution.TryGetSolution(args.Entity,  container.Containers.FirstOrDefault(), out var solution))
            return;

        foreach (var pair in ent.Comp.SolutionContents)
        {
            var isRemoved = false;
            foreach (var jug in pair.Value)
            {
                if (jug != null && jug.Value == solution)
                {
                    isRemoved = true;
                    pair.Value.Remove(jug.Value);
                    if (pair.Value.Count == 0)
                        ent.Comp.SolutionContents.Remove(pair.Key);
                    break;
                }
            }
            if (isRemoved)
                break;
        }
    }

    private void AddRefillVerb(EntityUid entity, RefillSmartContainersComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess
            || !args.CanInteract
            || !args.CanComplexInteract
            || !args.Using.HasValue // Check if they're holding something
            || !TryComp<SmartSolutionContainerComponent>(args.Using, out var smartComp) // Check if they're holding something that can be refilled.
            && !HasComp<StorageComponent>(args.Using)) // Check if they're holding something that can hold something that can be refilled. (Forgive me)
            return; // If not, do not show verb.

        var hasAccess = _accessReader.IsAllowed(args.User, entity);

        if (smartComp != null)
        {
            AlternativeVerb verb = new()
            {
                Disabled = !hasAccess,
                Message = Loc.GetString(hasAccess ? "refiller-refill-message" :  "refiller-insufficient-access-message"),
                Act = () => RefillSingle(component, (args.Using.Value, smartComp), args.User),
                Text = Loc.GetString("refiller-smart-container-verb"),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/spill.svg.192dpi.png")),
            };
            args.Verbs.Add(verb);
        }
        else if (TryComp<StorageComponent>(args.Using, out var storageComp))
        {
            AlternativeVerb verb = new()
            {
                Disabled = !hasAccess,
                Message = Loc.GetString(hasAccess ? "refiller-refill-message" :  "refiller-insufficient-access-message"),
                Act = () => RefillStorage(component, storageComp, args.User),
                Text = Loc.GetString("refiller-storage-container-verb"),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/spill.svg.192dpi.png")),
            };
            args.Verbs.Add(verb);
        }

    }

    private void RefillSingle(RefillSmartContainersComponent refillComp,
        Entity<SmartSolutionContainerComponent> smartEntity, EntityUid user)
    {
        _popup.PopupClient(
            Loc.GetString(TryRefill(refillComp, smartEntity) ? "refiller-successful-singular-refill" : "refiller-failed-refill"), user);
    }

    private void RefillStorage(RefillSmartContainersComponent refillComp, StorageComponent storageComp, EntityUid user)
    {
        bool refillFailed = false;
        bool refillSucceededOnce = false;
        foreach (var item in storageComp.StoredItems.Keys)
        {
            if (!TryComp<SmartSolutionContainerComponent>(item, out var smartComp))
                continue;
            if (TryRefill(refillComp, (item, smartComp)))
                refillSucceededOnce = true;
            else
                refillFailed = true;
        }
        // Succeeded at least once, but didn't refill all smart containers.
        if (refillFailed && refillSucceededOnce)
            _popup.PopupClient(Loc.GetString("refiller-partly-successful-storage-refill"), user);
        // All succeeded.
        else if (refillSucceededOnce)
            _popup.PopupClient(Loc.GetString("refiller-successful-storage-refill"), user);
        // None succeeded.
        else if (refillFailed)
            _popup.PopupClient(Loc.GetString("refiller-failed-refill"), user);
        // No smart containers in storage item.
        else
            _popup.PopupClient(Loc.GetString("refiller-no-smart-containers-in-storage"), user);
    }

    private bool TryRefill(RefillSmartContainersComponent refillComp,
        Entity<SmartSolutionContainerComponent> smartEntity)
    {
        var (smartUid, smartComp) = smartEntity;
        if (!Resolve(smartUid, ref smartComp.SolutionManager))
            return false;

        if (!_solution.ResolveSolution((smartUid, smartComp.SolutionManager),
                smartComp.SolutionName,
                ref smartComp.Solution))
            return false;

        var missingFraction = 1 - smartComp.Solution.Value.Comp.Solution.FillFraction;
        // No refill needed
        if (missingFraction == 0)
            return true;

        var maxAmount = smartComp.Solution.Value.Comp.Solution.MaxVolume;
        var missingAmount = smartComp.Solution.Value.Comp.Solution.AvailableVolume;

        var canBeMixed = false;
        var toBeMixed = new Dictionary<SolutionComponent, FixedPoint2>();
        var keyForMixedJugs = new List<ReagentId>();
        // Check if the RefillContainer has pure jugs of the chemical mixture and enough of each reagent to mix it.
        foreach (var previousReagent in smartComp.PreviousContents)
        {
            keyForMixedJugs.Add(previousReagent.Reagent);
            // May God have mercy upon my soul for this.
            List<ReagentId> reagentIds = [];
            reagentIds.Add(previousReagent.Reagent);
            // Check if a pure jug with the chemical is present.
            if (!refillComp.SolutionContents.TryGetValue(reagentIds, out var value))
            {
                canBeMixed = false;
                break;
            }

            var neededAmount = previousReagent.Quantity;
            // Check each pure jug for its amount.
            foreach (var jug in value)
            {
                if (jug == null || jug.Value.Comp.Solution.Volume <= 0)
                    continue;

                var jugVolume = jug.Value.Comp.Solution.Volume;
                // Check if the jug has enough to refill the container.
                if (jugVolume > neededAmount)
                {
                    toBeMixed.Add(jug.Value, neededAmount);
                    canBeMixed = true;
                }
                else
                {
                    toBeMixed.Add(jug.Value, jugVolume);
                    neededAmount -= jugVolume;
                }
            }
        }
        keyForMixedJugs.Sort((p, q) =>
            // Sort the list for later to make the code more efficient.
            string.Compare(p.ToString(), q.ToString(), StringComparison.Ordinal));

        if (refillComp.SolutionContents.TryGetValue(keyForMixedJugs, out var mixedJugs))
        {
            var availableAmount = new FixedPoint2();
            // Check each mixed jug for its amount.
            foreach (var mixedJug in mixedJugs)
            {
                if (mixedJug != null)
                    availableAmount += mixedJug.Value.Comp.Solution.Volume;
            }

            // Check if the total sum is enough to replace the missing amount.
            if (maxAmount / missingFraction <= availableAmount)
            {
                foreach (var mixedJug in mixedJugs)
                {
                    if (mixedJug == null)
                        continue;
                    var jugSolution = mixedJug.Value.Comp.Solution;
                    // If the jug has less than what's missing, inject the jug amount instead.
                    var transferAmount = jugSolution.Volume < missingAmount ? jugSolution.Volume : missingAmount;

                    _solution.TryTransferSolution(smartComp.Solution.Value, jugSolution, transferAmount);
                    missingAmount -= transferAmount;
                    // Check if the container is already full.
                    if (missingAmount == 0)
                        return true;
                }
            }
        }

        if (canBeMixed)
        {
            foreach (var pureJugs in toBeMixed)
            {
                _solution.TryTransferSolution(smartComp.Solution.Value, pureJugs.Key.Solution, pureJugs.Value);
            }
            return true;
        }
        return false;
    }
}
