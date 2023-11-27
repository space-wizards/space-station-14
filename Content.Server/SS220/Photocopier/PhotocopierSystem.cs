// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Server.Chat.Systems;
using Content.Shared.SS220.Photocopier;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Server.UserInterface;
using Content.Server.Power.Components;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.SS220.Photocopier.Forms;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Popups;
using Content.Shared.SS220.ButtScan;
using Content.Shared.SS220.ShapeCollisionTracker;
using Robust.Shared.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;

namespace Content.Server.SS220.Photocopier;

public sealed partial class PhotocopierSystem : EntitySystem
{
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    private FormManager? _specificFormManager;
    private readonly ISawmill _sawmill = Logger.GetSawmill("photocopier");

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _specificFormManager = _sysMan.GetEntitySystem<FormManager>();

        SubscribeLocalEvent<PhotocopierComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PhotocopierComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<PhotocopierComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<PhotocopierComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<PhotocopierComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<PhotocopierComponent, ShapeCollisionTrackerUpdatedEvent>(OnCollisionChanged);
        SubscribeLocalEvent<PhotocopierComponent, ComponentShutdown>(OnShutdown);

        // UI
        SubscribeLocalEvent<PhotocopierComponent, AfterActivatableUIOpenEvent>(OnToggleInterface);
        SubscribeLocalEvent<PhotocopierComponent, PhotocopierPrintMessage>(OnPrintButtonPressed);
        SubscribeLocalEvent<PhotocopierComponent, PhotocopierCopyMessage>(OnCopyButtonPressed);
        SubscribeLocalEvent<PhotocopierComponent, PhotocopierStopMessage>(OnStopButtonPressed);
        SubscribeLocalEvent<PhotocopierComponent, ExaminedEvent>(OnExamine);
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PhotocopierComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var photocopier, out var receiver))
        {
            if (!receiver.Powered)
                continue;

            ProcessPrinting(uid, frameTime, photocopier);
        }
    }

    /// <summary>
    /// Tries to get toner cartridge from toner cartridge slot of specified component
    /// </summary>
    private bool TryGetTonerCartridge(
        PhotocopierComponent component,
        [NotNullWhen(true)] out TonerCartridgeComponent? tonerCartridgeComponent)
    {
        var tonerSlotItem = component.TonerSlot.Item;
        if (tonerSlotItem is not null)
            return TryComp(tonerSlotItem, out tonerCartridgeComponent);

        tonerCartridgeComponent = null;
        return false;
    }

    #region EventListeners

    private void OnComponentInit(EntityUid uid, PhotocopierComponent component, ComponentInit args)
    {
        _itemSlots.AddItemSlot(uid, PhotocopierComponent.PaperSlotId, component.PaperSlot);
        _itemSlots.AddItemSlot(uid, PhotocopierComponent.TonerSlotId, component.TonerSlot);
        TryUpdateVisualState(uid, component);
    }

    private void OnComponentRemove(EntityUid uid, PhotocopierComponent component, ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(uid, component.PaperSlot);
        _itemSlots.RemoveItemSlot(uid, component.TonerSlot);
    }

    private void OnShutdown(EntityUid uid, PhotocopierComponent component, ComponentShutdown args)
    {
        component.EntityOnTop = null;
        component.HumanoidAppearanceOnTop = null;
        component.PrintAudioStream = null;
    }

    private void OnCollisionChanged(
        EntityUid uid,
        PhotocopierComponent component,
        ShapeCollisionTrackerUpdatedEvent args)
    {
        if (component.EntityOnTop is { } currentEntity &&
            component.HumanoidAppearanceOnTop is not null &&
            args.Colliding.Contains(currentEntity) &&
            !Deleted(currentEntity))
        {
            return;
        }

        component.EntityOnTop = null;
        component.HumanoidAppearanceOnTop = null;

        foreach (var otherEntity in args.Colliding)
        {
            if (!TryComp<HumanoidAppearanceComponent>(otherEntity, out var humanoidAppearance))
                continue;

            component.HumanoidAppearanceOnTop = humanoidAppearance;
            component.EntityOnTop = otherEntity;
            break;
        }

        UpdateUserInterface(uid, component);
    }

    private void OnToggleInterface(EntityUid uid, PhotocopierComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnExamine(EntityUid uid, PhotocopierComponent component, ExaminedEvent args)
    {
        if (component.PaperSlot.Item is null)
            return;

        args.PushText(Loc.GetString("photocopier-examine-scan-got-item"));
    }

    private void OnItemSlotChanged(EntityUid uid, PhotocopierComponent component, ContainerModifiedMessage args)
    {
        switch (args.Container.ID)
        {
            // Paper slot: Manually play paper insert sound, so it can be stopped if power is lost
            case PhotocopierComponent.PaperSlotId:
                if (component.PaperSlot.Item is not null && this.IsPowered(uid, EntityManager))
                    _audio.PlayPvs(component.PaperInsertSound, uid);
                break;

            // Toner slot: Stop printing if toner cartridge is yoinked
            case PhotocopierComponent.TonerSlotId when component.TonerSlot.Item is null:
                StopPrinting(uid, component, false);
                break;
        }

        UpdateUserInterface(uid, component);
        TryUpdateVisualState(uid, component);
    }

    private void OnPowerChanged(EntityUid uid, PhotocopierComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            StopPrinting(uid, component, false);
            component.ManualButtBurnAnimationRemainingTime = null;
        }

        TryUpdateVisualState(uid, component);
    }

    private void OnCopyButtonPressed(EntityUid uid, PhotocopierComponent component, PhotocopierCopyMessage args)
    {
        if (!component.Initialized)
            return;

        if (component.CopiesQueued > 0)
            return;

        if (!TryGetTonerCartridge(component, out var tonerCartridge) || tonerCartridge.Charges <= 0)
            return;

        // Prioritize inserted paper over butt
        if (TryQueueCopySlot(uid, component, args.Amount))
            return;

        if (component.EntityOnTop is not { } entityOnTop ||
            component.HumanoidAppearanceOnTop is not { } humanoidAppearanceOnTop ||
            Deleted(entityOnTop))
            return;

        TryQueueCopyPhysicalButt(uid, component, humanoidAppearanceOnTop, args.Amount);
        if (component.BurnsButts)
            BurnButt(entityOnTop, uid, component);
    }

    private void OnPrintButtonPressed(EntityUid uid, PhotocopierComponent component, PhotocopierPrintMessage args)
    {
        if (!component.Initialized)
            return;

        if (_specificFormManager is null)
            return;

        if (component.CopiesQueued > 0)
            return;

        if (!TryGetTonerCartridge(component, out var tonerCartridge) || tonerCartridge.Charges <= 0)
            return;

        var formToCopy = _specificFormManager.TryGetFormFromDescriptor(args.Descriptor);
        if (formToCopy is null)
            return;

        FormToDataToCopy(formToCopy, out var dataToCopy, out var metaDataToCopy);
        StartPrinting(uid, component, metaDataToCopy, dataToCopy, PhotocopierState.Copying, args.Amount);
    }

    private void OnStopButtonPressed(EntityUid uid, PhotocopierComponent component, PhotocopierStopMessage args)
    {
        StopPrinting(uid, component);
    }

    #endregion

    private bool IsHumanoidOnTop(PhotocopierComponent component)
    {
        return component.HumanoidAppearanceOnTop is not null &&
               component.EntityOnTop is { } entityOnTop &&
               !Deleted(entityOnTop);
    }

    /// <summary>
    /// Locks/unlocks contraband forms by adding them to available form collections hashset.
    /// Used by PhotocopierSusFormsWireAction.
    /// </summary>
    /// <param name="uid">EntityUid of photocopier entity</param>
    /// <param name="component">PhotocopierComponent to lock/unlock contraband at</param>
    /// <param name="unlocked">To add contraband or to remove?</param>
    public void SetContrabandFormsUnlocked(EntityUid uid, PhotocopierComponent component, bool unlocked)
    {
        foreach (var collection in component.ContrabandFormCollections)
        {
            if (unlocked)
                component.FormCollections.Add(collection);
            else
                component.FormCollections.Remove(collection);
        }

        component.SusFormsUnlocked = unlocked;
        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Tries to burn the butt of an entity on top of the specified photocopier.
    /// Has a cooldown, regardless if there is an entity on top.
    /// Also plays the animation and sound effect regardless if there is an entity on top.
    /// </summary>
    public void TryManuallyBurnButtOnTop(EntityUid uid, PhotocopierComponent component)
    {
        if (component.ManualButtBurnAnimationRemainingTime > 0)
            return;

        component.ManualButtBurnAnimationRemainingTime = component.ManualButtBurnDuration;
        TryUpdateVisualState(uid, component);

        if (component.EntityOnTop is not { } entityOnTop)
        {
            // play the sound regardless to catch the user's attention,. Will otherwise be done by BurnButt method.
            _audio.PlayPvs(component.ButtDamageSound, uid);
            return;
        }

        BurnButt(entityOnTop, uid, component);
    }

    /// <summary>
    /// Makes the photocopier burn the butt of a mob.
    /// </summary>
    /// <param name="mobUid">UID of mob, whose but to burn</param>
    /// <param name="photocopierUid">UID of photocopier that tries to burn a butt</param>
    /// <param name="component">Component of photocopier that burns.</param>
    private void BurnButt(EntityUid mobUid, EntityUid photocopierUid, PhotocopierComponent component)
    {
        if (component.ButtDamage is null)
            return;

        // TODO LATER: Когда добавим куклу нужно сделать так чтоб дамаг шел в гроин
        if (!TryComp<DamageableComponent>(mobUid, out var damageable))
            return;

        var dealtDamage = _damageableSystem.TryChangeDamage(
            mobUid, component.ButtDamage, false, false, damageable, photocopierUid);

        _audio.PlayPvs(component.ButtDamageSound, photocopierUid);

        // AAAAAAAAAAAAAAAAAAAAAAAAAAA
        if (dealtDamage is null || dealtDamage.Empty)
            return; //...but only if it dealt damage

        _chat.TryEmoteWithChat(mobUid, "Scream", ChatTransmitRange.GhostRangeLimit);
        _popup.PopupEntity(Loc.GetString("photocopier-popup-butt-burn"), photocopierUid, PopupType.SmallCaution);
    }

    /// <summary>
    /// Queues copy operation to copy an ass of specified HumanoidAppearanceComponent.
    /// Causes photocopier to check for HumanoidAppearanceComponents on top of it every tick.
    /// </summary>
    private void TryQueueCopyPhysicalButt(
        EntityUid uid,
        PhotocopierComponent component,
        HumanoidAppearanceComponent humanoidAppearance,
        int amount)
    {
        if (!TryGetTonerCartridge(component, out var tonerCartridge) || tonerCartridge.Charges <= 0)
            return;

        if (!_prototypeManager.TryIndex<SpeciesPrototype>(humanoidAppearance.Species, out var speciesPrototype))
            return;

        if(!component.BurnsButts)
            _popup.PopupEntity(Loc.GetString("photocopier-popup-butt-scan"), uid);

        var dataToCopy = new Dictionary<Type, IPhotocopiedComponentData>();
        var metaDataToCopy = new PhotocopyableMetaData() {PrototypeId = "ButtScan"};

        var buttScanData = new ButtScanPhotocopiedData() { ButtTexturePath = speciesPrototype.ButtScanTexture };
        dataToCopy.Add(typeof(ButtScanComponent), buttScanData);

        if (StartPrinting(uid, component, metaDataToCopy, dataToCopy, PhotocopierState.Copying, amount))
        {
            component.IsCopyingPhysicalButt = true;
            component.ButtSpecies = humanoidAppearance.Species;
        }
    }

    /// <summary>
    /// Caches data of item in paper slot and queues copy operation
    /// </summary>
    private bool TryQueueCopySlot(EntityUid uid, PhotocopierComponent component, int amount)
    {
        if (component.PaperSlot.Item is not { } copyEntity)
            return false;

        if (!TryGetPhotocopyableMetaData(copyEntity, out var metaData))
            return false;

        var dataToCopy = GetDataToCopyFromEntity(copyEntity);
        if (dataToCopy.Count == 0)
            return false;

        StartPrinting(uid, component, metaData, dataToCopy, PhotocopierState.Copying, amount);

        return true;
    }

    /// <summary>
    /// Does everything that ResetState does, plus stops sound.
    /// Effectively equal to pressing a stop button.
    /// </summary>
    private void StopPrinting(EntityUid uid, PhotocopierComponent component, bool updateVisualsAndUi = true)
    {
        ResetState(uid, component);
        StopPrintingSound(component, _audio);

        if (updateVisualsAndUi)
        {
            UpdateUserInterface(uid, component);
            TryUpdateVisualState(uid, component);
        }
    }

    private bool StartPrinting(
        EntityUid uid,
        PhotocopierComponent component,
        PhotocopyableMetaData? metaData,
        Dictionary<Type, IPhotocopiedComponentData>? dataToCopy,
        PhotocopierState state,
        int amount)
    {
        if (amount <= 0)
            return false;

        component.DataToCopy = dataToCopy;
        component.MetaDataToCopy = metaData;
        component.State = state;
        component.CopiesQueued = Math.Clamp(amount, 0, component.MaxQueueLength);

        if (state == PhotocopierState.Copying)
            _itemSlots.SetLock(uid, component.PaperSlot, true);

        return true;
    }

    /// <summary>
    /// Stops PhotocopierComponent from printing and clears queue. Resets cached data. Unlocks the paper slot.
    /// </summary>
    private void ResetState(EntityUid uid, PhotocopierComponent component)
    {
        component.CopiesQueued = 0;
        component.PrintingTimeRemaining = 0;
        component.DataToCopy = null;
        component.MetaDataToCopy = null;
        component.ButtSpecies = null;
        component.State = PhotocopierState.Idle;
        component.IsCopyingPhysicalButt = false;

        _itemSlots.SetLock(uid, component.PaperSlot, false);
    }

    private void ProcessPrinting(EntityUid uid, float frameTime, PhotocopierComponent component)
    {
        if (component.ManualButtBurnAnimationRemainingTime is not null)
        {
            component.ManualButtBurnAnimationRemainingTime -= frameTime;
            if (component.ManualButtBurnAnimationRemainingTime <= 0)
            {
                TryUpdateVisualState(uid, component);
                component.ManualButtBurnAnimationRemainingTime = null;
            }
        }

        if (component.PrintingTimeRemaining > 0)
        {
            if (!TryGetTonerCartridge(component, out var tonerCartridge) || tonerCartridge.Charges <= 0)
            {
                StopPrinting(uid, component);
                return;
            }

            if (component.IsCopyingPhysicalButt)
            {
                // If there is no butt or someones else butt is in the way - stop copying.
                if (component.HumanoidAppearanceOnTop is not { } humanoid
                    || component.ButtSpecies is null
                    || component.ButtSpecies != humanoid.Species)
                {
                    StopPrinting(uid, component);
                    return;
                }
            }

            component.PrintingTimeRemaining -= frameTime;

            var isPrinted = component.PrintingTimeRemaining <= 0;
            if (!isPrinted)
                return;

            SpawnCopyFromPhotocopier(uid, component);

            tonerCartridge.Charges--;
            component.CopiesQueued--;

            if (component.CopiesQueued <= 0)
                ResetState(uid, component); //Reset the rest of the fields

            UpdateUserInterface(uid, component);
            TryUpdateVisualState(uid, component);

            return;
        }

        if (component.CopiesQueued > 0)
        {
            if (!TryGetTonerCartridge(component, out var tonerCartridge) || tonerCartridge.Charges <= 0)
            {
                ResetState(uid, component);
                UpdateUserInterface(uid, component);
                TryUpdateVisualState(uid, component);
                return;
            }

            component.PrintingTimeRemaining = component.PrintingTime;
            component.PrintAudioStream = _audio.PlayPvs(component.PrintSound, uid)?.Entity;

            if (component.State == PhotocopierState.Copying)
                _itemSlots.SetLock(uid, component.PaperSlot, true);

            UpdateUserInterface(uid, component);
            TryUpdateVisualState(uid, component);
        }
    }

    /// <summary>
    /// Updates visual state using appearance system & component
    /// </summary>
    private void TryUpdateVisualState(EntityUid uid, PhotocopierComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = PhotocopierVisualState.Powered;
        var outOfToner = (!TryGetTonerCartridge(component, out var tonerCartridge) || tonerCartridge.Charges <= 0);

        if (!this.IsPowered(uid, EntityManager))
            state = PhotocopierVisualState.Off;
        else if (component.CopiesQueued > 0)
            state = (component.State == PhotocopierState.Copying) ? PhotocopierVisualState.Copying : PhotocopierVisualState.Printing;
        else if (outOfToner)
            state = PhotocopierVisualState.OutOfToner;

        var item = component.PaperSlot.Item;
        var gotItem = item != null;

        var burnsButtManually = component.ManualButtBurnAnimationRemainingTime > 0;
        var combinedState = new PhotocopierCombinedVisualState(state, gotItem, component.BurnsButts, burnsButtManually);

        _appearance.SetData(uid, PhotocopierVisuals.VisualState, combinedState);
    }

    /// <summary>
    /// Stops the audio stream of a printing sound, dereferences it
    /// </summary>
    private static void StopPrintingSound(PhotocopierComponent component, SharedAudioSystem audio)
    {
        component.PrintAudioStream = audio.Stop(component.PrintAudioStream);
        component.PrintAudioStream = null;
    }

    private void UpdateUserInterface(EntityUid uid, PhotocopierComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        int tonerAvailable;
        int tonerCapacity;
        if (!TryGetTonerCartridge(component, out var tonerCartridge))
        {
            tonerAvailable = 0;
            tonerCapacity = 0;
        }
        else
        {
            tonerAvailable = tonerCartridge.Charges;
            tonerCapacity = tonerCartridge.Capacity;
        }

        var isPaperInserted = component.PaperSlot.Item is not null;
        var assIsOnScanner = IsHumanoidOnTop(component);

        var state = new PhotocopierUiState(
            component.PaperSlot.Locked,
            isPaperInserted,
            component.CopiesQueued,
            component.FormCollections,
            tonerAvailable,
            tonerCapacity,
            assIsOnScanner,
            component.MaxQueueLength);

        _userInterface.TrySetUiState(uid, PhotocopierUiKey.Key, state);
    }
}
