using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Server.Stack;
using Content.Shared.Chemistry.Components;
using Content.Shared.Interaction;
using Content.Shared.OuterRim.Generator;
using Robust.Server.GameObjects;

namespace Content.Server._00OuterRim.Generator;

/// <inheritdoc/>
public sealed class GeneratorSystem : SharedGeneratorSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SolidFuelGeneratorAdapterComponent, InteractUsingEvent>(OnSolidFuelAdapterInteractUsing);
        SubscribeLocalEvent<ChemicalFuelGeneratorAdapterComponent, InteractUsingEvent>(OnChemicalFuelAdapterInteractUsing);
        SubscribeLocalEvent<SharedSolidFuelGeneratorComponent, SetTargetPowerMessage>(OnTargetPowerSet);
    }

    private void OnChemicalFuelAdapterInteractUsing(EntityUid uid, ChemicalFuelGeneratorAdapterComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.Used, out SolutionContainerManagerComponent? solutions) || !TryComp(uid, out SharedSolidFuelGeneratorComponent? generator))
            return;

        if (!(component.Whitelist?.IsValid(args.Used) ?? true))
            return;

        if (TryComp(args.Used, out ChemicalFuelGeneratorDirectSourceComponent? source))
        {
            if (!solutions.Solutions.ContainsKey(source.Solution))
            {
                Logger.Error($"Couldn't get solution {source.Solution} on {ToPrettyString(args.Used)}");
                return;
            }

            var solution = solutions.Solutions[source.Solution];
            generator.RemainingFuel += ReagentsToFuel(component, solution);
            solution.RemoveAllSolution();
            QueueDel(args.Used);
        }
    }

    private float ReagentsToFuel(ChemicalFuelGeneratorAdapterComponent component, Solution solution)
    {
        var total = 0.0f;
        foreach (var reagent in solution.Contents)
        {
            if (!component.ChemConversionFactors.ContainsKey(reagent.ReagentId))
                continue;

            total += reagent.Quantity.Float() * component.ChemConversionFactors[reagent.ReagentId];
        }

        return total;
    }

    private void OnTargetPowerSet(EntityUid uid, SharedSolidFuelGeneratorComponent component, SetTargetPowerMessage args)
    {
        component.TargetPower = args.TargetPower * 100;
    }

    private void OnSolidFuelAdapterInteractUsing(EntityUid uid, SolidFuelGeneratorAdapterComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.Used, out MaterialComponent? mat) || !TryComp(args.Used, out StackComponent? stack) || !TryComp(uid, out SharedSolidFuelGeneratorComponent? generator))
            return;

        if (!mat.MaterialIds.Contains(component.FuelMaterial))
            return;

        generator.RemainingFuel += stack.Count;
        QueueDel(args.Used);
        args.Handled = true;
        return;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (gen, supplier, xform) in EntityQuery<SharedSolidFuelGeneratorComponent, PowerSupplierComponent, TransformComponent>())
        {
            supplier.Enabled = !(gen.RemainingFuel <= 0.0f || xform.Anchored == false);

            supplier.MaxSupply = gen.TargetPower;

            var eff = 1 / CalcFuelEfficiency(gen.TargetPower, gen.OptimalPower);

            gen.RemainingFuel = MathF.Max(gen.RemainingFuel - (gen.OptimalBurnRate * frameTime * eff), 0.0f);
            UpdateUi(gen);
        }
    }

    private void UpdateUi(SharedSolidFuelGeneratorComponent comp)
    {
        if (!_uiSystem.IsUiOpen(comp.Owner, GeneratorComponentUiKey.Key))
            return;

        _uiSystem.TrySetUiState(comp.Owner, GeneratorComponentUiKey.Key, new SolidFuelGeneratorComponentBuiState(comp));
    }
}
