// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared.SS220.Photocopier;
using Content.Shared.SS220.Photocopier.Forms;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Server.UserInterface;
using Content.Server.Power.Components;
using Content.Server.Paper;
using Content.Server.Power.EntitySystems;
using Content.Server.SS220.Photocopier.Forms;
using Content.Shared.Damage;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.SS220.ButtScan;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Photocopier;

public sealed class PhotocopierSystem : EntitySystem
{
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

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
        SubscribeLocalEvent<PhotocopierComponent, GotEmaggedEvent>(OnEmagged);

        // UI
        SubscribeLocalEvent<PhotocopierComponent, AfterActivatableUIOpenEvent>(OnToggleInterface);
        SubscribeLocalEvent<PhotocopierComponent, PhotocopierPrintMessage>(OnPrintButtonPressed);
        SubscribeLocalEvent<PhotocopierComponent, PhotocopierCopyMessage>(OnCopyButtonPressed);
        SubscribeLocalEvent<PhotocopierComponent, PhotocopierStopMessage>(OnStopButtonPressed);
        SubscribeLocalEvent<PhotocopierComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<PhotocopierComponent, PhotocopierRefreshUiMessage>(OnRefreshUiMessage);
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
    /// Try to get a HumanoidAppearanceComponent of one of the creatures that are on top of the photocopier at the moment.
    /// Returns null if there are none.
    /// </summary>
    private bool TryGetHumanoidOnTop(
        EntityUid uid,
        [NotNullWhen(true)] out HumanoidAppearanceComponent? humanoidAppearance)
    {
        if (!TryComp<TransformComponent>(uid, out var xform))
        {
            humanoidAppearance = null;
            return false;
        }

        var map = xform.MapID;
        var bounds = _physics.GetWorldAABB(uid);

        // We shrink the box greatly to ensure it only intersects with the objects that are on top of the photocopier.
        // May be a hack, but at least it works reliably (on my computer)

        // lerp alpha (effective alpha will be twice as big since we perform lerp on both corners)
        const float shrinkCoefficient = 0.4f;
        // lerp corners towards each other
        var boundsTR = bounds.TopRight;
        var boundsBL = bounds.BottomLeft;
        bounds.TopRight = (boundsBL - boundsTR) * shrinkCoefficient + boundsTR;
        bounds.BottomLeft = (boundsTR - boundsBL) * shrinkCoefficient + boundsBL;

        var intersecting = _entityLookup.GetComponentsIntersecting<HumanoidAppearanceComponent>(
            map, bounds, LookupFlags.Dynamic | LookupFlags.Sundries);

        humanoidAppearance = intersecting.Count > 0 ? intersecting.ElementAt(0) : null;
        return humanoidAppearance is not null;
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

    private void OnToggleInterface(EntityUid uid, PhotocopierComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private static void OnExamine(EntityUid uid, PhotocopierComponent component, ExaminedEvent args)
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
        if (TryQueueCopySlot(component, args.Amount))
            return;

        if (TryGetHumanoidOnTop(uid, out var humanoidOnScanner))
        {
            TryQueueCopyPhysicalButt(component, humanoidOnScanner, args.Amount);
            if(HasComp<EmaggedComponent>(uid))
                BurnButt(humanoidOnScanner.Owner, uid, component);
        }
    }

    private void OnPrintButtonPressed(EntityUid uid, PhotocopierComponent component, PhotocopierPrintMessage args)
    {
        if (!component.Initialized)
            return;

        if(_specificFormManager is null)
            return;

        if (component.CopiesQueued > 0)
            return;

        if (!TryGetTonerCartridge(component, out var tonerCartridge) || tonerCartridge.Charges <= 0)
            return;

        component.DataToCopy = _specificFormManager.TryGetFormFromDescriptor(args.Descriptor);
        if (component.DataToCopy is null)
            return;

        component.CopiesQueued = Math.Clamp(args.Amount, 0, component.MaxQueueLength);
    }

    private void OnStopButtonPressed(EntityUid uid, PhotocopierComponent component, PhotocopierStopMessage args)
    {
        StopPrinting(uid, component);
    }

    private void OnRefreshUiMessage(EntityUid uid, PhotocopierComponent component, PhotocopierRefreshUiMessage args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnEmagged(EntityUid uid, PhotocopierComponent component, ref GotEmaggedEvent args)
    {
        _audio.PlayPvs(component.EmagSound, uid);
        args.Handled = true;
    }

    private void BurnButt(EntityUid mobUid, EntityUid photocopierUid, PhotocopierComponent component)
    {
        if (component.EmagButtDamage is null)
            return;

        // TODO LATER: Когда добавим куклу нужно сделать так чтоб дамаг шел в гроин
        if(!TryComp<DamageableComponent>(mobUid, out var damageable))
            return;

        var dealtDamage = _damageableSystem.TryChangeDamage(
            mobUid, component.EmagButtDamage, false, false, damageable, photocopierUid);

        _audio.PlayPvs(component.ButtDamageSound, photocopierUid);

        // AAAAAAAAAAAAAAAAAAAAAAAAAAA
        if (dealtDamage is null || dealtDamage.Empty)
            return; //...but only if it dealt damage
        _chat.TryEmoteWithChat(mobUid, "Scream", ChatTransmitRange.GhostRangeLimit);
    }

    /// <summary>
    /// Queues copy operation to copy an ass of specified HumanoidAppearanceComponent.
    /// Causes photocopier to check for HumanoidAppearanceComponents on top of it every tick.
    /// </summary>
    private void TryQueueCopyPhysicalButt(
        PhotocopierComponent component,
        HumanoidAppearanceComponent humanoidAppearance,
        int amount)
    {
        if (!TryGetTonerCartridge(component, out var tonerCartridge) || tonerCartridge.Charges <= 0)
            return;

        if (!_prototypeManager.TryIndex<SpeciesPrototype>(humanoidAppearance.Species, out var speciesPrototype))
            return;

        component.IsScanning = true;
        component.IsCopyingButt = true;
        component.IsCopyingPhysicalButt = true;
        component.ButtSpecies = humanoidAppearance.Species;
        component.ButtTextureToCopy = speciesPrototype.ButtScanTexture;
        component.CopiesQueued = Math.Clamp(amount, 0, component.MaxQueueLength);
    }

    /// <summary>
    /// Caches paper data (if paper is in) and queues copy operation
    /// </summary>
    private bool TryQueueCopySlot(PhotocopierComponent component, int amount)
    {
        var copyEntity = component.PaperSlot.Item;
        if (copyEntity is null)
            return false;

        if (TryComp<ButtScanComponent>(copyEntity, out var buttScan))
        {
            component.IsCopyingButt = true;
            component.ButtTextureToCopy = buttScan.ButtTexturePath;
        }
        else if (
            TryComp<MetaDataComponent>(copyEntity, out var metadata) &&
            TryComp<PaperComponent>(copyEntity, out var paper))
        {
            component.DataToCopy = new Form(
                metadata.EntityName,
                paper.Content,
                prototypeId: metadata.EntityPrototype?.ID,
                stampState: paper.StampState,
                stampedBy: paper.StampedBy);
        }
        else
        {
            return false;
        }

        component.IsScanning = true;
        component.CopiesQueued = Math.Clamp(amount, 0, component.MaxQueueLength);

        return true;
    }

    /// <summary>
    /// Does everything that ResetState does, plus stops sound.
    /// Effectively equal to pressing a stop button.
    /// </summary>
    private void StopPrinting(EntityUid uid, PhotocopierComponent component, bool updateVisualsAndUi = true)
    {
        ResetState(uid, component);
        StopPrintingSound(component);

        if (updateVisualsAndUi)
        {
            UpdateUserInterface(uid, component);
            TryUpdateVisualState(uid, component);
        }
    }

    /// <summary>
    /// Stops PhotocopierComponent from printing and clears queue. Resets cached data. Unlocks the paper slot.
    /// </summary>
    private void ResetState(EntityUid uid, PhotocopierComponent component)
    {
        component.CopiesQueued = 0;
        component.PrintingTimeRemaining = 0;
        component.DataToCopy = null;
        component.ButtTextureToCopy = null;
        component.ButtSpecies = null;
        component.IsScanning = false;
        component.IsCopyingButt = false;
        component.IsCopyingPhysicalButt = false;

        _itemSlots.SetLock(uid, component.PaperSlot, false);
    }

    /// <summary>
    /// Spawns a copy of paper using data cached in component.DataToCopy.
    /// </summary>
    private void SpawnPaperCopy(EntityUid uid, PhotocopierComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var printout = component.DataToCopy;
        if (printout is null)
        {
            _sawmill.Error("Entity " + uid + " tried to spawn a copy of paper, but DataToCopy was null.");
            return;
        }

        var entityToSpawn = string.IsNullOrEmpty(printout.PrototypeId) ? "Paper" : printout.PrototypeId;
        var printed = EntityManager.SpawnEntity(entityToSpawn, Transform(uid).Coordinates);

        if (TryComp<PaperComponent>(printed, out _))
        {
            _paperSystem.SetContent(printed, printout.Content);

            // Apply stamps
            if (printout.StampState is not null)
            {
                foreach (var stampedBy in printout.StampedBy)
                {
                    _paperSystem.TryStamp(printed, stampedBy, printout.StampState);
                }
            }
        }

        if (!TryComp<MetaDataComponent>(printed, out var metadata))
            return;

        if (!string.IsNullOrEmpty(printout.EntityName))
            metadata.EntityName = printout.EntityName;
    }

    /// <summary>
    /// Spawns a ButtScan on a photocopier using butt texture path that was stored in PhotocopierComponent.
    /// </summary>
    private void SpawnButtCopy(EntityUid uid, PhotocopierComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.ButtTextureToCopy is null)
            return;

        var printed = EntityManager.SpawnEntity("ButtScan", Transform(uid).Coordinates);
        if (TryComp<ButtScanComponent>(printed, out var buttScan))
            buttScan.SetAndDirtyIfChanged(ref buttScan.ButtTexturePath, component.ButtTextureToCopy);
    }

    private void ProcessPrinting(EntityUid uid, float frameTime, PhotocopierComponent component)
    {
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
                if (!TryGetHumanoidOnTop(uid, out var humanoid)
                    || component.ButtSpecies != humanoid.Species)
                {
                    StopPrinting(uid, component);
                    return;
                }
            }

            component.PrintingTimeRemaining -= frameTime;

            var isPrinted = component.PrintingTimeRemaining <= 0;
            if(!isPrinted)
                return;

            if (component.IsCopyingButt)
                SpawnButtCopy(uid, component);
            else
                SpawnPaperCopy(uid, component);

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
            component.PrintAudioStream = _audio.PlayPvs(component.PrintSound, uid);

            if (component.IsScanning)
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

        if (component.CopiesQueued > 0)
            state = component.IsScanning? PhotocopierVisualState.Copying : PhotocopierVisualState.Printing;
        else if (!this.IsPowered(uid, EntityManager))
            state = PhotocopierVisualState.Off;
        else if (outOfToner)
            state = PhotocopierVisualState.OutOfToner;

        var item = component.PaperSlot.Item;
        var gotItem = item != null;
        var combinedState = new PhotocopierCombinedVisualState(state, gotItem, HasComp<EmaggedComponent>(uid));

        _appearance.SetData(uid, PhotocopierVisuals.VisualState, combinedState);
    }

    /// <summary>
    /// Stops audio stream of a printing sound, dereferences it
    /// </summary>
    private static void StopPrintingSound(PhotocopierComponent component)
    {
        component.PrintAudioStream?.Stop();
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
        var assIsOnScanner = TryGetHumanoidOnTop(uid, out _);

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
