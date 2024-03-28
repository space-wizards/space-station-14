using Content.Shared.Clothing.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Bodycamera;

public abstract class SharedBodyCameraSystem : EntitySystem
{
    [Dependency] protected readonly SharedPowerCellSystem PowerCell = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyCameraComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<BodyCameraComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<BodyCameraComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<BodyCameraComponent, GotUnequippedEvent>(OnUnequipped);
    }

    protected virtual void OnPowerCellChanged(Entity<BodyCameraComponent> bodyCamera, ref PowerCellChangedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Ejected)
        {
            TryDisable(bodyCamera);
        }
    }

    /// <summary>
    /// Do popup for the equipee
    /// Only when equipped to a slot specified in the Clothing component - not pockets
    /// </summary>
    protected virtual void OnEquipped(Entity<BodyCameraComponent> bodyCamera, ref GotEquippedEvent args)
    {
        //Dont enable the camera unless placed into a slot allowed by the ClothingComponent
        if (!TryComp<ClothingComponent>(bodyCamera, out var clothingComp))
            return;

        //Ensure the current slot is an allowed equipment slot
        //And not a pocket
        if (!args.SlotFlags.HasFlag(clothingComp.Slots))
            return;

        TryEnable(bodyCamera, args.Equipee);

        //Lock the power cell slot to prevent removal
        //Prevents a prediction issue
        SetPowerCellLock(bodyCamera, true);
    }

    /// <summary>
    /// Show popup for the equipee
    /// </summary>
    protected virtual void OnUnequipped(Entity<BodyCameraComponent> bodyCamera, ref GotUnequippedEvent args)
    {
        TryDisable(bodyCamera, args.Equipee);
        SetPowerCellLock(bodyCamera, false);
    }

    /// <summary>
    /// Indicate if the camera is powered via examination
    /// </summary>
    protected virtual void OnExamine(Entity<BodyCameraComponent> bodyCamera, ref ExaminedEvent args)
    {
        var msg = bodyCamera.Comp.Enabled
            ? Loc.GetString(bodyCamera.Comp.CameraExamineOn)
            : Loc.GetString(bodyCamera.Comp.CameraExamineOff);
        args.PushMarkup(msg);
    }

    /// <summary>
    /// Enable the camera and play sound if there is enough charge
    /// </summary>
    protected virtual bool TryEnable(Entity<BodyCameraComponent> bodyCamera, EntityUid? user = null)
    {
        if (bodyCamera.Comp.Enabled)
            return false;

        if (!PowerCell.HasDrawCharge(bodyCamera))
            return false;

        if (user == null && !TryGetWearer(bodyCamera, out user))
            return false;

        Audio.PlayPredicted(bodyCamera.Comp.PowerOnSound, bodyCamera, user);

        var message = Loc.GetString(bodyCamera.Comp.CameraOnUse);
        Popup.PopupClient(message, bodyCamera, user.Value);

        bodyCamera.Comp.Enabled = true;
        PowerCell.SetPowerCellDrawEnabled(bodyCamera, true);
        return true;
    }

    /// <summary>
    /// Disable the bodycamera
    /// </summary>
    /// <param name="bodyCamera">Which bodycamera to disable</param>
    /// <param name="user">Which user is currently wearing or interacting with the bodycamera</param>
    /// <param name="force">Force the update even if the bodycamera is already disabled</param>
    protected virtual bool TryDisable(Entity<BodyCameraComponent> bodyCamera, EntityUid? user = null)
    {
        if (!bodyCamera.Comp.Enabled)
            return false;

        if (user == null && !TryGetWearer(bodyCamera, out user))
            return false;

        Audio.PlayPredicted(bodyCamera.Comp.PowerOffSound, bodyCamera, user);

        var message = Loc.GetString(bodyCamera.Comp.CameraOffUse);
        Popup.PopupClient(message, bodyCamera, user.Value);

        bodyCamera.Comp.Enabled = false;
        PowerCell.SetPowerCellDrawEnabled(bodyCamera, false);
        return true;
    }

    /// <summary>
    /// Locks the power cell slot so it cannot be removed (or inserted into)
    /// </summary>
    protected void SetPowerCellLock(Entity<BodyCameraComponent> bodyCamera, bool locked)
    {
        if (!TryComp<PowerCellSlotComponent>(bodyCamera, out var powerCellSlotComponent))
            return;

        _itemSlotsSystem.SetLock(bodyCamera, powerCellSlotComponent.CellSlotId, locked);
    }

    protected bool TryGetWearer(EntityUid bodyCamera, [NotNullWhen(true)] out EntityUid? wearer)
    {
        wearer = null;

        if (!_containerSystem.TryGetContainingContainer(bodyCamera, out var container))
            return false;

        if (container is null)
            return false;

        wearer = container.Owner;

        return true;
    }
}
