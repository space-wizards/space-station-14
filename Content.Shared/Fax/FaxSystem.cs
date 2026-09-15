using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Cloning;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
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

namespace Content.Shared.Fax;
/// <summary>
/// System for handling the sending of entities through fax machines.
/// TODO: DIRTYFIELDS
/// TODO: ON FAX SHUTDOWN DELETE EVERYTHING IN QUEUE
/// TODO: FIX UI BUGS
/// TODO: POWER STATE HANDLING!!!
/// TODO: STAMP DATA
/// TODO: FIX NUKECODEPAPERSYSTEM AND ADMINFAXEUI
/// </summary>
public abstract partial class FaxSystem : EntitySystem
{
    [Dependency] protected ISharedAdminLogManager AdminLogger = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedCloningSystem _cloningSystem = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] protected EmagSystem Emag = default!;
    [Dependency] private ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private LabelSystem _labelSystem = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private PaperSystem _paperSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] protected SharedAudioSystem AudioSystem = default!;
    [Dependency] private SharedDeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] protected SharedPopupSystem PopupSystem = default!;
    [Dependency] private SharedTransformSystem _xFormSystem = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [Dependency] protected EntityQuery<FaxableObjectComponent> FaxableQuery;
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
            if (Timing.CurTime >= fax.NextInteractTime)
            {
                ProcessInsertion((uid, fax));
                ProcessSendingTimeout((uid, fax));
            }

            UpdateAppearance((uid, fax));
        }
    }

    private void ProcessPrint(Entity<FaxMachineComponent> entity)
    {
        if ((entity.Comp.Functions & FaxFunctions.Printing) == 0 || Timing.CurTime < entity.Comp.PrintTimeEnd)
            return;

        PrintFromQueue(entity);
        if (entity.Comp.PrintingQueue.Count == 0)
        {
            entity.Comp.Functions &= ~FaxFunctions.Printing;
            return;
        }

        StartPrint(entity);
    }

    private void ProcessInsertion(Entity<FaxMachineComponent> entity)
    {
        if ((entity.Comp.Functions & FaxFunctions.Inserting) == 0 || Timing.CurTime < entity.Comp.InsertionEnd)
            return;

        FinishInsert(entity);
        UpdateUserInterface(entity);
    }

    private void ProcessSendingTimeout(Entity<FaxMachineComponent> entity)
    {
        if ((entity.Comp.Functions & FaxFunctions.Sending) == 0)
            return;

        entity.Comp.Functions &= ~FaxFunctions.Sending;
        UpdateUserInterface(entity);
    }

    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<FaxMachineComponent> entity, ref ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(entity.Owner, PaperSlotId, entity.Comp.PaperSlot);
        UpdateAppearance(entity);
    }

    [SubscribeLocalEvent]
    private void OnComponentRemove(Entity<FaxMachineComponent> entity, ref ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(entity.Owner, entity.Comp.PaperSlot);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<FaxMachineComponent> entity, ref MapInitEvent args)
    {
        // Load all faxes on map in cache each other to prevent taking same name by user created fax
        Refresh(entity);
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
        _itemSlotsSystem.SetLock(fax.Owner, fax.Comp.PaperSlot, true);
        UpdateAppearance(fax);
    }

    private void FinishInsert(Entity<FaxMachineComponent> fax)
    {
        fax.Comp.InsertionEnd = TimeSpan.Zero;
        fax.Comp.Functions &= ~FaxFunctions.Inserting;
        _itemSlotsSystem.SetLock(fax.Owner, fax.Comp.PaperSlot, false);
    }

    private void Eject(Entity<FaxMachineComponent> fax)
    {
        FinishInsert(fax);
        _itemSlotsSystem.TryEject(fax, fax.Comp.PaperSlot, null, out _, true);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<FaxMachineComponent> fax, ref PowerChangedEvent args)
    {
        if (args.Powered)
        {
            _itemSlotsSystem.SetLock(fax.Owner, fax.Comp.PaperSlot, false);
            return;
        }

        if ((fax.Comp.Functions & FaxFunctions.Inserting) != 0)
            Eject(fax);

        if ((fax.Comp.Functions & FaxFunctions.Printing) != 0)
            fax.Comp.PrintTimeEnd = TimeSpan.Zero; // TODO: CancelPrint method!

        if ((fax.Comp.Functions & (FaxFunctions.Printing | FaxFunctions.Inserting)) != 0)
            UpdateAppearance(fax);

        _itemSlotsSystem.SetLock(fax.Owner, fax.Comp.PaperSlot, true); // Lock slot when power is off
    }

    [SubscribeLocalEvent]
    private void OnPingPayload(Entity<FaxMachineComponent> ent, ref DeviceNetworkPacketEvent<FaxPingPayload> args)
    {
        var isForSyndie = Emag.CheckFlag(ent.Owner, EmagType.Interaction) && args.Data.IsSyndicate;
        if (!isForSyndie && !ent.Comp.ResponsePings)
            return;

        var pong = new FaxPongPayload
        {
            FaxName = ent.Comp.FaxName,
        };

        _deviceNetworkSystem.SendPacket(ent.Owner, args.SenderAddress, ref pong);
    }

    [SubscribeLocalEvent]
    private void OnPongPayload(Entity<FaxMachineComponent> ent, ref DeviceNetworkPacketEvent<FaxPongPayload> args)
    {
        ent.Comp.KnownFaxes[args.SenderAddress] = args.Data.FaxName;
        UpdateUserInterface(ent);
    }

    [SubscribeLocalEvent]
    private void OnPrintPayload(Entity<FaxMachineComponent> ent, ref DeviceNetworkPacketEvent<FaxPrintout> args)
    {
        Receive((ent, ent), args.Data);
    }

    [SubscribeLocalEvent]
    private void OnToggleInterface(Entity<FaxMachineComponent> entity, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(entity);
    }

    [SubscribeLocalEvent]
    private void OnFileButtonPressed(Entity<FaxMachineComponent> entity, ref FaxFileMessage args)
    {
        args.Label = args.Label?[..Math.Min(args.Label.Length, FaxFileMessageValidation.MaxLabelSize)];
        args.Content = args.Content[..Math.Min(args.Content.Length, FaxFileMessageValidation.MaxContentSize)];
        PrintFile(entity, ref args);
    }

    [SubscribeLocalEvent]
    private void OnCopyButtonPressed(Entity<FaxMachineComponent> fax, ref FaxCopyMessage args)
    {
        Copy(fax, args.Actor);
    }

    [SubscribeLocalEvent]
    private void OnSendButtonPressed(Entity<FaxMachineComponent> entity, ref FaxSendMessage args)
    {
        Send(entity, args.Actor);
    }

    [SubscribeLocalEvent]
    private void OnRefreshButtonPressed(Entity<FaxMachineComponent> entity, ref FaxRefreshMessage args)
    {
        Refresh(entity);
    }

    [SubscribeLocalEvent]
    private void OnDestinationSelected(Entity<FaxMachineComponent> entity, ref FaxDestinationMessage args)
    {
        SetDestination(entity, args.Address);
    }

    protected void UpdateAppearance(Entity<FaxMachineComponent> entity)
    {
        if (FaxableQuery.TryComp(entity.Comp.PaperSlot.Item, out var faxable))
            entity.Comp.InsertingState = faxable.InsertingState;

        _appearanceSystem.SetData(entity, FaxMachineVisuals.VisualState, entity.Comp.Functions);
    }

    // TODO: Delet this
    protected void UpdateUserInterface(Entity<FaxMachineComponent> fax)
    {
        if (_ui.TryGetOpenUi(fax.Owner, FaxUiKey.Key, out var ui))
            ui.Update();
    }

    protected void Faxecute(Entity<FaxMachineComponent> fax, EntityUid? target = null)
    {
        target ??= fax.Comp.PaperSlot.Item;

        if (target == null || !_faxecuteQuery.TryComp(fax, out var faxecute))
            return;

        var damageSpec = faxecute.Damage;
        _damageable.ChangeDamage(target.Value, damageSpec);
        PopupSystem.PopupEntity(Loc.GetString("fax-machine-popup-error", ("target", fax)), fax, PopupType.LargeCaution);
    }

    /// <summary>
    ///     Set fax destination address not checking if he knows it exists
    /// </summary>
    private void SetDestination(Entity<FaxMachineComponent> entity, string destAddress)
    {
        entity.Comp.DestinationFaxAddress = destAddress;
        entity.Comp.DestinationFaxName = entity.Comp.KnownFaxes[destAddress];

        UpdateUserInterface(entity);
    }

    /// <summary>
    ///     Clears current known fax info and make network scan ping
    ///     Adds special data to  payload if it was emagged to identify itself as a Syndicate
    /// </summary>
    private void Refresh(Entity<FaxMachineComponent> entity)
    {
        entity.Comp.DestinationFaxAddress = null;
        entity.Comp.KnownFaxes.Clear();

        var payload = new FaxPingPayload
        {
            IsSyndicate = Emag.CheckFlag(entity, EmagType.Interaction),
        };

        _deviceNetworkSystem.SendPacket(entity.Owner, null, ref payload);
    }

    private void PrintFile(Entity<FaxMachineComponent> entity, ref FaxFileMessage args)
    {
        PrintFile(entity, args.Content, args.OfficePaper, args.Label, args.Actor);
    }

    /// <summary>
    ///     Makes fax print from a file from the computer. A timeout is set after copying,
    ///     which is shared by the send button.
    /// </summary>
    [PublicAPI]
    public void PrintFile(Entity<FaxMachineComponent> fax, string content, bool officePaper, string? label = null, EntityUid? actor = null)
    {
        var prototype = officePaper ? fax.Comp.PrintOfficePaperId : fax.Comp.PrintPaperId;

        var printout = GetPrintout(content, label, prototype: prototype);
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

    public FaxPrintout GetPrintout(string content, string? label = null, string? sender = null, string prototype = PaperId)
    {
        var name = Loc.GetString("fax-machine-printed-paper-name");

        var paper = Spawn(prototype);
        var meta = MetaData(paper);
        FlagPredicted((paper, meta));
        _paperSystem.SetContent(paper, content);
        _metaData.SetEntityName(paper, name, meta);
        _labelSystem.Label(paper, label);

        return new FaxPrintout(GetNetEntity(paper), sender);
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
        return CanInteract(fax, out paper) && _whitelist.IsWhitelistPassOrNull(fax.Comp.Whitelist, paper.Value);
    }

    /// <summary>
    /// Checks if the fax machine buttons are currently interactable.
    /// </summary>
    /// <param name="fax">Fax machine</param>
    /// <param name="paper">Inserted entity if it exists</param>
    /// <returns>True if the fax is not subject to an interaction cooldown, and it has an item inserted, and that the item is fully inserted!</returns>
    [PublicAPI]
    public bool CanInteract(Entity<FaxMachineComponent> fax, [NotNullWhen(true)] out EntityUid? paper)
    {
        paper = null;
        if (fax.Comp.NextInteractTime > Timing.CurTime)
            return false;

        if ((fax.Comp.Functions & FaxFunctions.Inserting) != 0)
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

        if (!_cloningSystem.TryClone(paper.Value, null, fax.Comp.Settings, out var copied))
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
    public void Send(Entity<FaxMachineComponent> fax, EntityUid? user)
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

        if (!_cloningSystem.TryClone(sendEntity.Value, null, fax.Comp.Settings, out var sent))
            return;

        var payload = new FaxPrintout(GetNetEntity(sendEntity.Value));

        _deviceNetworkSystem.SendPacket(fax.Owner, fax.Comp.DestinationFaxAddress, ref payload);

        AdminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(user):actor} " +
            $"sent fax from \"{fax.Comp.FaxName}\" {ToPrettyString(fax):tool} " +
            $"to \"{faxName}\" ({fax.Comp.DestinationFaxAddress}) " +
            $"of {ToPrettyString(sendEntity):subject}: {_paperSystem.GetContent(sent.Value)}");

        Timeout(fax);
        fax.Comp.Functions |= FaxFunctions.Sending;
        AudioSystem.PlayPredicted(fax.Comp.SendSound, fax, user);
        Dirty(fax);
        UpdateUserInterface(fax);
    }

    /// <summary>
    ///     Accepts a new message and adds it to the queue to print
    ///     If has parameter "notifyAdmins" also output a special message to admin chat.
    /// </summary>
    public void Receive(Entity<FaxMachineComponent?> fax, FaxPrintout printout)
    {
        if (!FaxQuery.Resolve(fax, ref fax.Comp))
            return;

        var faxName = printout.SenderName ?? Loc.GetString("fax-machine-popup-source-unknown");

        PopupSystem.PopupEntity(Loc.GetString("fax-machine-popup-received", ("from", faxName)), fax);

        if (fax.Comp.NotifyAdmins)
            NotifyAdmins(faxName);

        // Can't predict this atm...
        EnqueuePrint((fax, fax.Comp), printout);
    }

    private void PauseFax(Entity<FaxMachineComponent> fax)
    {
        // TODO: LOGIC!!!
        // Pause the timers on the fax mascheen!!!
        Dirty(fax);
    }

    private void Timeout(Entity<FaxMachineComponent> fax)
    {
        fax.Comp.NextInteractTime = Timing.CurTime + fax.Comp.InteractionTimeout;
        Dirty(fax);
        // TODO: DIRTYFIELD!!!
    }

    private void EnqueuePrint(Entity<FaxMachineComponent> fax, EntityUid printout, string? sender = null)
    {
        EnqueuePrint(fax, new FaxPrintout(GetNetEntity(printout), sender));
    }

    private void EnqueuePrint(Entity<FaxMachineComponent> fax, FaxPrintout print)
    {
        fax.Comp.PrintingQueue.Enqueue(print);
        StartPrint(fax);
    }

    private void StartPrint(Entity<FaxMachineComponent> fax)
    {
        fax.Comp.PrintTimeEnd = Timing.CurTime + fax.Comp.PrintingTime;
        fax.Comp.Functions |= FaxFunctions.Printing;
        UpdateAppearance(fax);
        Dirty(fax);

        // Can't predict audio because cloning isn't predicted B);
        if (_net.IsServer)
            AudioSystem.PlayPvs(fax.Comp.PrintSound, fax);
    }

    private void PrintFromQueue(Entity<FaxMachineComponent> entity)
    {
        EntityUid printout;
        do // Clear out any bad predicted entities!
        {
            if (entity.Comp.PrintingQueue.Count == 0)
                return;

            printout = GetEntity(entity.Comp.PrintingQueue.Dequeue().Printout);
        } while (printout == EntityUid.Invalid);

        _xFormSystem.SetCoordinates(printout, Transform(entity).Coordinates);

        AdminLogger.Add(LogType.Action, LogImpact.Low, $"\"{entity.Comp.FaxName}\" {ToPrettyString(entity):tool} printed {ToPrettyString(printout):subject}: {_paperSystem.GetContent(printout)}");
        UpdateUserInterface(entity);
    }

    protected abstract void NotifyAdmins(string faxName);
}

[Serializable, NetSerializable]
public enum FaxUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum FaxMachineVisuals : byte
{
    VisualState,
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
