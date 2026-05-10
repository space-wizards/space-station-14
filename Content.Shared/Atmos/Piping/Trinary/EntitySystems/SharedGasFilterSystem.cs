using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Database;

namespace Content.Shared.Atmos.Piping.Trinary.EntitySystems;

public abstract partial class SharedGasFilterSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasFilterComponent, GasFilterToggleStatusMessage>(OnToggleStatusMessage);
        SubscribeLocalEvent<GasFilterComponent, GasFilterChangeRateMessage>(OnTransferRateChangeMessage);
        SubscribeLocalEvent<GasFilterComponent, GasFilterSelectGasMessage>(OnSelectGasMessage);
    }

    protected virtual void UpdateUi(Entity<GasFilterComponent> ent)
    {
    }

    protected void UpdateAppearance(Entity<GasFilterComponent> ent)
    {
        _appearance.SetData(ent, FilterVisuals.Enabled, ent.Comp.Enabled);
    }

    private void OnToggleStatusMessage(Entity<GasFilterComponent> ent, ref GasFilterToggleStatusMessage args)
    {
        ent.Comp.Enabled = args.Enabled;
        _adminLogger.Add(LogType.AtmosPowerChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the power on {ToPrettyString(ent.Owner):device} to {args.Enabled}");

        Dirty(ent);
        UpdateUi(ent);
        UpdateAppearance(ent);
    }

    private void OnTransferRateChangeMessage(Entity<GasFilterComponent> ent, ref GasFilterChangeRateMessage args)
    {
        ent.Comp.TransferRate = Math.Clamp(args.Rate, 0f, ent.Comp.MaxTransferRate);
        _adminLogger.Add(LogType.AtmosVolumeChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the transfer rate on {ToPrettyString(ent.Owner):device} to {args.Rate}");

        Dirty(ent);
        UpdateUi(ent);
    }

    private void OnSelectGasMessage(Entity<GasFilterComponent> ent, ref GasFilterSelectGasMessage args)
    {
        if (args.Gas.HasValue)
        {
            if (!Enum.IsDefined(typeof(Gas), args.Gas))
            {
                Log.Warning($"{ToPrettyString(ent.Owner)} received GasFilterSelectGasMessage with an invalid ID: {args.Gas}");
                return;
            }

            ent.Comp.FilteredGas = args.Gas;
            _adminLogger.Add(LogType.AtmosFilterChanged, LogImpact.Medium,
                $"{ToPrettyString(args.Actor):player} set the filter on {ToPrettyString(ent.Owner):device} to {args.Gas.ToString()}");
        }
        else
        {
            ent.Comp.FilteredGas = null;
            _adminLogger.Add(LogType.AtmosFilterChanged, LogImpact.Medium,
                $"{ToPrettyString(args.Actor):player} set the filter on {ToPrettyString(ent.Owner):device} to none");
        }

        Dirty(ent);
        UpdateUi(ent);
    }
}
