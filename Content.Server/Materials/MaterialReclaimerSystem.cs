using System.Linq;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Construction;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Power.Components;
using Content.Server.Stack;
using Content.Server.Wires;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Robust.Shared.Utility;

namespace Content.Server.Materials;

public sealed class MaterialReclaimerSystem : SharedMaterialReclaimerSystem
{
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SpillableSystem _spillable = default!;
    [Dependency] private readonly StackSystem _stack = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaterialReclaimerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MaterialReclaimerComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<MaterialReclaimerComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        SubscribeLocalEvent<MaterialReclaimerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<ActiveMaterialReclaimerComponent, PowerChangedEvent>(OnActivePowerChanged);
        SubscribeLocalEvent<MaterialReclaimerComponent, InteractUsingEvent>(OnInteractUsing,
            before: new []{typeof(WiresSystem), typeof(SolutionTransferSystem)});
    }

    private void OnStartup(EntityUid uid, MaterialReclaimerComponent component, ComponentStartup args)
    {
        component.OutputSolution = _solutionContainer.EnsureSolution(uid, component.SolutionContainerId);
    }

    private void OnUpgradeExamine(EntityUid uid, MaterialReclaimerComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade(Loc.GetString("material-reclaimer-upgrade-process-rate"), component.MaterialProcessRate / component.BaseMaterialProcessRate);
    }

    private void OnRefreshParts(EntityUid uid, MaterialReclaimerComponent component, RefreshPartsEvent args)
    {
        var rating = args.PartRatings[component.MachinePartProcessRate] - 1;
        component.MaterialProcessRate = component.BaseMaterialProcessRate * MathF.Pow(component.PartRatingProcessRateMultiplier, rating);
        Dirty(component);
    }

    private void OnPowerChanged(EntityUid uid, MaterialReclaimerComponent component, ref PowerChangedEvent args)
    {
        component.Powered = args.Powered;
        Dirty(component);
    }

    private void OnInteractUsing(EntityUid uid, MaterialReclaimerComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // if we're trying to get a solution out of the reclaimer, don't destroy it
        if (component.OutputSolution.Contents.Any())
        {
            if (TryComp<SolutionContainerManagerComponent>(args.Used, out var managerComponent) &&
                managerComponent.Solutions.Any(s => s.Value.AvailableVolume > 0))
            {
                if (TryComp<DrinkComponent>(args.Used, out var drink) &&
                    !drink.Opened)
                    return;

                if (TryComp<SolutionTransferComponent>(args.Used, out var transfer) &&
                    transfer.CanReceive)
                    return;
            }
        }

        args.Handled = TryStartProcessItem(uid, args.Used, component, args.User);
    }

    private void OnActivePowerChanged(EntityUid uid, ActiveMaterialReclaimerComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            TryFinishProcessItem(uid, null, component);
    }

    public override bool TryFinishProcessItem(EntityUid uid, MaterialReclaimerComponent? component = null, ActiveMaterialReclaimerComponent? active = null)
    {
        if (!Resolve(uid, ref component, ref active, false))
            return false;

        if (!base.TryFinishProcessItem(uid, component, active))
            return false;

        if (active.ReclaimingContainer.ContainedEntities.FirstOrNull() is not { } item)
            return false;

        QueueDel(item);

        if (!TryComp<PhysicalCompositionComponent>(item, out var compositionComponent))
            return false;

        // scales the output if the process was interrupted.
        var completion = 1f - Math.Clamp((float) Math.Round((active.EndTime - Timing.CurTime) / active.Duration), 0f, 1f);
        var xform = Transform(uid);

        if (TryComp<MaterialStorageComponent>(uid, out var materialStorage))
        {
            foreach (var (material, amount) in compositionComponent.MaterialComposition)
            {
                var outputAmount = (int) (amount * completion * component.Efficiency);
                _materialStorage.TryChangeMaterialAmount(uid, material, outputAmount, materialStorage);

                foreach (var (storedMaterial, storedAmount) in materialStorage.Storage)
                {
                    var stacks = _materialStorage.SpawnMultipleFromMaterial(storedAmount, material, xform.Coordinates,
                        out var materialOverflow);
                    var amountConsumed = storedAmount - materialOverflow;
                    _materialStorage.TryChangeMaterialAmount(uid, storedMaterial, -amountConsumed, materialStorage);
                    foreach (var stack in stacks)
                    {
                        if (Exists(stack)) // make sure we don't merge it out of existence
                            _stack.TryMergeToContacts(stack);
                    }
                }
            }
        }

        var overflow = new Solution
        {
            MaxVolume = compositionComponent.ChemicalComposition.Values.Sum()
        };
        var totalChemicals = compositionComponent.ChemicalComposition;

        // if the item we inserted has reagents, add it in.
        if (TryComp<SolutionContainerManagerComponent>(item, out var solutionContainer))
        {
            foreach (var solution in solutionContainer.Solutions.Values)
            {
                foreach (var quantity in solution.Contents)
                {
                    totalChemicals[quantity.ReagentId] =
                        totalChemicals.GetValueOrDefault(quantity.ReagentId) + quantity.Quantity;
                }
            }
        }

        foreach (var (reagent, amount) in totalChemicals)
        {
            var outputAmount = amount * completion * component.Efficiency;
            _solutionContainer.TryAddReagent(uid, component.OutputSolution, reagent, outputAmount, out var accepted);
            var overflowAmount = outputAmount - accepted;
            if (overflowAmount > 0)
            {
                overflow.AddReagent(reagent, overflowAmount);
            }
        }

        if (overflow.Volume > 0)
        {
            _spillable.SpillAt(uid, overflow, component.PuddleId, transformComponent: xform);
        }
        return true;
    }
}
