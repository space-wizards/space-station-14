using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos.Visuals;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.Atmos.Piping.Binary.Systems;

public abstract class SharedGasVolumePumpSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _receiver = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasVolumePumpComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GasVolumePumpComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<GasVolumePumpComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<GasVolumePumpComponent, GasVolumePumpToggleStatusMessage>(OnToggleStatusMessage);
        SubscribeLocalEvent<GasVolumePumpComponent, GasVolumePumpChangeTransferRateMessage>(OnTransferRateChangeMessage);
    }

    private void OnInit(Entity<GasVolumePumpComponent> ent, ref ComponentInit args)
    {
        UpdateAppearance(ent.Owner, ent.Comp);
    }

    private void OnPowerChanged(Entity<GasVolumePumpComponent> ent, ref PowerChangedEvent args)
    {
        UpdateAppearance(ent.Owner, ent.Comp);
    }

    protected virtual void UpdateUi(Entity<GasVolumePumpComponent> entity)
    {

    }

    private void OnToggleStatusMessage(EntityUid uid, GasVolumePumpComponent pump, GasVolumePumpToggleStatusMessage args)
    {
        pump.Enabled = args.Enabled;
        _adminLogger.Add(LogType.AtmosPowerChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the power on {ToPrettyString(uid):device} to {args.Enabled}");

        Dirty(uid, pump);
        UpdateUi((uid, pump));
        UpdateAppearance(uid, pump);
    }

    private void OnTransferRateChangeMessage(EntityUid uid, GasVolumePumpComponent pump, GasVolumePumpChangeTransferRateMessage args)
    {
        pump.TransferRate = Math.Clamp(args.TransferRate, 0f, pump.MaxTransferRate);
        Dirty(uid, pump);
        UpdateUi((uid, pump));
        _adminLogger.Add(LogType.AtmosVolumeChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the transfer rate on {ToPrettyString(uid):device} to {args.TransferRate}");
    }

    private void OnExamined(EntityUid uid, GasVolumePumpComponent pump, ExaminedEvent args)
    {
        if (!Transform(uid).Anchored)
            return;

        if (Loc.TryGetString("gas-volume-pump-system-examined",
                out var str,
                ("statusColor", "lightblue"), // TODO: change with volume?
                ("rate", pump.TransferRate)
            ))
        {
            args.PushMarkup(str);
        }
    }

    protected void UpdateAppearance(EntityUid uid, GasVolumePumpComponent? pump = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref pump, ref appearance, false))
            return;

        bool pumpOn = pump.Enabled && _receiver.IsPowered(uid);
        if (!pumpOn)
            _appearance.SetData(uid, GasVolumePumpVisuals.State, GasVolumePumpState.Off, appearance);
        else if (pump.Blocked)
            _appearance.SetData(uid, GasVolumePumpVisuals.State, GasVolumePumpState.Blocked, appearance);
        else
            _appearance.SetData(uid, GasVolumePumpVisuals.State, GasVolumePumpState.On, appearance);
    }
}
