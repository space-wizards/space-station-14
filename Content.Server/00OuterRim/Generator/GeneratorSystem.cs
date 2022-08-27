using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Server.Stack;
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
        SubscribeLocalEvent<SharedSolidFuelGeneratorComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SharedSolidFuelGeneratorComponent, SetTargetPowerMessage>(OnTargetPowerSet);
    }

    private void OnTargetPowerSet(EntityUid uid, SharedSolidFuelGeneratorComponent component, SetTargetPowerMessage args)
    {
        component.TargetPower = args.TargetPower * 100;
    }

    private void OnInteractUsing(EntityUid uid, SharedSolidFuelGeneratorComponent component, InteractUsingEvent args)
    {
        if (!TryComp(args.Used, out MaterialComponent? mat) || !TryComp(args.Used, out StackComponent? stack))
            return;

        if (!mat.MaterialIds.Contains(component.FuelMaterial))
            return;

        component.RemainingFuel += stack.Count;
        QueueDel(args.Used);
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
