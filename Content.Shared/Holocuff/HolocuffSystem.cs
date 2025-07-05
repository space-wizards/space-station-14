using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Storage;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Popups;

namespace Content.Shared.Holocuff;

public sealed class HolocuffSystem : EntitySystem
{
    // TODO: ADD POWERCELL USE
    // [Dependency] private readonly SharedPowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedCuffableSystem _cuff = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;


    public override void Initialize()
    {
        base.Initialize();
        // TODO: EXAMINE
        // SubscribeLocalEvent<HolocuffProjectorComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<HolocuffProjectorComponent, AfterInteractEvent>(OnCuffAfterInteract);
        SubscribeLocalEvent<HolocuffProjectorComponent, MeleeHitEvent>(OnCuffMeleeHit);
    }

    /*
    private void OnExamine(EntityUid uid, HolocuffProjectorComponent component, ExaminedEvent args)
    {
        // TODO: This should probably be using an itemstatus
        // TODO: I'm too lazy to do this rn but it's literally copy-paste from emag.
        _powerCell.TryGetBatteryFromSlot(uid, out var battery);
        var charges = UsesRemaining(component, battery);
        var maxCharges = MaxUses(component, battery);

        using (args.PushGroup(nameof(HolocuffProjectorComponent)))
        {
            args.PushMarkup(Loc.GetString("limited-charges-charges-remaining", ("charges", charges)));

            if (charges > 0 && charges == maxCharges)
            {
                args.PushMarkup(Loc.GetString("limited-charges-max-charges"));
            }
        }
    }

    private int UsesRemaining(HolocuffProjectorComponent component, BatteryComponent? battery = null)
    {
        if (battery == null ||
            component.ChargeUse == 0f) return 0;

        return (int)(battery.CurrentCharge / component.ChargeUse);
    }

    private int MaxUses(HolocuffProjectorComponent component, BatteryComponent? battery = null)
    {
        if (battery == null ||
            component.ChargeUse == 0f) return 0;

        return (int)(battery.MaxCharge / component.ChargeUse);
    }
    */

    private void OnCuffAfterInteract(EntityUid uid, HolocuffProjectorComponent component, AfterInteractEvent args)
    {
        if (args.Target is not { Valid: true } target)
            return;

        if (!args.CanReach)
        {
            _popup.PopupClient(Loc.GetString("handcuff-component-too-far-away-error"), args.User, args.User);
            return;
        }

        // TODO: ADD POWERCELL USE
        /*
        if (!_powerCell.TryUseCharge(uid, component.ChargeUse, user: args.User)) // if no battery or no charge, doesn't work)
            return;
        */

        var handcuff = Spawn(component.CuffProto);
        if (!TryComp<HandcuffComponent>(handcuff, out var handcuffComp))
            return;

        _cuff.SetHandcuffTool(handcuffComp, uid);

        var result = _cuff.TryCuffing(args.User, target, handcuff, handcuffComp);
        args.Handled = result;
    }

    private void OnCuffMeleeHit(EntityUid uid, HolocuffProjectorComponent component, MeleeHitEvent args)
    {
        if (!args.HitEntities.Any())
            return;

        // TODO: ADD POWERCELL USE
        /*
        if (!_powerCell.TryUseCharge(uid, component.ChargeUse, user: args.User)) // if no battery or no charge, doesn't work)
            return;
        */

        var handcuff = Spawn(component.CuffProto);
        if (!TryComp<HandcuffComponent>(handcuff, out var handcuffComp))
            return;

        _cuff.SetHandcuffTool(handcuffComp, uid);

        _cuff.TryCuffing(args.User, args.HitEntities.First(), handcuff, handcuffComp);
        args.Handled = true;
    }
}
