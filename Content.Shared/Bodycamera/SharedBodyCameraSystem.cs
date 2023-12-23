using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Bodycamera;

public abstract class SharedBodyCameraSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedPowerCellSystem _powerCell = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyCameraComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<BodyCameraComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<BodyCameraComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<BodyCameraComponent, GotUnequippedEvent>(OnUnequipped);
    }

    protected virtual void OnComponentStartup(EntityUid uid, BodyCameraComponent component, ComponentStartup args)
    {
    }

    protected virtual void OnPowerCellSlotEmpty(EntityUid uid, BodyCameraComponent comp, ref PowerCellSlotEmptyEvent args)
    {
    }

    protected virtual void OnPowerCellChanged(EntityUid uid, BodyCameraComponent comp, PowerCellChangedEvent args)
    {
    }

    /// <summary>
    /// Do popup for the equipee
    /// Only when equipped to a slot specified in the Clothing component - not pockets
    /// </summary>
    protected virtual void OnEquipped(EntityUid uid, BodyCameraComponent comp, GotEquippedEvent args)
    {
        //Dont enable the camera unless placed into a slot allowed by the ClothingComponent
        //Primary purpose is to stop it being activated in pockets
        if (TryComp<ClothingComponent>(uid, out var clothingComp)
            && (clothingComp.Slots & args.SlotFlags) != args.SlotFlags)
        {
            return;
        }

        comp.Equipped = true;

        if (!TryEnable(uid, comp))
            return;

        var message = Loc.GetString("bodycamera-component-on-use", ("state", Loc.GetString("bodycamera-component-on-state")));
        _popup.PopupClient(message, uid, args.Equipee);
    }

    /// <summary>
    /// Show popup for the equipee
    /// </summary>
    protected virtual void OnUnequipped(EntityUid uid, BodyCameraComponent comp, GotUnequippedEvent args)
    {
        comp.Equipped = false;
        if (!TryDisable(uid, comp))
            return;

        var message = Loc.GetString("bodycamera-component-on-use", ("state", Loc.GetString("bodycamera-component-off-state")));
        _popup.PopupClient(message, uid, args.Equipee);
    }

    /// <summary>
    /// Indicate if the camera is powered via examination
    /// </summary>
    protected virtual void OnExamine(EntityUid uid, BodyCameraComponent comp, ExaminedEvent args)
    {
        var msg = comp.Enabled
            ? Loc.GetString("bodycamera-component-examine-on-state")
            : Loc.GetString("bodycamera-component-examine-off-state");
        args.PushMarkup(msg);
    }

    /// <summary>
    /// Enable the camera and play sound if there is enough charge
    /// </summary>
    protected virtual bool TryEnable(EntityUid uid, BodyCameraComponent comp)
    {
        if (comp.Enabled)
            return false;

        if (!_powerCell.HasDrawCharge(uid))
            return false;

        comp.Enabled = true;
        return true;
    }

    /// <summary>
    /// Disable the camera and play sound
    /// </summary>
    protected virtual bool TryDisable(EntityUid uid, BodyCameraComponent comp)
    {
        if (!comp.Enabled)
            return false;

        comp.Enabled = false;
        return true;
    }
}
