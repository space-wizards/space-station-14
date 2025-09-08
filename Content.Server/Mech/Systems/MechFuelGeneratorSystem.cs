using Content.Server.Mech.Components;
using Content.Server.Power.Generator;
using Content.Shared.Materials;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Power.Generator;
using Robust.Shared.GameObjects;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Bridges mech FuelGenerator-based modules to the mech battery by consuming fuel via the standard
/// generator events and adding the module's chargeRate into the per-tick recharge accumulator.
/// </summary>
public sealed partial class MechFuelGeneratorBridgeSystem : EntitySystem
{
    [Dependency] private readonly GeneratorSystem _generator = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MechGeneratorModuleComponent, MechEquipmentUiMessageRelayEvent>(OnMechGeneratorMessage);
        SubscribeLocalEvent<MechGeneratorModuleComponent, MechEquipmentUiStateReadyEvent>(OnUiStateReady);
    }

    private void OnMechGeneratorMessage(EntityUid uid, MechGeneratorModuleComponent component, MechEquipmentUiMessageRelayEvent args)
    {
        if (args.Message is not MechGeneratorEjectFuelMessage)
            return;

        if (!TryComp<FuelGeneratorComponent>(uid, out _))
            return;

        _generator.EmptyGenerator(uid);
    }

    private void OnUiStateReady(EntityUid uid, MechGeneratorModuleComponent gen, MechEquipmentUiStateReadyEvent args)
    {
        var ui = new MechGeneratorUiState();

        // Read live telemetry written by generator systems each tick
        if (TryComp<MechEnergyAccumulatorComponent>(uid, out var telem))
        {
            ui.ChargeCurrent = telem.Current;
            ui.ChargeMax = telem.Max;
        }

        if (gen.GenerationType == MechGenerationType.FuelGenerator)
        {
            if (TryComp<SolidFuelGeneratorAdapterComponent>(uid, out var solid))
            {
                var amount = _materialStorage.GetMaterialAmount(uid, solid.FuelMaterial);
                amount += (int) MathF.Floor(solid.FractionalMaterial);

                if (TryComp<MaterialStorageComponent>(uid, out var storage))
                {
                    ui.HasFuel = true;
                    ui.FuelCapacity = storage.StorageLimit ?? 0;
                }

                ui.FuelName = solid.FuelMaterial;
                ui.FuelAmount = amount;
            }
        }

        args.States[GetNetEntity(uid)] = ui;
    }

	public override void Update(float frameTime)
	{
		var query = EntityQueryEnumerator<MechComponent>();
		while (query.MoveNext(out var mechUid, out var mech))
		{
			if (!TryComp<MechEnergyAccumulatorComponent>(mechUid, out var acc))
				acc = EnsureComp<MechEnergyAccumulatorComponent>(mechUid);

			foreach (var module in mech.ModuleContainer.ContainedEntities)
			{
				if (!TryComp<MechGeneratorModuleComponent>(module, out var gen))
					continue;
				if (gen.GenerationType != MechGenerationType.FuelGenerator)
					continue;

				var telem = EnsureComp<MechEnergyAccumulatorComponent>(module);
				telem.Max = 0f;
				telem.Current = 0f;

				if (!TryComp<FuelGeneratorComponent>(module, out var fuelGen))
					continue;

				// max output is the configured target power
				telem.Max = fuelGen.TargetPower;

				var availableFuel = _generator.GetFuel(module);
				if (availableFuel <= 0 || _generator.GetIsClogged(module))
					continue;

				var eff = 1 / SharedGeneratorSystem.CalcFuelEfficiency(fuelGen.TargetPower, fuelGen.OptimalPower, fuelGen);
				var burn = fuelGen.OptimalBurnRate * frameTime * eff;
				RaiseLocalEvent(module, new GeneratorUseFuel(burn));

				// Current contribution equals target power when fuel is available
				var current = fuelGen.TargetPower;
				acc.PendingRechargeRate += current;
				telem.Current = current;
			}

			UpdateMechUI(mechUid);
		}
	}

    private void UpdateMechUI(EntityUid uid)
    {
        RaiseLocalEvent(uid, new UpdateMechUiEvent());
    }
}
