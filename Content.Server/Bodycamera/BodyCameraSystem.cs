using Content.Server.Access.Systems;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Server.SurveillanceCamera;
using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Bodycamera;
using Content.Shared.Timing;
using Robust.Shared.Containers;

namespace Content.Server.Bodycamera;

public sealed class BodyCameraSystem : SharedBodyCameraSystem
{
    [Dependency] private readonly SurveillanceCameraSystem _camera = default!;
    [Dependency] private readonly IdCardSystem _idCardSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyCameraComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        SubscribeLocalEvent<BodyCameraComponent, PowerCellChangedEvent>(OnPowerCellChanged);
    }

    /// <summary>
    /// Sync the SurveillanceCameraComponent state (default enabled) to the BodyCamera state (default disabled)
    /// </summary>
    protected override void OnComponentStartup(EntityUid uid, BodyCameraComponent component, ComponentStartup args)
    {
        base.OnComponentStartup(uid, component, args);

        if (HasComp<SurveillanceCameraComponent>(uid))
            _camera.SetActive(uid, false);
    }

    /// <summary>
    /// Set the camera name to the equipee name and job role
    /// </summary>
    protected override void OnEquipped(EntityUid uid, BodyCameraComponent comp, GotEquippedEvent args)
    {
        base.OnEquipped(uid, comp, args);

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

        _camera.SetName(uid, $"{userJob} - {userName}");
    }

    /// <summary>
    /// Disable camera once battery is dead
    /// </summary>
    protected override void OnPowerCellSlotEmpty(EntityUid uid, BodyCameraComponent comp, ref PowerCellSlotEmptyEvent args)
    {
        if (!TryDisable(uid, comp))
            return;

        //Since these trigger on the server side only, play sounds here
        _audio.PlayPvs(comp.PowerOffSound, uid);
        if (_containerSystem.TryGetContainingContainer(uid, out var container))
        {
            var message = Loc.GetString("bodycamera-component-on-use", ("state", Loc.GetString("bodycamera-component-off-state")));
            _popup.PopupEntity(message, uid, container.Owner);
        }
    }

    protected override void OnPowerCellChanged(EntityUid uid, BodyCameraComponent comp, PowerCellChangedEvent args)
    {
        //If the battery is changed while equipped, try to re-enable
        //Prevents needing to unequip and re-equip the camera after the battery runs out
        if (!comp.Equipped)
            return;

        if (args.Ejected)
            return;

        if (!TryEnable(uid, comp))
            return;

        //Since these trigger on the server side only, play sounds here
        _audio.PlayPvs(comp.PowerOnSound, uid);
        if (_containerSystem.TryGetContainingContainer(uid, out var container))
        {
            var message = Loc.GetString("bodycamera-component-on-use", ("state", Loc.GetString("bodycamera-component-on-state")));
            _popup.PopupEntity(message, uid, container.Owner);
        }
    }

    /// <summary>
    /// Enable the camera and start drawing power
    /// </summary>
    protected override bool TryEnable(EntityUid uid, BodyCameraComponent comp)
    {
        if (!base.TryEnable(uid, comp))
            return false;

        _camera.SetActive(uid, true);
        _powerCell.SetPowerCellDrawEnabled(uid, true);
        comp.Enabled = true;
        return true;
    }

    /// <summary>
    /// Disable the camera and stop drawing power
    /// </summary>
    protected override bool TryDisable(EntityUid uid, BodyCameraComponent comp)
    {
        if (!base.TryDisable(uid, comp))
            return false;

        _camera.SetActive(uid, false);
        _powerCell.SetPowerCellDrawEnabled(uid, false);
        comp.Enabled = false;
        return true;
    }

}
