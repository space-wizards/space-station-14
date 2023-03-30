using Content.Server.Chemistry.EntitySystems;
using Content.Server.Construction;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Materials;
using Robust.Shared.Utility;

namespace Content.Server.Materials;

public sealed class MaterialReclaimerSystem : SharedMaterialReclaimerSystem
{
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SpillableSystem _spillable = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaterialReclaimerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MaterialReclaimerComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<MaterialReclaimerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<ActiveMaterialReclaimerComponent, PowerChangedEvent>(OnActivePowerChanged);
    }

    private void OnStartup(EntityUid uid, MaterialReclaimerComponent component, ComponentStartup args)
    {
        component.OutputSolution = _solutionContainer.EnsureSolution(uid, component.SolutionContainerId);
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

        if (!TryComp<PhysicalCompositionComponent>(item, out var compositionComponent))
            return false;

        // scales the output if the process was interrupted.
        var completion = 1f - Math.Clamp((float) Math.Round((active.EndTime - Timing.CurTime) / active.Duration), 0f, 1f);

        var xform = Transform(uid);
        foreach (var (material, amount) in compositionComponent.MaterialComposition)
        {
            _materialStorage.SpawnMultipleFromMaterial((int) (amount * completion), material, xform.Coordinates);
        }

        var overflow = new Solution
        {
            MaxVolume = compositionComponent.ChemicalComposition.Values.Sum()
        };
        foreach (var (reagent, amount) in compositionComponent.ChemicalComposition)
        {
            _solutionContainer.TryAddReagent(uid, component.OutputSolution, reagent, amount, out var accepted);
            var overflowAmount = amount - accepted;
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
