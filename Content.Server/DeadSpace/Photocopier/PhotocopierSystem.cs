// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Paper;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Emag.Systems;
using Content.Shared.DeadSpace.Photocopier;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.ContentPack;
using Robust.Shared.Audio;
using Content.Server.Station.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared.Hands.Components;
using Content.Shared.Database;
using Content.Server.GameTicking;
using Content.Shared.UserInterface;
using Content.Shared.Power;

namespace Content.Server.DeadSpace.Photocopier;

public sealed class PhotocopierSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private const string PaperSlotId = "Paper";

    public override void Initialize()
    {
        base.Initialize();

        // Hooks
        SubscribeLocalEvent<PhotocopierComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PhotocopierComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PhotocopierComponent, ComponentRemove>(OnComponentRemove);

        SubscribeLocalEvent<PhotocopierComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<PhotocopierComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<PhotocopierComponent, PowerChangedEvent>(OnPowerChanged);

        // Interaction
        SubscribeLocalEvent<PhotocopierComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<PhotocopierComponent, GetVerbsEvent<Verb>>(OnGetVerbs);

        // UI
        SubscribeLocalEvent<PhotocopierComponent, AfterActivatableUIOpenEvent>(OnToggleInterface);
        SubscribeLocalEvent<PhotocopierComponent, PhotocopierChoseFormMessage>(OnFormButtonPressed);
        SubscribeLocalEvent<PhotocopierComponent, PhotocopierCopyModeMessage>(OnCopyModeButtonPressed);
        SubscribeLocalEvent<PhotocopierComponent, PhotocopierPrintModeMessage>(OnPrintModeButtonPressed);
        SubscribeLocalEvent<PhotocopierComponent, PhotocopierPrintMessage>(OnPrintButtonPressed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PhotocopierComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var fax, out var receiver))
        {
            if (!receiver.Powered)
                continue;

            ProcessPrintingAnimation(uid, frameTime, fax);
            ProcessScanningAnimation(uid, frameTime, fax);
            ProcessUseTimeout(uid, frameTime, fax);
        }
    }

    private void ProcessPrintingAnimation(EntityUid uid, float frameTime, PhotocopierComponent comp)
    {
        if (comp.ScanningTimeRemaining > 0)
            return;

        if (comp.PrintingTimeRemaining > 0)
        {
            comp.PrintingTimeRemaining -= frameTime;
            UpdateAppearance(uid, comp);

            var isAnimationEnd = comp.PrintingTimeRemaining <= 0;
            if (isAnimationEnd)
            {
                SpawnPaperFromQueue(uid, comp);
                UpdateUserInterface(uid, comp);
            }

            return;
        }

        if (comp.PrintingQueue.Count > 0)
        {
            comp.PrintingTimeRemaining = comp.PrintingTime;
            _audioSystem.PlayPvs(comp.PrintSound, uid, AudioParams.Default.WithVolume(-4f));
        }
    }

    private void ProcessScanningAnimation(EntityUid uid, float frameTime, PhotocopierComponent comp)
    {
        if (comp.ScanningTimeRemaining <= 0)
            return;

        comp.ScanningTimeRemaining -= frameTime;
        UpdateAppearance(uid, comp);

        var isAnimationEnd = comp.ScanningTimeRemaining <= 0;
        if (isAnimationEnd)
        {
            _itemSlotsSystem.SetLock(uid, comp.PaperSlot, false);
            UpdateUserInterface(uid, comp);
        }
    }

    private void ProcessUseTimeout(EntityUid uid, float frameTime, PhotocopierComponent comp)
    {
        if (comp.UseTimeoutRemaining > 0)
        {
            comp.UseTimeoutRemaining -= frameTime;

            if (comp.UseTimeoutRemaining <= 0)
                UpdateUserInterface(uid, comp);
        }
    }

    private void OnComponentInit(EntityUid uid, PhotocopierComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, PaperSlotId, component.PaperSlot);
        UpdateAppearance(uid, component);
    }

    private void OnComponentRemove(EntityUid uid, PhotocopierComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.PaperSlot);
    }

    private void OnMapInit(EntityUid uid, PhotocopierComponent component, MapInitEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnItemSlotChanged(EntityUid uid, PhotocopierComponent component, ContainerModifiedMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.PaperSlot.ID)
            return;

        UpdateUserInterface(uid, component);
    }

    private void OnPowerChanged(EntityUid uid, PhotocopierComponent component, ref PowerChangedEvent args)
    {
        var isInsertInterrupted = !args.Powered && component.ScanningTimeRemaining > 0;
        if (isInsertInterrupted)
        {
            component.ScanningTimeRemaining = 0f; // Reset animation

            // Drop from slot because animation did not play completely
            _itemSlotsSystem.SetLock(uid, component.PaperSlot, false);
            _itemSlotsSystem.TryEject(uid, component.PaperSlot, null, out var _, true);
        }

        var isPrintInterrupted = !args.Powered && component.PrintingTimeRemaining > 0;
        if (isPrintInterrupted)
        {
            component.PrintingTimeRemaining = 0f; // Reset animation
        }

        if (isInsertInterrupted || isPrintInterrupted)
            UpdateAppearance(uid, component);

        _itemSlotsSystem.SetLock(uid, component.PaperSlot, !args.Powered); // Lock slot when power is off
    }

    private void OnEmagged(EntityUid uid, PhotocopierComponent component, ref GotEmaggedEvent args)
    {
        _audioSystem.PlayPvs(component.EmagSound, uid);
        component.WasEmagged = true;
        UpdateUserInterface(uid, component);
        args.Handled = true;
    }

    private void OnGetVerbs(EntityUid uid, PhotocopierComponent component, GetVerbsEvent<Verb> args)
    {
        args.Verbs.Add(new Verb
        {
            Priority = 11,
            Act = () =>
            {
                if (EntityManager.TryGetComponent(args.User, out HandsComponent? hands)
                    && hands.ActiveHandEntity is { } held
                    && EntityManager.TryGetComponent(held, out TonerCartridgeComponent? toner))
                {
                    if (component.TonerLeft == component.MaxTonerAmount)
                    {
                        _popupSystem.PopupEntity(Loc.GetString("photocopier-verb-toner-already-full-popup"), args.User, args.User);
                        return;
                    }

                    int amountToFull = component.MaxTonerAmount - component.TonerLeft;

                    if (toner.CurrentAmount <= amountToFull)
                    {
                        component.TonerLeft += toner.CurrentAmount;
                        toner.CurrentAmount -= toner.CurrentAmount;
                        _audioSystem.PlayPvs(component.TonerRestock, uid);
                        _popupSystem.PopupEntity(Loc.GetString("photocopier-verb-toner-popup"), uid, args.User);
                    }
                    else if (toner.CurrentAmount > amountToFull)
                    {
                        component.TonerLeft += amountToFull;
                        toner.CurrentAmount -= amountToFull;
                        _audioSystem.PlayPvs(component.TonerRestock, uid);
                        _popupSystem.PopupEntity(Loc.GetString("photocopier-verb-toner-popup"), uid, args.User);
                    }
                }
                else
                {
                    _popupSystem.PopupEntity(Loc.GetString("photocopier-verb-no-toner-popup"), args.User, args.User);
                    return;
                }
            },
            Text = Loc.GetString("photocopier-verb-toner-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png"))
        });
    }

    private void OnToggleInterface(EntityUid uid, PhotocopierComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnFormButtonPressed(EntityUid uid, PhotocopierComponent component, PhotocopierChoseFormMessage args)
    {
        component.ChosenPaper = args.PaperworkForm;
        UpdateUserInterface(uid, component);
    }

    private void OnCopyModeButtonPressed(EntityUid uid, PhotocopierComponent component, PhotocopierCopyModeMessage args)
    {
        component.Mode = PhotocopierMode.Copy;
        UpdateUserInterface(uid, component);
    }

    private void OnPrintModeButtonPressed(EntityUid uid, PhotocopierComponent component, PhotocopierPrintModeMessage args)
    {
        component.Mode = PhotocopierMode.Print;
        UpdateUserInterface(uid, component);
    }

    private void OnPrintButtonPressed(EntityUid uid, PhotocopierComponent component, PhotocopierPrintMessage args)
    {
        if (args.Amount > component.TonerLeft)
        {
            _popupSystem.PopupEntity(Loc.GetString("photocopier-popup-low-toner-amount"), uid, args.Actor);
            return;
        }

        if (args.Mode == PhotocopierMode.Copy && component.PaperSlot != null)
        {
            _audioSystem.PlayPvs(component.ScanSound, uid);
            var isPaperInserted = component.PaperSlot.Item.HasValue;
            if (isPaperInserted)
            {
                component.ScanningTimeRemaining = component.ScanningTime;
                _itemSlotsSystem.SetLock(uid, component.PaperSlot, true);
            }
            PrintCopy(uid, args.Amount, component);
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Actor):actor}start printing {args.Amount} copys on photocopier");
        }
        else if (args.Mode == PhotocopierMode.Print)
        {
            if (component.ChosenPaper == null)
            {
                return;
            }
            PrintForm(uid, args.Amount, component.ChosenPaper, component);
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Actor):actor} start printing {args.Amount} copys of '{Loc.GetString(component.ChosenPaper.Name)}' on photocopier");
        }
        component.TonerLeft -= args.Amount;
        UpdateUserInterface(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, PhotocopierComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.ScanningTimeRemaining > 0)
            _appearanceSystem.SetData(uid, PhotocopierVisuals.VisualState, PhotocopierVisualState.Scanning);
        else if (component.PrintingTimeRemaining > 0)
            _appearanceSystem.SetData(uid, PhotocopierVisuals.VisualState, PhotocopierVisualState.Printing);
        else
            _appearanceSystem.SetData(uid, PhotocopierVisuals.VisualState, PhotocopierVisualState.Normal);
    }

    private void UpdateUserInterface(EntityUid uid, PhotocopierComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var isPaperInserted = component.PaperSlot.Item != null;
        var canPrint = component.UseTimeoutRemaining <= 0 &&
                      component.ScanningTimeRemaining <= 0;
        var state = new PhotocopierUiState(canPrint, isPaperInserted, component.ChosenPaper, component.Mode, component.PhotocopierType, component.WasEmagged,
            component.TonerLeft < 0 ? 0 : component.TonerLeft, component.MaxTonerAmount);
        _userInterface.SetUiState(uid, PhotocopierUiKey.Key, state);
    }

    public void PrintCopy(EntityUid uid, int сopiesAmount, PhotocopierComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var copyEntity = component.PaperSlot.Item;
        if (copyEntity == null)
            return;

        if (!TryComp<MetaDataComponent>(copyEntity, out var metadata) ||
            !TryComp<PaperComponent>(copyEntity, out var paper) || metadata.EntityPrototype == null)
            return;

        _appearanceSystem.SetData(uid, PhotocopierVisuals.VisualState, PhotocopierVisualState.Printing);

        var printout = new PhotocopierPrintout(paper.Content, metadata.EntityName, metadata.EntityPrototype.ID, paper.StampState, paper.StampedBy);

        for (int i = 0; i < сopiesAmount; i++)
        {
            component.PrintingQueue.Enqueue(printout);
            component.UseTimeoutRemaining += component.UseTimeout;
        }
        component.UseTimeoutRemaining += component.ScanningTime;
    }

    private void PrintForm(EntityUid uid, int сopiesAmount, PaperworkFormPrototype formPrototype, PhotocopierComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        string text = _resourceManager.ContentFileReadText(formPrototype.Text).ReadToEnd();

        text = text.Replace("DOCUMENT NAME", Loc.GetString(formPrototype.Name));
        text = text.Replace("{{HOUR.MINUTE.SECOND}}", _gameTicker.RoundDuration().ToString("hh\\:mm\\:ss"));
        text = text.Replace("{{DAY.MONTH.YEAR}}", DateTime.UtcNow.AddHours(3).ToString("dd.MM") + ".2709");

        if (_station.GetOwningStation(uid) is { } station)
        {
            text = text.Replace("STATION XX-00", Name(station));
        }

        var printout = new PhotocopierPrintout(text, "Распечатанный документ", formPrototype.PaperPrototype, null, null);

        for (int i = 0; i < сopiesAmount; i++)
        {
            component.PrintingQueue.Enqueue(printout);
            component.UseTimeoutRemaining += component.UseTimeout;
        }
    }

    private void SpawnPaperFromQueue(EntityUid uid, PhotocopierComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.PrintingQueue.Count == 0)
            return;

        var printout = component.PrintingQueue.Dequeue();

        var printed = EntityManager.SpawnEntity(printout.PrototypeId, Transform(uid).Coordinates);

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
        }
    }
}
