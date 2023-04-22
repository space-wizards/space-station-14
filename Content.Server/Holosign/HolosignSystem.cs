using Content.Shared.Interaction.Events;
using Content.Server.Coordinates.Helpers;
using Content.Server.Power.Components;
using Content.Server.PowerCell;
using Content.Shared.Charges.Systems;

namespace Content.Server.Holosign;

public sealed class HolosignSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HolosignProjectorComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<HolosignProjectorComponent, ExaminedChargesEvent>(OnExamineCharges);
    }

    private void OnExamineCharges(EntityUid uid, HolosignProjectorComponent component, ref ExaminedChargesEvent args)
    {
        // TODO: This should probably be using an itemstatus
        _powerCell.TryGetBatteryFromSlot(uid, out var battery);
        _charges.SetCharges(uid, ChargesRemaining(component, battery));
        _charges.SetMaxCharges(uid, MaxCharges(component, battery));
    }

    private int ChargesRemaining(HolosignProjectorComponent component, BatteryComponent? battery = null)
    {
        if (battery == null ||
            component.ChargeUse == 0f) return 0;

        return (int) (battery.CurrentCharge / component.ChargeUse);
    }

    private int MaxCharges(HolosignProjectorComponent component, BatteryComponent? battery = null)
    {
        if (battery == null ||
            component.ChargeUse == 0f) return 0;

        return (int) (battery.MaxCharge / component.ChargeUse);
    }

    private void OnUse(EntityUid uid, HolosignProjectorComponent component, UseInHandEvent args)
    {
        if (args.Handled ||
            !_powerCell.TryGetBatteryFromSlot(uid, out var battery) ||
            !battery.TryUseCharge(component.ChargeUse))
            return;

        // TODO: Too tired to deal
        var holo = EntityManager.SpawnEntity(component.SignProto, Transform(args.User).Coordinates.SnapToGrid(EntityManager));
        Transform(holo).Anchored = true;

        args.Handled = true;
    }

}
