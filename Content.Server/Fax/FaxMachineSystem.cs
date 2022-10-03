using Content.Server.Administration;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Paper;
using Content.Server.Popups;
using Content.Server.Tools;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Emag.Systems;
using Content.Shared.Fax;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Fax;

// TODO: Transfer paper stamps
// TODO: Remove verbs and add UI, guh
// Should Syndicate saw all CantCom channels?

public sealed class FaxMachineSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ToolSystem _toolSystem = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;

    public const string PaperSlotId = "FaxMachine-paper";

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<FaxMachineComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<FaxMachineComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FaxMachineComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<FaxMachineComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<FaxMachineComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<FaxMachineComponent, GetVerbsEvent<Verb>>(OnVerb);
        SubscribeLocalEvent<FaxMachineComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<FaxMachineComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<FaxMachineComponent, GotEmaggedEvent>(OnEmagged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var comp in EntityQuery<FaxMachineComponent>())
        {
            // Printing animation
            if (comp.PrintingTimeRemaining > 0)
            {
                comp.PrintingTimeRemaining -= frameTime;
                UpdateAppearance(comp.Owner, comp);

                var isAnimationEnd = comp.PrintingTimeRemaining <= 0;
                if (isAnimationEnd)
                    SpawnPaperFromQueue(comp.Owner, comp);
            }
            else if (comp.PrintingQueue.Count > 0)
            {
                comp.PrintingTimeRemaining = comp.PrintingTime;
                _audioSystem.PlayPvs(comp.PrintSound, comp.Owner);
            }

            // Inserting animation
            if (comp.InsertingTimeRemaining > 0)
            {
                comp.InsertingTimeRemaining -= frameTime;
                UpdateAppearance(comp.Owner, comp);

                var isAnimationEnd = comp.InsertingTimeRemaining <= 0;
                if (isAnimationEnd)
                    _itemSlotsSystem.SetLock(comp.Owner, comp.PaperSlot, false);
            }

            // Sending timeout
            if (comp.SendTimeoutRemaining > 0)
                comp.SendTimeoutRemaining -= frameTime;
        }
    }

    private void OnComponentInit(EntityUid uid, FaxMachineComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, FaxMachineSystem.PaperSlotId, component.PaperSlot);
        UpdateAppearance(uid, component);
    }

    private void OnComponentRemove(EntityUid uid, FaxMachineComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.PaperSlot);
    }

    private void OnMapInit(EntityUid uid, FaxMachineComponent component, MapInitEvent args)
    {
        // Load all faxes on map in cache each other to prevent taking same name by user created fax
        Refresh(uid, component);
    }

    private void OnItemSlotChanged(EntityUid uid, FaxMachineComponent component, ContainerModifiedMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.PaperSlot.ID)
            return;

        var isPaperInserted = component.PaperSlot.Item.HasValue;
        if (isPaperInserted)
        {
            component.InsertingTimeRemaining = component.InsertionTime;
            _itemSlotsSystem.SetLock(uid, component.PaperSlot, true);
        }
    }
    
    private void OnInteractUsing(EntityUid uid, FaxMachineComponent component, InteractUsingEvent args)
    {
        if (args.Handled ||
            !TryComp<ActorComponent>(args.User, out var actor) ||
            !_toolSystem.HasQuality(args.Used, "Screwing")) // Screwing because Pulsing already used by device linking
            return;

        _quickDialog.OpenDialog(actor.PlayerSession,
            Loc.GetString("fax-machine-dialog-rename"),
            Loc.GetString("fax-machine-dialog-field-name"),
            (string newName) =>
        {
            if (component.FaxName == newName)
                return;

            if (newName.Length > 20)
            {
                _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-name-long"), uid, Filter.Pvs(uid));
                return;
            }

            if (component.KnownFaxes.ContainsValue(newName) && !component.Emagged) // Allow exist names if emagged for fun
            {
                _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-name-exist"), uid, Filter.Pvs(uid));
                return;
            }

            args.Handled = true;
            component.FaxName = newName;
            _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-name-set"), uid, Filter.Pvs(uid));
        });
    }
    
    private void OnEmagged(EntityUid uid, FaxMachineComponent component, GotEmaggedEvent args)
    {
        if (component.Emagged)
            return;

        _audioSystem.PlayPvs(component.EmagSound, uid);
        component.Emagged = true;
        args.Handled = true;
    }

    private void OnVerb(EntityUid uid, FaxMachineComponent component, GetVerbsEvent<Verb> args)
    {
        // standard interaction checks
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;
        
        // send verb
        var isSendTimeout = component.SendTimeoutRemaining > 0;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("fax-machine-verb-send"),
            Message = Loc.GetString("fax-machine-verb-send-desc"),
            Act = () => Send(uid),
            Disabled = isSendTimeout,
        });
        
        // refresh network verb
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("fax-machine-verb-refresh"),
            Message = Loc.GetString("fax-machine-verb-refresh-desc"),
            Act = () => Refresh(uid),
        });

        // destination select verbs
        foreach (var (faxAddress, faxName) in component.KnownFaxes)
        {
            args.Verbs.Add(new()
            {
                Text = faxName,
                Message = Loc.GetString("fax-machine-verb-destination-desc"),
                Category = VerbCategory.FaxDestination,
                Act = () => SetDestination(uid, faxAddress),
            });
        }
    }
    
    private void OnPacketReceived(EntityUid uid, FaxMachineComponent component, DeviceNetworkPacketEvent args)
    {
        if (!HasComp<DeviceNetworkComponent>(uid) || string.IsNullOrEmpty(args.SenderAddress))
            return;

        if (args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
        {
            switch (command)
            {
                case FaxMachineConstants.FaxPingCommand:
                    var isForSyndie = component.Emagged &&
                                      args.Data.ContainsKey(FaxMachineConstants.FaxSyndicateData);
                    if (!isForSyndie && !component.ShouldResponsePings)
                        return;

                    var payload = new NetworkPayload()
                    {
                        { DeviceNetworkConstants.Command, FaxMachineConstants.FaxPongCommand },
                        { FaxMachineConstants.FaxNameData, component.FaxName }
                    };
                    _deviceNetworkSystem.QueuePacket(uid, args.SenderAddress, payload);

                    break;
                case FaxMachineConstants.FaxPongCommand:
                    if (!args.Data.TryGetValue(FaxMachineConstants.FaxNameData, out string? faxName))
                        return;

                    component.KnownFaxes[args.SenderAddress] = faxName;

                    break;
                case FaxMachineConstants.FaxPrintCommand:
                    if (!args.Data.TryGetValue(FaxMachineConstants.FaxContentData, out string? content))
                        return;

                    Receive(uid, content, args.SenderAddress);

                    break;
            }
        }
    }

    private void UpdateAppearance(EntityUid uid, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.InsertingTimeRemaining > 0)
            _appearanceSystem.SetData(uid, FaxMachineVisuals.VisualState, FaxMachineVisualState.Inserting);
        else if (component.PrintingTimeRemaining > 0)
            _appearanceSystem.SetData(uid, FaxMachineVisuals.VisualState, FaxMachineVisualState.Printing);
        else
            _appearanceSystem.SetData(uid, FaxMachineVisuals.VisualState, FaxMachineVisualState.Normal);
    }

    public void SetDestination(EntityUid uid, string destAddress, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.DestinationFaxAddress = destAddress;

        var faxName = Loc.GetString("fax-machine-popup-source-unknown");
        if (component.KnownFaxes.ContainsKey(destAddress)) // If admin manually set address unknown for fax
            faxName = component.KnownFaxes[destAddress];

        var msg = Loc.GetString("fax-machine-popup-destination", ("destination", faxName));
        _popupSystem.PopupEntity(msg, uid, Filter.Pvs(uid));
    }

    public void Refresh(EntityUid uid, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.DestinationFaxAddress = null;
        component.KnownFaxes.Clear();
        
        var payload = new NetworkPayload()
        {
            { DeviceNetworkConstants.Command, FaxMachineConstants.FaxPingCommand }
        };

        if (component.Emagged)
            payload.Add(FaxMachineConstants.FaxSyndicateData, true);

        _deviceNetworkSystem.QueuePacket(uid, null, payload);
    }

    public void Send(EntityUid uid, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var sendEntity = component.PaperSlot.Item;
        if (sendEntity == null)
        {
            _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-paper-not-inserted"), uid, Filter.Pvs(uid));
            return;
        }

        if (component.DestinationFaxAddress == null)
        {
            _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-destination-not-selected"), uid, Filter.Pvs(uid));
            return;
        }
        
        if (!component.KnownFaxes.TryGetValue(component.DestinationFaxAddress, out var faxName))
        {
            _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-destination-not-found"), uid, Filter.Pvs(uid));
            return;
        }

        if (!TryComp<PaperComponent>(sendEntity, out var paper))
            return;

        var payload = new NetworkPayload()
        {
            { DeviceNetworkConstants.Command, FaxMachineConstants.FaxPrintCommand },
            { FaxMachineConstants.FaxContentData, paper.Content }
        };
        _deviceNetworkSystem.QueuePacket(uid, component.DestinationFaxAddress, payload);

        component.SendTimeoutRemaining += component.SendTimeout;
        
        _audioSystem.PlayPvs(component.SendSound, uid);
        _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-send"), uid, Filter.Pvs(uid));
    }

    public void Receive(EntityUid uid, string content, string? fromAddress, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var faxName = Loc.GetString("fax-machine-popup-source-unknown");
        if (fromAddress != null && component.KnownFaxes.ContainsKey(fromAddress)) // If message received from unknown for fax address
            faxName = component.KnownFaxes[fromAddress];

        _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-received", ("from", faxName)), uid, Filter.Pvs(uid));
        _appearanceSystem.SetData(uid, FaxMachineVisuals.VisualState, FaxMachineVisualState.Printing);

        component.PrintingQueue.Enqueue(content);
    }

    private void SpawnPaperFromQueue(EntityUid uid, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.PrintingQueue.Count == 0)
            return;

        var content = component.PrintingQueue.Dequeue();
        var printed = EntityManager.SpawnEntity("Paper", Transform(uid).Coordinates);
        if (TryComp<PaperComponent>(printed, out var paper))
            _paperSystem.SetContent(printed, content);
    }
}
