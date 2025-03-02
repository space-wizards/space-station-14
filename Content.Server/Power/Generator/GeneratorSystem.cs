using System.Linq;
using Content.Server.Audio;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Materials;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Power.Generator;
using Robust.Server.GameObjects;

namespace Content.Server.Power.Generator;

/// <inheritdoc/>
/// <seealso cref="FuelGeneratorComponent"/>
/// <seealso cref="ChemicalFuelGeneratorAdapterComponent"/>
/// <seealso cref="SolidFuelGeneratorAdapterComponent"/>
public sealed class GeneratorSystem : SharedGeneratorSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    public override void Initialize()
    {

        UpdatesBefore.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<FuelGeneratorComponent, PortableGeneratorSetTargetPowerMessage>(OnTargetPowerSet);
        SubscribeLocalEvent<FuelGeneratorComponent, PortableGeneratorEjectFuelMessage>(OnEjectFuel);
        SubscribeLocalEvent<FuelGeneratorComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<SolidFuelGeneratorAdapterComponent, GeneratorGetFuelEvent>(SolidGetFuel);
        SubscribeLocalEvent<SolidFuelGeneratorAdapterComponent, GeneratorUseFuel>(SolidUseFuel);
        SubscribeLocalEvent<SolidFuelGeneratorAdapterComponent, GeneratorEmpty>(SolidEmpty);
        SubscribeLocalEvent<ChemicalFuelGeneratorAdapterComponent, GeneratorGetFuelEvent>(ChemicalGetFuel);
        SubscribeLocalEvent<ChemicalFuelGeneratorAdapterComponent, GeneratorUseFuel>(ChemicalUseFuel);
        SubscribeLocalEvent<ChemicalFuelGeneratorAdapterComponent, GeneratorGetCloggedEvent>(ChemicalGetClogged);
        SubscribeLocalEvent<ChemicalFuelGeneratorAdapterComponent, GeneratorEmpty>(ChemicalEmpty);
    }

    private void OnAnchorStateChanged(EntityUid uid, FuelGeneratorComponent component, ref AnchorStateChangedEvent args)
    {
        // Turn off generator if unanchored while running.

        if (!component.On)
            return;

        SetFuelGeneratorOn(uid, false, component);
    }

    private void OnEjectFuel(EntityUid uid, FuelGeneratorComponent component, PortableGeneratorEjectFuelMessage args)
    {
        EmptyGenerator(uid);
    }

    private void SolidEmpty(EntityUid uid, SolidFuelGeneratorAdapterComponent component, GeneratorEmpty args)
    {
        _materialStorage.EjectAllMaterial(uid);
    }

    private void ChemicalEmpty(Entity<ChemicalFuelGeneratorAdapterComponent> entity, ref GeneratorEmpty args)
    {
        if (!_solutionContainer.ResolveSolution(entity.Owner, entity.Comp.SolutionName, ref entity.Comp.Solution, out var solution))
            return;

        var spillSolution = _solutionContainer.SplitSolution(entity.Comp.Solution.Value, solution.Volume);
        _puddle.TrySpillAt(entity.Owner, spillSolution, out _);
    }

    private void ChemicalGetClogged(Entity<ChemicalFuelGeneratorAdapterComponent> entity, ref GeneratorGetCloggedEvent args)
    {
        if (!_solutionContainer.ResolveSolution(entity.Owner, entity.Comp.SolutionName, ref entity.Comp.Solution, out var solution))
            return;

        foreach (var reagentQuantity in solution)
        {
            if (!entity.Comp.Reagents.ContainsKey(reagentQuantity.Reagent.Prototype))
            {
                args.Clogged = true;
                return;
            }
        }
    }

    private void ChemicalUseFuel(Entity<ChemicalFuelGeneratorAdapterComponent> entity, ref GeneratorUseFuel args)
    {
        if (!_solutionContainer.ResolveSolution(entity.Owner, entity.Comp.SolutionName, ref entity.Comp.Solution, out var solution))
            return;

        var totalReagent = 0f;
        foreach (var (reagentId, _) in entity.Comp.Reagents)
        {
            totalReagent += solution.GetTotalPrototypeQuantity(reagentId).Float();
            totalReagent += entity.Comp.FractionalReagents.GetValueOrDefault(reagentId);
        }

        if (totalReagent == 0)
            return;

        foreach (var (reagentId, multiplier) in entity.Comp.Reagents)
        {
            var fractionalReagent = entity.Comp.FractionalReagents.GetValueOrDefault(reagentId);
            var availableReagent = solution.GetTotalPrototypeQuantity(reagentId);
            var availForRatio = fractionalReagent + availableReagent.Float();
            var removalPercentage = availForRatio / totalReagent;

            var toRemove = RemoveFractionalFuel(
                ref fractionalReagent,
                args.FuelUsed * removalPercentage,
                multiplier * FixedPoint2.Epsilon.Float(),
                availableReagent.Value);

            entity.Comp.FractionalReagents[reagentId] = fractionalReagent;
            _solutionContainer.RemoveReagent(entity.Comp.Solution.Value, reagentId, FixedPoint2.FromCents(toRemove));
        }
    }

    private void ChemicalGetFuel(Entity<ChemicalFuelGeneratorAdapterComponent> entity, ref GeneratorGetFuelEvent args)
    {
        if (!_solutionContainer.ResolveSolution(entity.Owner, entity.Comp.SolutionName, ref entity.Comp.Solution, out var solution))
            return;

        var fuel = 0f;
        foreach (var (reagentId, multiplier) in entity.Comp.Reagents)
        {
            var reagent = solution.GetTotalPrototypeQuantity(reagentId).Float();
            reagent += entity.Comp.FractionalReagents.GetValueOrDefault(reagentId) * FixedPoint2.Epsilon.Float();

            fuel += reagent * multiplier;
        }

        args.Fuel = fuel;
    }

    private void SolidUseFuel(EntityUid uid, SolidFuelGeneratorAdapterComponent component, GeneratorUseFuel args)
    {
        var availableMaterial = _materialStorage.GetMaterialAmount(uid, component.FuelMaterial);
        var toRemove = RemoveFractionalFuel(
            ref component.FractionalMaterial,
            args.FuelUsed,
            component.Multiplier,
            availableMaterial);

        _materialStorage.TryChangeMaterialAmount(uid, component.FuelMaterial, -toRemove);
    }

    private int RemoveFractionalFuel(ref float fractional, float fuelUsed, float multiplier, int availableQuantity)
    {
        // Just a sanity thing since I got worried this might be possible.
        if (!float.IsFinite(fractional))
            fractional = 0;

        fractional -= fuelUsed / multiplier;
        if (fractional >= 0)
            return 0;

        // worst (unrealistic) case: -5.5 -> -6.0 -> 6
        var toRemove = -(int) MathF.Floor(fractional);
        toRemove = Math.Min(availableQuantity, toRemove);

        fractional = Math.Max(0, fractional + toRemove);
        return toRemove;
    }

    private void SolidGetFuel(
        EntityUid uid,
        SolidFuelGeneratorAdapterComponent component,
        ref GeneratorGetFuelEvent args)
    {
        var material = component.FractionalMaterial + _materialStorage.GetMaterialAmount(uid, component.FuelMaterial);
        args.Fuel = material * component.Multiplier;
    }

    private void OnTargetPowerSet(EntityUid uid, FuelGeneratorComponent component,
        PortableGeneratorSetTargetPowerMessage args)
    {
        component.TargetPower = Math.Clamp(
            args.TargetPower,
            component.MinTargetPower / 1000,
            component.MaxTargetPower / 1000) * 1000;
    }

    public void SetFuelGeneratorOn(EntityUid uid, bool on, FuelGeneratorComponent? generator = null)
    {
        if (!Resolve(uid, ref generator))
            return;

        if (on && !Transform(uid).Anchored)
        {
            // Generator must be anchored to start.
            return;
        }

        generator.On = on;
        UpdateState(uid, generator);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<FuelGeneratorComponent, PowerSupplierComponent>();

        while (query.MoveNext(out var uid, out var gen, out var supplier))
        {
            if (!gen.On)
                continue;

            var fuel = GetFuel(uid);
            if (fuel <= 0)
            {
                SetFuelGeneratorOn(uid, false, gen);
                continue;
            }

            if (GetIsClogged(uid))
            {
                _popup.PopupEntity(Loc.GetString("generator-clogged", ("generator", uid)), uid, PopupType.SmallCaution);
                SetFuelGeneratorOn(uid, false, gen);
                continue;
            }

            supplier.Enabled = true;

            supplier.MaxSupply = gen.TargetPower;

            var eff = 1 / CalcFuelEfficiency(gen.TargetPower, gen.OptimalPower, gen);
            var consumption = gen.OptimalBurnRate * frameTime * eff;
            RaiseLocalEvent(uid, new GeneratorUseFuel(consumption));
        }
    }

    public float GetFuel(EntityUid generator)
    {
        GeneratorGetFuelEvent getFuelEvent = default;
        RaiseLocalEvent(generator, ref getFuelEvent);
        return getFuelEvent.Fuel;
    }

    public bool GetIsClogged(EntityUid generator)
    {
        GeneratorGetCloggedEvent getCloggedEvent = default;
        RaiseLocalEvent(generator, ref getCloggedEvent);
        return getCloggedEvent.Clogged;
    }

    public void EmptyGenerator(EntityUid generator)
    {
        RaiseLocalEvent(generator, GeneratorEmpty.Instance);
    }

    private void UpdateState(EntityUid generator, FuelGeneratorComponent component)
    {
        _appearance.SetData(generator, GeneratorVisuals.Running, component.On);
        _ambientSound.SetAmbience(generator, component.On);
        if (!component.On)
            Comp<PowerSupplierComponent>(generator).Enabled = false;
    }
}

/// <summary>
/// Raised by <see cref="GeneratorSystem"/> to calculate the amount of remaining fuel in the generator.
/// </summary>
[ByRefEvent]
public record struct GeneratorGetFuelEvent(float Fuel);

/// <summary>
/// Raised by <see cref="GeneratorSystem"/> to check if a generator is "clogged".
/// For example there's bad chemicals in the fuel tank that prevent starting it.
/// </summary>
[ByRefEvent]
public record struct GeneratorGetCloggedEvent(bool Clogged);

/// <summary>
/// Raised by <see cref="GeneratorSystem"/> to draw fuel from its adapters.
/// </summary>
/// <remarks>
/// Implementations are expected to round fuel consumption up if the used fuel value is too small (e.g. reagent units).
/// </remarks>
public record struct GeneratorUseFuel(float FuelUsed);

/// <summary>
/// Raised by <see cref="GeneratorSystem"/> to empty a generator of its fuel contents.
/// </summary>
public sealed class GeneratorEmpty
{
    public static readonly GeneratorEmpty Instance = new();
}
