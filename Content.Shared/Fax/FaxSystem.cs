using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Cloning;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Content.Shared.Popups;
using Content.Shared.Fax.Components;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Fax;
/// <summary>
/// System for handling the sending of entities through fax machines.
/// </summary>
public abstract partial class FaxSystem : EntitySystem
{
    [Dependency] protected ISharedAdminLogManager AdminLogger = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedCloningSystem _cloning = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] protected EmagSystem Emag = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private LabelSystem _labelSystem = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private PaperSystem _paperSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] protected SharedAudioSystem AudioSystem = default!;
    [Dependency] private SharedDeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] private SharedTransformSystem _xForm = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [Dependency] private EntityQuery<FaxableObjectComponent> _faxableQuery;
    [Dependency] private EntityQuery<FaxecuteComponent> _faxecuteQuery;
    [Dependency] protected EntityQuery<FaxMachineComponent> FaxQuery;

    private const string PaperSlotId = "Paper";
    public const string PaperId = "Paper";
    public const string OfficePaperId = "PaperOffice";

    // We can't predict power shutting off, so we just let the animation continue if power gets cut out.
    // If that ever changes, have this animation pause cause it would be pretty funny.
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FaxMachineComponent>();
        while (query.MoveNext(out var uid, out var fax))
        {
            // Fax is doing nothing. Do nothing in return.
            if (fax.Functions == FaxFunctions.Idle)
                continue;

            ProcessPrint((uid, fax));
            ProcessInsertion((uid, fax));
            ProcessSendingTimeout((uid, fax));

            UpdateAppearance((uid, fax));
        }
    }

    private void ProcessPrint(Entity<FaxMachineComponent> fax)
    {
        if ((fax.Comp.Functions & FaxFunctions.Printing) == 0 || Timing.CurTime < fax.Comp.PrintTimeEnd)
            return;

        PrintFromQueue(fax);
        if (fax.Comp.PrintingQueue.Count == 0)
        {
            fax.Comp.Functions &= ~FaxFunctions.Printing;
            DirtyField(fax.AsNullable(), nameof(FaxMachineComponent.Functions));
            return;
        }

        StartPrint(fax);
    }

    private void ProcessInsertion(Entity<FaxMachineComponent> fax)
    {
        if ((fax.Comp.Functions & FaxFunctions.Inserting) == 0 || Timing.CurTime < fax.Comp.InsertionEnd)
            return;

        FinishInsert(fax);
        UpdateUserInterface(fax);
    }

    private void ProcessSendingTimeout(Entity<FaxMachineComponent> fax)
    {
        if ((fax.Comp.Functions & FaxFunctions.Processing) == 0 || !CanInteract(fax))
            return;

        fax.Comp.Functions &= ~FaxFunctions.Processing;
        DirtyField(fax.AsNullable(), nameof(FaxMachineComponent.Functions));
        UpdateUserInterface(fax);
    }

    [SubscribeLocalEvent]
    private void AfterAutoHandleState(Entity<FaxMachineComponent> fax, ref AfterAutoHandleStateEvent args)
    {
        // Required for PONG payload :P
        UpdateUserInterface(fax);
    }

    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<FaxMachineComponent> entity, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(entity.Owner, PaperSlotId, entity.Comp.PaperSlot);
        UpdateAppearance(entity);
    }

    [SubscribeLocalEvent]
    private void OnComponentRemove(Entity<FaxMachineComponent> fax, ref ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(fax.Owner, fax.Comp.PaperSlot);
        // TODO: Send this when power goes out!
        var payload = new FaxShutdownPayload();
        _deviceNetwork.SendPacket(fax.Owner, null, ref payload);

        // Don't leave hanging entities in nullspace!
        while (fax.Comp.PrintingQueue.TryDequeue(out var queue))
        {
            Del(GetEntity(queue.Printout));
        }
    }

    [SubscribeLocalEvent(after: [typeof(SharedDeviceNetworkSystem)])]
    private void OnMapInit(Entity<FaxMachineComponent> fax, ref MapInitEvent args)
    {
        Refresh(fax);
    }

    [SubscribeLocalEvent]
    private void OnCanDrag(Entity<FaxableObjectComponent> fax, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnCanDragDrop(Entity<FaxMachineComponent> fax, ref CanDropTargetEvent args)
    {
        if (args.Handled || !_itemSlots.CanInsert(fax.Owner, fax.Comp.PaperSlot, args.Dragged, args.User))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnDragDropped(Entity<FaxMachineComponent> fax, ref DragDropTargetEvent args)
    {
        _itemSlots.TryInsert(fax.Owner, fax.Comp.PaperSlot, args.Dragged, args.User);
    }

    [SubscribeLocalEvent]
    private void OnItemInserted(Entity<FaxMachineComponent> fax, ref EntInsertedIntoContainerMessage args)
    {
        if (Timing.ApplyingState || !fax.Comp.Initialized || args.Container.ID != fax.Comp.PaperSlot.ID)
            return;

        Insert(fax);
        UpdateUserInterface(fax);
    }

    [SubscribeLocalEvent]
    private void OnItemRemoved(Entity<FaxMachineComponent> fax, ref EntRemovedFromContainerMessage args)
    {
        if (Timing.ApplyingState || !fax.Comp.Initialized || args.Container.ID != fax.Comp.PaperSlot.ID)
            return;

        UpdateAppearance(fax);
        UpdateUserInterface(fax);
    }

    private void Insert(Entity<FaxMachineComponent> fax)
    {
        fax.Comp.InsertionEnd = fax.Comp.InsertionTime + Timing.CurTime;
        fax.Comp.Functions |= FaxFunctions.Inserting;

        if (_faxableQuery.TryComp(fax.Comp.PaperSlot.Item, out var faxable) && faxable.InsertingState != null)
        {
            _appearance.SetData(fax, FaxMachineVisuals.Inserting, faxable.InsertingState);
        }
        else
        {
            _appearance.RemoveData(fax, FaxMachineVisuals.Inserting);
        }

        _itemSlots.SetLock(fax.Owner, fax.Comp.PaperSlot, true);
        DirtyField(fax.AsNullable(), nameof(FaxMachineComponent.Functions));
        UpdateAppearance(fax);
    }

    private void FinishInsert(Entity<FaxMachineComponent> fax)
    {
        fax.Comp.InsertionEnd = TimeSpan.Zero;
        fax.Comp.Functions &= ~FaxFunctions.Inserting;
        DirtyField(fax.AsNullable(), nameof(FaxMachineComponent.Functions));
        _itemSlots.SetLock(fax.Owner, fax.Comp.PaperSlot, false);
    }

    private void Eject(Entity<FaxMachineComponent> fax)
    {
        FinishInsert(fax);
        _itemSlots.TryEject(fax, fax.Comp.PaperSlot, null, out _, true);
    }

    // TODO: One day this should pause the print, but at the moment it's not worth the extra complexity :P
    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<FaxMachineComponent> fax, ref PowerChangedEvent args)
    {
        if (args.Powered)
        {
            _itemSlots.SetLock(fax.Owner, fax.Comp.PaperSlot, false);
            // Update devices now that we can send pings again.
            Refresh(fax);
            UpdateAppearance(fax);
            return;
        }

        if ((fax.Comp.Functions & FaxFunctions.Inserting) != 0)
        {
            Eject(fax);
            UpdateAppearance(fax);
        }

         // Lock slot when power is off
         var payload = new FaxShutdownPayload();
         _deviceNetwork.SendPacket(fax.Owner, null, ref payload);
         _itemSlots.SetLock(fax.Owner, fax.Comp.PaperSlot, true);
    }

    [SubscribeLocalEvent]
    private void OnPingPayload(Entity<FaxMachineComponent> fax, ref DeviceNetworkPacketEvent<FaxPingPayload> args)
    {
        var opposingForce = Emag.CheckFlag(fax.Owner, EmagType.Interaction) ^ args.Data.IsSyndicate;
        if (!opposingForce)
            AddDestination(fax, args.SenderAddress, args.Data.FaxName);
        else if (!fax.Comp.ResponsePings)
            return;

        var pong = new FaxPongPayload(fax.Comp.FaxName);

        _deviceNetwork.SendPacket(fax.Owner, args.SenderAddress, ref pong);
    }

    [SubscribeLocalEvent]
    private void OnPongPayload(Entity<FaxMachineComponent> fax, ref DeviceNetworkPacketEvent<FaxPongPayload> args)
    {
        AddDestination(fax, args.SenderAddress, args.Data.FaxName);
    }

    [SubscribeLocalEvent]
    private void OnRemovePayload(Entity<FaxMachineComponent> fax, ref DeviceNetworkPacketEvent<FaxShutdownPayload> args)
    {
        fax.Comp.KnownFaxes.Remove(args.SenderAddress);
        DirtyField(fax.AsNullable(), nameof(FaxMachineComponent.KnownFaxes));

        if (fax.Comp.DestinationFaxAddress != args.SenderAddress)
            return;

        fax.Comp.DestinationFaxAddress = fax.Comp.KnownFaxes.FirstOrNull()?.Key;
        DirtyField(fax.AsNullable(), nameof(FaxMachineComponent.DestinationFaxAddress));
    }

    [SubscribeLocalEvent]
    private void OnPrintPayload(Entity<FaxMachineComponent> fax, ref DeviceNetworkPacketEvent<FaxPayload> args)
    {
        Receive((fax, fax), args.Data);
    }

    [SubscribeLocalEvent]
    private void OnToggleInterface(Entity<FaxMachineComponent> fax, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(fax);
    }

    [SubscribeLocalEvent]
    private void OnFileButtonPressed(Entity<FaxMachineComponent> fax, ref FaxFileMessage args)
    {
        args.Label = args.Label?[..Math.Min(args.Label.Length, FaxFileMessageValidation.MaxLabelSize)];
        args.Content = args.Content[..Math.Min(args.Content.Length, FaxFileMessageValidation.MaxContentSize)];
        PrintFile(fax, ref args);
    }

    [SubscribeLocalEvent]
    private void OnCopyButtonPressed(Entity<FaxMachineComponent> fax, ref FaxCopyMessage args)
    {
        Copy(fax, args.Actor);
    }

    [SubscribeLocalEvent]
    private void OnSendButtonPressed(Entity<FaxMachineComponent> fax, ref FaxSendMessage args)
    {
        Send(fax, args.Actor);
    }

    [SubscribeLocalEvent]
    private void OnRefreshButtonPressed(Entity<FaxMachineComponent> fax, ref FaxRefreshMessage args)
    {
        Refresh(fax);
    }

    [SubscribeLocalEvent]
    private void OnDestinationSelected(Entity<FaxMachineComponent> fax, ref FaxDestinationMessage args)
    {
        SetDestination(fax, args.Address);
    }

    private void UpdateAppearance(Entity<FaxMachineComponent> fax)
    {
        _appearance.SetData(fax, FaxMachineVisuals.VisualState, fax.Comp.Functions);
    }

    protected void UpdateUserInterface(Entity<FaxMachineComponent> fax)
    {
        if (_ui.TryGetOpenUi(fax.Owner, FaxUiKey.Key, out var ui))
            ui.Update();
    }

    private void Faxecute(Entity<FaxMachineComponent> fax, EntityUid? target = null)
    {
        target ??= fax.Comp.PaperSlot.Item;

        if (target == null || !_faxecuteQuery.TryComp(fax, out var faxecute))
            return;

        var damageSpec = faxecute.Damage;
        _damageable.ChangeDamage(target.Value, damageSpec);
        Popup.PopupEntity(Loc.GetString("fax-machine-popup-error", ("target", fax)), fax, PopupType.LargeCaution);
    }

    private void AddDestination(Entity<FaxMachineComponent> fax, string address, string name)
    {
        fax.Comp.KnownFaxes[address] = name;
        DirtyField(fax.AsNullable(), nameof(FaxMachineComponent.KnownFaxes));

        if (fax.Comp.DestinationFaxAddress == null)
            SetDestination(fax, address);
    }

    /// <summary>
    ///     Set fax destination address not checking if he knows it exists
    /// </summary>
    private void SetDestination(Entity<FaxMachineComponent> fax, string destAddress)
    {
        fax.Comp.DestinationFaxAddress = destAddress;
        DirtyField(fax.AsNullable(), nameof(FaxMachineComponent.DestinationFaxAddress));
        UpdateUserInterface(fax);
    }

    /// <summary>
    ///     Clears current known fax info and make network scan ping
    ///     Adds special data to  payload if it was emagged to identify itself as a Syndicate
    /// </summary>
    protected void Refresh(Entity<FaxMachineComponent> fax)
    {
        // Device network not predicted...
        if (_net.IsClient)
            return;

        fax.Comp.DestinationFaxAddress = null;
        fax.Comp.KnownFaxes.Clear();

        var payload = new FaxPingPayload(fax.Comp.FaxName, Emag.CheckFlag(fax, EmagType.Interaction));

        _deviceNetwork.SendPacket(fax.Owner, null, ref payload);
        DirtyField(fax.AsNullable(), nameof(FaxMachineComponent.KnownFaxes));
    }

    private void PrintFile(Entity<FaxMachineComponent> fax, ref FaxFileMessage args)
    {
        PrintFile(fax, args.Content, args.OfficePaper, args.Label, args.Actor);
    }

    /// <summary>
    ///     Makes fax print from a file from the computer. A timeout is set after copying,
    ///     which is shared by the send button.
    /// </summary>
    private void PrintFile(Entity<FaxMachineComponent> fax, string content, bool officePaper, string? label = null, EntityUid? actor = null)
    {
        var prototype = officePaper ? fax.Comp.PrintOfficePaperId : fax.Comp.PrintPaperId;

        var printout = GetPayload(content, label, prototype: prototype);
        EnqueuePrint(fax, printout);
        Timeout(fax);
        UpdateUserInterface(fax);

        // Unfortunately, since a paper entity does not yet exist, we have to emulate what LabelSystem will do.
        AdminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(actor):actor} " +
            $"added print job to \"{fax.Comp.FaxName}\" {ToPrettyString(fax):tool} " +
            $"of {ToPrettyString(printout.Printout)}: {content}");
    }

    /// <summary>
    /// Returns a FaxPayload which can be sent through the fax network based on a specific FaxPrintout
    /// This WILL spawn an entity in nullspace so if you don't send it to a fax machine, it WILL leak.
    /// Simply write perfect code or perish.
    /// </summary>
    /// <param name="printout">Fax Printout we are sending</param>
    /// <param name="prototype">Prototype of the entity being created</param>
    /// <returns>The Payload we will be sending to a fax machine.</returns>
    [PublicAPI]
    public FaxPayload GetPayload(FaxPrintout printout, string prototype = PaperId)
    {
        var paper = Spawn(prototype);
        var meta = MetaData(paper);
        FlagPredicted((paper, meta));
        _metaData.SetEntityName(paper, printout.Name, meta);
        _labelSystem.Label(paper, printout.Label);

        if (!TryComp<PaperComponent>(paper, out var paperComp))
            return new FaxPayload(GetNetEntity(paper), printout.Sender);

        _paperSystem.SetContent((paper, paperComp), printout.Content);
        paperComp.EditingDisabled = printout.Locked;

        if (printout.StampState == null)
            return new FaxPayload(GetNetEntity(paper), printout.Sender);

        foreach (var stamp in printout.StampedBy)
        {
            _paperSystem.TryStamp((paper, paperComp), stamp, printout.StampState);
        }

        return new FaxPayload(GetNetEntity(paper), printout.Sender);
    }

    private FaxPayload GetPayload(string content, string? label = null, string? sender = null, string prototype = PaperId)
    {
        var name = Loc.GetString("fax-machine-printed-paper-name");
        var paper = Spawn(prototype);
        var meta = MetaData(paper);
        FlagPredicted((paper, meta));
        _paperSystem.SetContent(paper, content);
        _metaData.SetEntityName(paper, name, meta);
        _labelSystem.Label(paper, label);

        return new FaxPayload(GetNetEntity(paper), sender);
    }

    /// <summary>
    /// Checks if the entity inserted into this fax can be copied or sent.
    /// </summary>
    /// <param name="fax">Fax machine we're checking.</param>
    /// <param name="paper">Resolved entity we can send</param>
    /// <returns>Returns true if there is an entity inserted, and this machine can send it!</returns>
    [PublicAPI]
    public bool CanFax(Entity<FaxMachineComponent> fax, [NotNullWhen(true)] out EntityUid? paper)
    {
        paper = null;
        if (!CanInteract(fax))
            return false;

        return PaperInserted(fax, out paper) && _whitelist.IsWhitelistPassOrNull(fax.Comp.Whitelist, paper.Value);
    }

    /// <summary>
    /// Checks if a fax machine is subject to an interaction cooldown.
    /// </summary>
    [PublicAPI]
    public bool CanInteract(Entity<FaxMachineComponent> fax)
    {
        return Timing.CurTime >= fax.Comp.NextInteractTime;
    }

    /// <summary>
    /// Checks if the fax machine buttons are currently interactable.
    /// </summary>
    /// <param name="fax">Fax machine</param>
    /// <param name="paper">Inserted entity if it exists</param>
    /// <returns>True if the fax is not subject to an interaction cooldown, and it has an item inserted, and that the item is fully inserted!</returns>
    [PublicAPI]
    public bool PaperInserted(Entity<FaxMachineComponent> fax, [NotNullWhen(true)] out EntityUid? paper)
    {
        paper = null;
        if ((fax.Comp.Functions & FaxFunctions.Inserting) == FaxFunctions.Inserting)
            return false;

        paper = fax.Comp.PaperSlot.Item;
        return paper != null;
    }

    /// <summary>
    ///     Copies the paper in the fax. A timeout is set after copying,
    ///     which is shared by the send button.
    /// </summary>
    private void Copy(Entity<FaxMachineComponent> fax, EntityUid? actor)
    {
        if (!CanFax(fax, out var paper))
        {
            Faxecute(fax);
            return;
        }

        Timeout(fax);

        if (!_cloning.TryClone(paper.Value, null, fax.Comp.Settings, out var copied))
            return;

        EnqueuePrint(fax, copied.Value);
        UpdateUserInterface(fax);

        AdminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(actor):actor} " +
            $"added copy job to \"{fax.Comp.FaxName}\" {ToPrettyString(fax):tool} " +
            $"of {ToPrettyString(fax):subject}: {_paperSystem.GetContent(copied.Value)}");
    }

    /// <summary>
    ///     Sends message to addressee if paper is set and a known fax is selected
    ///     A timeout is set after sending, which is shared by the copy button.
    /// </summary>
    private void Send(Entity<FaxMachineComponent> fax, EntityUid? user)
    {
        if (!CanFax(fax, out var sendEntity))
        {
            Faxecute(fax);
            return;
        }

        if (fax.Comp.DestinationFaxAddress == null)
            return;

        if (!fax.Comp.KnownFaxes.TryGetValue(fax.Comp.DestinationFaxAddress, out var faxName))
            return;

        if (!_cloning.TryClone(sendEntity.Value, null, fax.Comp.Settings, out var sent))
            return;

        var payload = new FaxPayload(GetNetEntity(sent.Value));

        _deviceNetwork.SendPacket(fax.Owner, fax.Comp.DestinationFaxAddress, ref payload);

        AdminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(user):actor} " +
            $"sent fax from \"{fax.Comp.FaxName}\" {ToPrettyString(fax):tool} " +
            $"to \"{faxName}\" ({fax.Comp.DestinationFaxAddress}) " +
            $"of {ToPrettyString(sent):subject}: {_paperSystem.GetContent(sent.Value)}");

        Timeout(fax);
        AudioSystem.PlayPredicted(fax.Comp.SendSound, fax, user);
        UpdateUserInterface(fax);
    }

    /// <summary>
    ///     Accepts a new message and adds it to the queue to print
    ///     If has parameter "notifyAdmins" also output a special message to admin chat.
    /// </summary>
    [PublicAPI]
    public void Receive(Entity<FaxMachineComponent?> fax, FaxPayload payload)
    {
        if (!FaxQuery.Resolve(fax, ref fax.Comp))
            return;

        var faxName = payload.SenderName ?? Loc.GetString("fax-machine-popup-source-unknown");

        Popup.PopupEntity(Loc.GetString("fax-machine-popup-received", ("from", faxName)), fax);

        if (fax.Comp.NotifyAdmins)
            NotifyAdmins(faxName);

        // Can't predict this atm...
        EnqueuePrint((fax, fax.Comp), payload);
    }

    private void Timeout(Entity<FaxMachineComponent> fax)
    {
        fax.Comp.NextInteractTime = Timing.CurTime + fax.Comp.InteractionTimeout;
        fax.Comp.Functions |= FaxFunctions.Processing;
        DirtyFields(fax.AsNullable(), null, nameof(FaxMachineComponent.NextInteractTime), nameof(FaxMachineComponent.Functions));
    }

    private void EnqueuePrint(Entity<FaxMachineComponent> fax, EntityUid printout, string? sender = null)
    {
        EnqueuePrint(fax, new FaxPayload(GetNetEntity(printout), sender));
    }

    private void EnqueuePrint(Entity<FaxMachineComponent> fax, FaxPayload print)
    {
        fax.Comp.PrintingQueue.Enqueue(print);
        DirtyField(fax.AsNullable(), nameof(FaxMachineComponent.PrintingQueue));
        StartPrint(fax);
    }

    private void StartPrint(Entity<FaxMachineComponent> fax)
    {
        fax.Comp.PrintTimeEnd = Timing.CurTime + fax.Comp.PrintingTime;
        fax.Comp.Functions |= FaxFunctions.Printing;
        UpdateAppearance(fax);
        DirtyFields(fax.AsNullable(), null, nameof(FaxMachineComponent.PrintTimeEnd), nameof(FaxMachineComponent.Functions));

        // Can't predict audio because cloning isn't predicted B);
        if (_net.IsServer)
            AudioSystem.PlayPvs(fax.Comp.PrintSound, fax);
    }

    private void PrintFromQueue(Entity<FaxMachineComponent> fax)
    {
        EntityUid printout;
        do
        {
            if (!fax.Comp.PrintingQueue.TryDequeue(out var queued))
                return;

            printout = GetEntity(queued.Printout);
        } while (printout == EntityUid.Invalid); // Error handling for badly predicted or deleted entities!

        _xForm.SetCoordinates(printout, Transform(fax).Coordinates);

        AdminLogger.Add(LogType.Action, LogImpact.Low, $"\"{fax.Comp.FaxName}\" {ToPrettyString(fax):tool} printed {ToPrettyString(printout):subject}: {_paperSystem.GetContent(printout)}");
        DirtyField(fax.AsNullable(), nameof(FaxMachineComponent.PrintingQueue));
        UpdateUserInterface(fax);
    }

    protected abstract void NotifyAdmins(string faxName);
}

