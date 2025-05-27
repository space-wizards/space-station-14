using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Database;
using Content.Shared.Examine;

namespace Content.Shared.Atmos.EntitySystems;

/// <summary>
/// Handles all shared interactions with the gas pressure relief valve.
/// </summary>
public abstract class SharedGasPressureReliefValveSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UserInterfaceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasPressureReliefValveComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<GasPressureReliefValveComponent, GasPressureReliefValveChangeThresholdMessage>(
            OnThresholdChangeMessage);
    }

    /// <summary>
    /// Presents predicted examine information to the person examining the valve.
    /// </summary>
    /// <param name="valveEntity"> <see cref="Entity{T}"/> of the valve</param>
    /// <param name="args">Event arguments for examination</param>
    private void OnExamined(Entity<GasPressureReliefValveComponent> valveEntity, ref ExaminedEvent args)
    {
        if (!Transform(valveEntity).Anchored || !args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(GasPressureReliefValveComponent)))
        {
            args.PushMarkup(Loc.GetString("gas-pressure-relief-valve-system-examined",
                ("statusColor", valveEntity.Comp.Enabled ? "green" : "red"),
                ("open", valveEntity.Comp.Enabled)));

            args.PushMarkup(Loc.GetString("gas-pressure-relief-valve-examined-threshold-pressure",
                ("threshold", $"{valveEntity.Comp.Threshold:0.#}")));

            args.PushMarkup(Loc.GetString("gas-pressure-relief-valve-examined-flow-rate",
                ("flowRate", $"{valveEntity.Comp.FlowRate:0.#}")));
        }
    }

    /// <summary>
    /// Validates, logs, and updates the pressure threshold of the valve.
    /// </summary>
    /// <param name="valveEntity">The <see cref="Entity{T}"/> of the valve.</param>
    /// <param name="args">The received pressure from the <see cref="GasPressurePumpChangeOutputPressureMessage"/>message.</param>
    private void OnThresholdChangeMessage(Entity<GasPressureReliefValveComponent> valveEntity,
        ref GasPressureReliefValveChangeThresholdMessage args)
    {
        valveEntity.Comp.Threshold = Math.Max(0f, args.ThresholdPressure);
        _adminLogger.Add(LogType.AtmosVolumeChanged,
            LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the pressure threshold on {ToPrettyString(valveEntity):device} to {valveEntity.Comp.Threshold}");
        // Dirty the entire entity to ensure we get all of that Fresh:tm: UI info from the server.
        Dirty(valveEntity);
        UpdateUi(valveEntity);
    }

    protected virtual void UpdateUi(Entity<GasPressureReliefValveComponent> ent)
    {
    }
}
