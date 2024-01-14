using Content.Shared.Interaction.Events;
using Content.Shared.Examine;
using Content.Shared.Coordinates.Helpers;
using Content.Server.Power.Components;
using Content.Server.PowerCell;

namespace Content.Server.Holosign;

public sealed class HolosignSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HolosignProjectorComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<HolosignProjectorComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, HolosignProjectorComponent component, ExaminedEvent args)
    {
        // TODO: This should probably be using an itemstatus
        // TODO: I'm too lazy to do this rn but it's literally copy-paste from emag.
        _powerCell.TryGetBatteryFromSlot(uid, out var battery);
        var charges = UsesRemaining(component, battery);
        var maxCharges = MaxUses(component, battery);

        using (args.PushGroup(nameof(HolosignProjectorComponent)))
        {
            args.PushMarkup(Loc.GetString("limited-charges-charges-remaining", ("charges", charges)));

            if (charges > 0 && charges == maxCharges)
            {
                args.PushMarkup(Loc.GetString("limited-charges-max-charges"));
            }
        }
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

    private int UsesRemaining(HolosignProjectorComponent component, BatteryComponent? battery = null)
    {
        if (battery == null ||
            component.ChargeUse == 0f) return 0;

        return (int) (battery.CurrentCharge / component.ChargeUse);
    }

    private int MaxUses(HolosignProjectorComponent component, BatteryComponent? battery = null)
    {
        if (battery == null ||
            component.ChargeUse == 0f) return 0;

        return (int) (battery.MaxCharge / component.ChargeUse);
    }
}
