using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Shared.Atmos.EntitySystems;

/// <summary>
/// Handles all shared interactions with the gas pressure relief valve.
/// Things like inspection, UI, and other shared logic.
/// </summary>
public abstract class SharedGasPressureReliefValveSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UserInterfaceSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasPressureReliefValveComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GasPressureReliefValveComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<GasPressureReliefValveComponent, GasPressureReliefValveChangeThresholdMessage>(OnThresholdChangeMessage);
        SubscribeLocalEvent<GasPressureReliefValveComponent, ActivateInWorldEvent>(OnValveActivate);
    }

    private void OnInit(Entity<GasPressureReliefValveComponent> valveEntity, ref ComponentInit args)
    {
        UpdateAppearance(valveEntity);
    }

    /// <summary>
    /// Presents predicted examine information to the person examining the valve.
    /// </summary>
    /// <param name="valveEntity">Entity reference containing the valve component</param>
    /// <param name="args">Event arguments for examination</param>
    private void OnExamined(Entity<GasPressureReliefValveComponent> valveEntity, ref ExaminedEvent args)
    {
        // No cool stuff provided if it's unable to be examined.
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

    private void UpdateAppearance(Entity<GasPressureReliefValveComponent, AppearanceComponent?> valveEntity)
    {
        if (!Resolve(valveEntity, ref valveEntity.Comp2, false))
            return;

        _appearance.SetData(valveEntity, FilterVisuals.Enabled, valveEntity.Comp1.Enabled, valveEntity.Comp2);
    }


    private void OnValveActivate(Entity<GasPressureReliefValveComponent> valveEntity, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        if (Transform(valveEntity).Anchored)
        {
            UserInterfaceSystem.OpenUi(valveEntity.Owner, GasPressureReliefValveUiKey.Key, actor.PlayerSession);
        }
        else
        {
            _popupSystem.PopupCursor(Loc.GetString("comp-gas-pump-ui-needs-anchor"), args.User);
        }

        args.Handled = true;
    }


    private void OnThresholdChangeMessage(Entity<GasPressureReliefValveComponent> valveEntity,
        ref GasPressureReliefValveChangeThresholdMessage args)
    {
        // TODO: Do we need to do this threshold validation twice? It's implemented on the UI. I guess for a hacked client...?
        valveEntity.Comp.Threshold = Math.Max(0f, args.ThresholdPressure);
        _adminLogger.Add(LogType.AtmosVolumeChanged,
            LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the pressure threshold on {ToPrettyString(valveEntity):device} to {args.ThresholdPressure}");
        // TODO: Do we really need to dirty the entire entity/comp here?
        Dirty(valveEntity);
        UpdateUi(valveEntity);
        // TODO: Also, dirty before we update the UI or after? Check other code.
    }


    protected virtual void UpdateUi(Entity<GasPressureReliefValveComponent> ent)
    {
    }
}
