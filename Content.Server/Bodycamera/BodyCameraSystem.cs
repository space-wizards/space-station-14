using Content.Server.Access.Systems;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Server.SurveillanceCamera;
using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Timing;

namespace Content.Server.Bodycamera;

public sealed class BodyCameraSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SurveillanceCameraSystem _camera = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IdCardSystem _idCardSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyCameraComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<BodyCameraComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        SubscribeLocalEvent<BodyCameraComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<BodyCameraComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<BodyCameraComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<BodyCameraComponent, GotUnequippedEvent>(OnUnequipped);
    }

    /// <summary>
    /// Sync the SurveillanceCameraComponent state (default enabled) to the BodyCamera state (default disabled)
    /// </summary>
    private void OnComponentStartup(EntityUid uid, BodyCameraComponent component, ComponentStartup args)
    {
        _camera.SetActive(uid, false);
    }

    /// <summary>
    /// Disable camera once battery is dead
    /// </summary>
    private void OnPowerCellSlotEmpty(EntityUid uid, BodyCameraComponent component, ref PowerCellSlotEmptyEvent args)
    {
        if (component.Enabled)
            TryDisable(uid, component);
    }

    private void OnPowerCellChanged(EntityUid uid, BodyCameraComponent comp, PowerCellChangedEvent args)
    {
        //If the battery is changed while equipped, try to re-enable
        //Prevents needing to unequip and re-equip the camera after the battery runs out
        if (comp.Equipped
            && !comp.Enabled
            && !args.Ejected)
            TryEnable(uid, comp);
    }

    /// <summary>
    /// Enable the camera and rename it
    /// Only when equipped to a slot specified in the Clothing component - not pockets
    /// </summary>
    private void OnEquipped(EntityUid uid, BodyCameraComponent comp, GotEquippedEvent args)
    {
        //Dont enable the camera unless placed into a slot allowed by the ClothingComponent
        //Primary purpose is to stop it being activated in pockets
        if (TryComp<ClothingComponent>(uid, out var clothingComp)
            && (clothingComp.Slots & args.SlotFlags) != args.SlotFlags)
        {
            return;
        }

        if (!TryEnable(uid, comp))
            return;

        //Construct the camera name using the players name and job (from ID card)
        //Use defaults if no ID card is found
        var userName = Loc.GetString("bodycamera-component-unknown-name");
        var userJob = Loc.GetString("bodycamera-component-unknown-job");

        if (_idCardSystem.TryFindIdCard(args.Equipee, out var card))
        {
            if (card.Comp.FullName != null)
                userName = card.Comp.FullName;
            if (card.Comp.JobTitle != null)
                userJob = card.Comp.JobTitle;
        }

        string cameraName = $"{userJob} - {userName}";
        _camera.SetName(uid, cameraName);

        var state = Loc.GetString("bodycamera-component-on-state");
        var message = Loc.GetString("bodycamera-component-on-use", ("state", state));
        _popup.PopupEntity(message, args.Equipee);

        comp.Equipped = true;
    }

    /// <summary>
    /// Disable the camera when unequipped
    /// </summary>
    private void OnUnequipped(EntityUid uid, BodyCameraComponent comp, GotUnequippedEvent args)
    {
        
        if (!TryDisable(uid, comp))
            return;

        var state = Loc.GetString("bodycamera-component-off-state");
        var message = Loc.GetString("bodycamera-component-on-use", ("state", state));
        _popup.PopupEntity(message, args.Equipee);

        comp.Equipped = false;
    }

    /// <summary>
    /// Indicate if the camera is powered via examination
    /// </summary>
    private void OnExamine(EntityUid uid, BodyCameraComponent comp, ExaminedEvent args)
    {
        var msg = comp.Enabled
            ? Loc.GetString("bodycamera-component-examine-on-state")
            : Loc.GetString("bodycamera-component-examine-off-state");
        args.PushMarkup(msg);
    }

    /// <summary>
    /// Enable the camera and play sound if there is enough charge
    /// </summary>
    private bool TryEnable(EntityUid uid, BodyCameraComponent comp)
    {
        if (comp.Enabled)
            return false;

        if (!_powerCell.HasDrawCharge(uid))
            return false;

        _camera.SetActive(uid, true);
        _powerCell.SetPowerCellDrawEnabled(uid, true);
        _audio.PlayPvs(comp.PowerOnSound, uid);
        comp.Enabled = true;
        return true;
    }

    /// <summary>
    /// Disable the camera and play sound
    /// </summary>
    private bool TryDisable(EntityUid uid, BodyCameraComponent comp)
    {
        if (!comp.Enabled)
            return false;

        _camera.SetActive(uid, false);
        _powerCell.SetPowerCellDrawEnabled(uid, false);
        _audio.PlayPvs(comp.PowerOffSound, uid);
        comp.Enabled = false;
        return true;
    }
}
