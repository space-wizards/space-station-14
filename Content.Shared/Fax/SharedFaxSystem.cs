using Content.Shared.Administration;
using Content.Shared.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.Fax.Components;
using Content.Shared.Fax.Systems;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.NameModifier.Components;
using Content.Shared.Paper;
using Content.Shared.Power;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Network;

namespace Content.Shared.Fax;

public abstract class SharedFaxSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly LabelSystem _labelSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedQuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly FaxecuteSystem _faxecute = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly INetManager _net = default!;

    protected static readonly ProtoId<ToolQualityPrototype> ScrewingQuality = "Screwing";

    protected const string PaperSlotId = "Paper";

    public override void Initialize()
    {
        base.Initialize();

        // Hooks
        SubscribeLocalEvent<FaxMachineComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<FaxMachineComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<FaxMachineComponent, EntityUnpausedEvent>(OnEntityUnpausedEvent);

        SubscribeLocalEvent<FaxMachineComponent, EntInsertedIntoContainerMessage>(OnItemSlotChangedAny);
        SubscribeLocalEvent<FaxMachineComponent, EntRemovedFromContainerMessage>(OnItemSlotChangedAny);

        SubscribeLocalEvent<FaxMachineComponent, PowerChangedEvent>(OnPowerChanged);

        // Interaction
        SubscribeLocalEvent<FaxMachineComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<FaxMachineComponent, GotEmaggedEvent>(OnEmagged);

        // UI
        SubscribeLocalEvent<FaxMachineComponent, FaxFileMessage>(OnFileButtonPressed);
        SubscribeLocalEvent<FaxMachineComponent, FaxCopyMessage>(OnCopyButtonPressed);
        SubscribeLocalEvent<FaxMachineComponent, FaxSendMessage>(OnSendButtonPressed);
        SubscribeLocalEvent<FaxMachineComponent, FaxDestinationMessage>(OnDestinationSelected);

        // Other
        SubscribeLocalEvent<FaxMachineComponent, QueuePrintoutEvent>(OnQueuePrintoutEvent);
        SubscribeLocalEvent<FaxMachineComponent, PrintedEvent>(OnPrintedEvent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FaxMachineComponent>();
        while (query.MoveNext(out var uid, out var fax))
        {
            Entity<FaxMachineComponent> ent = (uid, fax);
            var dirty = false;

            var actions = Enum.GetValues<FaxActions>();
            foreach (var action in actions)
            {
                if (!ent.Comp.IsActionActive[action])
                    continue;

                if (ent.Comp.ReadyTimes.GetValueOrDefault(action) > _gameTiming.CurTime)
                    continue;

                ent.Comp.IsActionActive[action] = false;
                ent.Comp.ActionSounds[action] = null;

                switch (action)
                {
                    case FaxActions.Send:
                        break;
                    case FaxActions.Insert:
                        _itemSlotsSystem.SetLock(ent.Owner, ent.Comp.PaperSlot, false);
                        break;
                    case FaxActions.Print:
                        SpawnPaperFromQueue(ent);
                        TryPrint(ent);
                        break;
                }

                dirty = true;
            }

            if (!dirty)
                continue;

            UpdateUserInterface(ent);
            UpdateAppearance(ent);
            Dirty(ent);
        }
    }

    private void OnComponentInit(Entity<FaxMachineComponent> ent, ref ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(ent.Owner, PaperSlotId, ent.Comp.PaperSlot);
        UpdateAppearance(ent);
        UpdateUserInterface(ent);
    }

    private void OnComponentRemove(Entity<FaxMachineComponent> ent, ref ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(ent.Owner, ent.Comp.PaperSlot);
    }

    private void OnEntityUnpausedEvent(Entity<FaxMachineComponent> ent, ref EntityUnpausedEvent args)
    {
        foreach (var action in ent.Comp.ReadyTimes.Keys)
        {
            ent.Comp.ReadyTimes[action] += args.PausedTime;
        }
        Dirty(ent);
    }

    private void OnItemSlotChangedAny<T>(Entity<FaxMachineComponent> ent, ref T args) where T: ContainerModifiedMessage
    {
        OnItemSlotChanged(ent, args);
    }
    private void OnItemSlotChanged(Entity<FaxMachineComponent> ent, ContainerModifiedMessage args)
    {
        if (!ent.Comp.Initialized)
            return;

        if (args.Container.ID != ent.Comp.PaperSlot.ID)
            return;

        var isPaperInserted = ent.Comp.PaperSlot.Item.HasValue;
        if (isPaperInserted)
        {
            DoAction(ent, FaxActions.Insert);
            _itemSlotsSystem.SetLock(ent.Owner, ent.Comp.PaperSlot, true);

        }
        else
            ent.Comp.IsActionActive[FaxActions.Insert] = false;

        UpdateAppearance(ent);
        UpdateUserInterface(ent);
        Dirty(ent);
    }

    private void OnPowerChanged(Entity<FaxMachineComponent> ent, ref PowerChangedEvent args)
    {

        if (args.Powered)
        {
            _itemSlotsSystem.SetLock(ent.Owner, ent.Comp.PaperSlot, false);
            TryPrint(ent);
            return;
        }

        _itemSlotsSystem.SetLock(ent.Owner, ent.Comp.PaperSlot, true);

        if (ent.Comp.IsActionActive[FaxActions.Insert])
        {
            ent.Comp.IsActionActive[FaxActions.Insert] = false;
            _itemSlotsSystem.TryEject(ent.Owner, ent.Comp.PaperSlot, null, out var _, true);
        }

        if (ent.Comp.IsActionActive[FaxActions.Print])
        {
            ent.Comp.IsActionActive[FaxActions.Print] = false;
        }

        foreach (var sound in ent.Comp.ActionSounds)
        {
            _audioSystem.Stop(sound.Value);
        }
        ent.Comp.ActionSounds.Clear();

        UpdateAppearance(ent);
        UpdateUserInterface(ent);
        Dirty(ent);
    }

    private void OnInteractUsing(Entity<FaxMachineComponent> ent, ref InteractUsingEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (args.Handled ||
            !TryComp<ActorComponent>(args.User, out var actor) ||
            !_toolSystem.HasQuality(args.Used, ScrewingQuality)) // Screwing because Pulsing already used by device linking
            return;

        var user = args.User;

        _quickDialog.OpenDialog(actor.PlayerSession,
            Loc.GetString("fax-machine-dialog-rename"),
            Loc.GetString("fax-machine-dialog-field-name"),
            (string newName) =>
        {
            if (!_gameTiming.IsFirstTimePredicted)
                return;

            if (ent.Comp.FaxName == newName)
                return;

            if (newName.Length > 20)
            {
                _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-name-long"), ent.Owner);
                return;
            }

            if (ent.Comp.KnownFaxes.ContainsValue(newName) && !_emag.CheckFlag(ent.Owner, EmagType.Interaction)) // Allow existing names if emagged for fun
            {
                _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-name-exist"), ent.Owner);
                return;
            }

            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(user):user} renamed {ToPrettyString(ent.Owner):tool} from \"{ent.Comp.FaxName}\" to \"{newName}\"");
            ent.Comp.FaxName = newName;
            _popupSystem.PopupPredicted(Loc.GetString("fax-machine-popup-name-set"), ent.Owner, user);
            UpdateUserInterface(ent);

            Dirty(ent);
        });

        args.Handled = true;
    }

    private void OnEmagged(EntityUid uid, FaxMachineComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    private void OnFileButtonPressed(Entity<FaxMachineComponent> ent, ref FaxFileMessage args)
    {
        args.Label = args.Label?[..Math.Min(args.Label.Length, FaxFileMessageValidation.MaxLabelSize)];
        args.Content = args.Content[..Math.Min(args.Content.Length, FaxFileMessageValidation.MaxContentSize)];
        PrintFile(ent, args);
    }

    private void OnCopyButtonPressed(Entity<FaxMachineComponent> ent, ref FaxCopyMessage args)
    {
        if (HasComp<MobStateComponent>(ent.Comp.PaperSlot.Item))
            _faxecute.Faxecute(ent); // when button pressed it will hurt the mob.
        else
            Copy(ent, args);
    }

    private void OnSendButtonPressed(Entity<FaxMachineComponent> ent, ref FaxSendMessage args)
    {
        if (HasComp<MobStateComponent>(ent.Comp.PaperSlot.Item))
            _faxecute.Faxecute(ent); // when button pressed it will hurt the mob.
        else
            Send(ent, ref args);
    }

    private void OnDestinationSelected(Entity<FaxMachineComponent> ent, ref FaxDestinationMessage args)
    {
        SetDestination(ent, args.Address);
    }

    protected void UpdateAppearance(Entity<FaxMachineComponent> ent)
    {
        if (TryComp<FaxableObjectComponent>(ent.Comp.PaperSlot.Item, out var faxable))
        {
            ent.Comp.InsertingState = faxable.InsertingState;
            Dirty(ent);
        }

        if (ent.Comp.IsActionActive[FaxActions.Insert])
            _appearanceSystem.SetData(ent.Owner, FaxMachineVisuals.VisualState, FaxMachineVisualState.Inserting);
        else if (ent.Comp.IsActionActive[FaxActions.Print])
            _appearanceSystem.SetData(ent.Owner, FaxMachineVisuals.VisualState, FaxMachineVisualState.Printing);
        else
            _appearanceSystem.SetData(ent.Owner, FaxMachineVisuals.VisualState, FaxMachineVisualState.Normal);
    }

    protected void UpdateUserInterface(Entity<FaxMachineComponent> ent)
    {
        var isPaperInserted = ent.Comp.PaperSlot.Item != null;
        var canSend = isPaperInserted &&
                      ent.Comp.DestinationFaxAddress != null &&
                      !ent.Comp.IsActionActive[FaxActions.Send] &&
                      !ent.Comp.IsActionActive[FaxActions.Print];
        var canCopy = isPaperInserted &&
                      !ent.Comp.IsActionActive[FaxActions.Send] &&
                      !ent.Comp.IsActionActive[FaxActions.Print];
        var state = new FaxUiState(ent.Comp.FaxName, ent.Comp.KnownFaxes, canSend, canCopy, isPaperInserted, ent.Comp.DestinationFaxAddress);
        _userInterface.SetUiState(ent.Owner, FaxUiKey.Key, state);
    }

    /// <summary>
    ///     Set fax destination address not checking if he knows it exists
    /// </summary>
    public void SetDestination(Entity<FaxMachineComponent> ent, string destAddress)
    {
        ent.Comp.DestinationFaxAddress = destAddress;

        UpdateUserInterface(ent);
        Dirty(ent);
    }

    /// <summary>
    ///     Makes fax print from a file from the computer. A timeout is set after copying,
    ///     which is shared by the send button.
    /// </summary>
    public void PrintFile(Entity<FaxMachineComponent > ent, FaxFileMessage args)
    {
        var prototype = args.OfficePaper ? ent.Comp.PrintOfficePaperId : ent.Comp.PrintPaperId;

        var name = Loc.GetString("fax-machine-printed-paper-name");

        var printout = new FaxPrintout(args.Content, name, args.Label, prototype);

        RaiseLocalEvent(ent, new QueuePrintoutEvent(printout));

        // Unfortunately, since a paper entity does not yet exist, we have to emulate what LabelSystem will do.
        var nameWithLabel = (args.Label is { } label) ? $"{name} ({label})" : name;
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} " +
            $"added print job to \"{ent.Comp.FaxName}\" {ToPrettyString(ent.Owner):tool} " +
            $"of {nameWithLabel}: {args.Content}");

        UpdateUserInterface(ent);
    }

    /// <summary>
    ///     Copies the paper in the fax. A timeout is set after copying,
    ///     which is shared by the send button.
    /// </summary>
    protected void Copy(Entity<FaxMachineComponent> ent, FaxCopyMessage args)
    {
        var sendEntity = ent.Comp.PaperSlot.Item;
        if (sendEntity == null)
            return;

        if (!TryComp(sendEntity, out MetaDataComponent? metadata) ||
            !TryComp<PaperComponent>(sendEntity, out var paper))
            return;

        TryComp<LabelComponent>(sendEntity, out var labelComponent);
        TryComp<NameModifierComponent>(sendEntity, out var nameMod);

        // TODO: See comment in 'Send()' about not being able to copy whole entities
        var printout = new FaxPrintout(paper.Content,
                                       nameMod?.BaseName ?? metadata.EntityName,
                                       labelComponent?.CurrentLabel,
                                       metadata.EntityPrototype?.ID ?? ent.Comp.PrintPaperId,
                                       paper.StampState,
                                       paper.StampedBy,
                                       paper.EditingDisabled);

        RaiseLocalEvent(ent, new QueuePrintoutEvent(printout));
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} " +
            $"added copy job to \"{ent.Comp.FaxName}\" {ToPrettyString(ent.Owner):tool} " +
            $"of {ToPrettyString(sendEntity):subject}: {printout.Content}");

        UpdateUserInterface(ent);
        UpdateAppearance(ent);
    }

    /// <summary>
    ///     Sends message to addressee if paper is set and a known fax is selected
    ///     A timeout is set after sending, which is shared by the copy button.
    /// </summary>
    protected virtual void Send(Entity<FaxMachineComponent> ent, ref FaxSendMessage args)
    {
        DoAction(ent, FaxActions.Send);

        UpdateUserInterface(ent);
        UpdateAppearance(ent);
        Dirty(ent);
    }

    /// <summary>
    ///     Accepts a new message and adds it to the queue to print
    ///     If has parameter "notifyAdmins" also output a special message to admin chat.
    /// </summary>
    public void Receive(Entity<FaxMachineComponent?> ent, FaxPrintout printout, string? fromAddress = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var faxName = Loc.GetString("fax-machine-popup-source-unknown");
        if (fromAddress != null && ent.Comp.KnownFaxes.TryGetValue(fromAddress, out var fax)) // If message received from unknown fax address
            faxName = fax;

        _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-received", ("from", faxName)), ent.Owner);
        _appearanceSystem.SetData(ent.Owner, FaxMachineVisuals.VisualState, FaxMachineVisualState.Printing);

        if (ent.Comp.NotifyAdmins)
            NotifyAdmins(faxName);

        RaiseLocalEvent(ent, new QueuePrintoutEvent(printout));
    }

    protected void SpawnPaperFromQueue(Entity<FaxMachineComponent> ent)
    {
        if (ent.Comp.PrintingQueue.Count == 0)
            return;

        var printout = ent.Comp.PrintingQueue.Dequeue();

        var entityToSpawn = printout.PrototypeId.Length == 0 ? ent.Comp.PrintPaperId.ToString() : printout.PrototypeId;

        if (_net.IsServer)
        {
            var printed = Spawn(entityToSpawn, Transform(ent.Owner).Coordinates);
            if (TryComp<PaperComponent>(printed, out var paper))
            {
                _paperSystem.SetContent((printed, paper), printout.Content);

                // Apply stamps
                if (printout.StampState != null)
                {
                    foreach (var stamp in printout.StampedBy)
                    {
                        _paperSystem.TryStamp((printed, paper), stamp, printout.StampState);
                    }
                }

                paper.EditingDisabled = printout.Locked;
            }

            _metaData.SetEntityName(printed, printout.Name);

            if (printout.Label is { } label)
            {
                _labelSystem.Label(printed, label);
            }

            RaiseLocalEvent(ent, new PrintedEvent(printed));
        }

        if (ent.Comp.PrintingQueue.Count == 0)
            RaiseLocalEvent(ent, new PrintedAllEvent());

        Dirty(ent);
    }

    private void OnQueuePrintoutEvent(Entity<FaxMachineComponent> ent, ref QueuePrintoutEvent args)
    {
        ent.Comp.PrintingQueue.Enqueue(args.Printout);

        TryPrint(ent);

        Dirty(ent);
    }

    private bool TryPrint(Entity<FaxMachineComponent> ent)
    {
        if (ent.Comp.PrintingQueue.Count == 0)
            return false;

        if (ent.Comp.IsActionActive[FaxActions.Print])
            return false;

        DoAction(ent, FaxActions.Print);

        RaiseLocalEvent(ent, new PrintEvent(ent.Comp.PrintingQueue.Peek()));

        UpdateUserInterface(ent);
        UpdateAppearance(ent);
        Dirty(ent);
        return true;
    }

    protected virtual void OnPrintedEvent(Entity<FaxMachineComponent> ent, ref PrintedEvent args)
    {
        TryPrint(ent);
    }

    protected virtual void NotifyAdmins(string faxName) { }

    /// <summary>
    /// Helper function to set things
    /// </summary>
    /// <remarks>
    /// Always call <see cref="EntitySystem.Dirty()"/> after this
    /// </remarks>
    private void DoAction(Entity<FaxMachineComponent> ent, FaxActions action)
    {
        ent.Comp.ReadyTimes[action] = _gameTiming.CurTime + ent.Comp.ActionTimeout[action];
        ent.Comp.IsActionActive[action] = true;

        if (ent.Comp.ActionSoundsSpecifiers.TryGetValue(action, out var specifier))
        {
            var sound = _audioSystem.PlayPredicted(specifier, ent.Owner, null);
            if (sound != null)
                ent.Comp.ActionSounds[action] = sound.Value.Entity;
        }
    }
}

/// <summary>
/// Event raised when new printout gets added to the printing queue.
/// </summary>
public record struct QueuePrintoutEvent(FaxPrintout Printout);

/// <summary>
/// Event raised when printing process begins. This event is not a guarantee of mail getting printed.
/// </summary>
public record struct PrintEvent(FaxPrintout Printout);

/// <summary>
/// Event raised after printout has been printed.
/// </summary>
public record struct PrintedEvent(EntityUid Printed);

/// <summary>
/// Event raised after printing queue empties.
/// </summary>
public record struct PrintedAllEvent();