[Serializable, NetSerializable]
public enum FaxUiKey : byte
{
    Key
}

[DataDefinition]
public readonly partial record struct FaxPrintout(string Content, string Name)
{
    [DataField(required: true)]
    public readonly string Content = Content;

    [DataField(required: true)]
    public readonly string Name = Name;

    [DataField]
    public readonly string? Label;

    [DataField]
    public readonly string? Sender;

    [DataField]
    public readonly string? StampState;

    [DataField]
    public readonly List<StampDisplayInfo> StampedBy = new ();

    [DataField]
    public readonly bool Locked;

    public FaxPrintout(string Content, string Name, string? Label, string? Sender) : this(Content, Name)
    {
        this.Label = Label;
        this.Sender = Sender;
    }

    public FaxPrintout(string Content, string Name, string? Label, string? Sender, string StampState, List<StampDisplayInfo> StampedBy, bool Locked = true)
        : this(Content, Name, Label, Sender)
    {
        this.StampState = StampState;
        this.StampedBy = StampedBy;
        this.Locked = Locked;
    }
}

[Serializable, NetSerializable]
public sealed class FaxFileMessage : BoundUserInterfaceMessage
{
    public string? Label;
    public string Content;
    public bool OfficePaper;

    public FaxFileMessage(string? label, string content, bool officePaper)
    {
        Label = label;
        Content = content;
        OfficePaper = officePaper;
    }
}

public static class FaxFileMessageValidation
{
    public const int MaxLabelSize = HandLabelerComponent.MaxLabelLength; // parity with Content.Server.Labels.Components.HandLabelerComponent.MaxLabelChars
    public const int MaxContentSize = 10000;
}

[Serializable, NetSerializable]
public sealed class FaxCopyMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class FaxSendMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class FaxRefreshMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class FaxDestinationMessage : BoundUserInterfaceMessage
{
    public string Address { get; }
    public FaxDestinationMessage(string address)
    {
        Address = address;
    }
}
