using System;
using System.Collections.Generic;
using Content.Shared.Printer;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Content.Shared.DoAfter;
using Content.Server.Popups;
using Content.Server.Paper;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Map;

namespace Content.Server.Printer;

public sealed class PrinterSystem : EntitySystem
{

    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrinterComponent, ComponentInit>(OnComponentInit);

        SubscribeLocalEvent<PrinterComponent, PrinterPrintFile>(OnPrintFile);
        SubscribeLocalEvent<PrinterComponent, PrinterUnstuckEvent>(OnUnstuckDoAfter);
        
        SubscribeLocalEvent<PrinterComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<PrinterComponent, EntRemovedFromContainerMessage>(OnItemRemoved);
        
        SubscribeLocalEvent<PrinterComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<PrinterComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<PrinterComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<PrinterComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<PrinterComponent, ContainerIsRemovingAttemptEvent>(OnRemoveAttempt);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        var query = EntityQueryEnumerator<PrinterComponent, ApcPowerReceiverComponent, ContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var printer, out var receiver, out var containerManager))
        {
            if(!receiver.Powered || containerManager.GetContainer(printer.TrayId).ContainedEntities.Count == 0)
            {
                if(printer.FilesRemaining > 0)
                    printer.FilesRemaining = 0;
                continue;
            }
            if(printer.PrintingTimeLeft > 0)
                ProcessPrintingAnimation(uid, deltaTime, printer);
        }
    }

    private void OnComponentInit(EntityUid uid, PrinterComponent component, ComponentInit args)
    {
        UpdateAppearance(uid, component);
    }

    private void ProcessPrintingAnimation(EntityUid uid, float deltaTime, PrinterComponent component)
    {
        component.PrintingTimeLeft -= deltaTime;
        if(component.PrintingTimeLeft <= 0f && component.FilesRemaining > 0)
        {
            component.PrintingTimeLeft = 0f;

            if(!TryComp<ContainerManagerComponent>(uid, out var containerManager))
                return;
            if(!containerManager.HasContainer(component.TrayId))
                return;

            var paperTray = containerManager.GetContainer(component.TrayId);
            var paperList = paperTray.ContainedEntities;
            if(paperTray.Count > 0)
            {
                var paper = paperList[0];
                if (HasComp<PaperComponent>(paper))
                    _paperSystem.SetContent(paper, component.FileText);
                if(component.FileName != string.Empty)
                    _metaDataSystem.SetEntityName(paper, component.FileName);

                component.IsPrinting = false;
                containerManager.Remove(paper);
                component.FilesRemaining--;
                if(component.FilesRemaining > 0 && component.PaperQuantity > 0)
                {
                    StartPrinting(uid, component);
                    return;
                }
            }
        }     
    }

    private void OnPrintFile(EntityUid uid, PrinterComponent component, PrinterPrintFile args)
    {
        if(component.IsPrinting || component.IsStuck)
            return;
        component.FileText = args.Content;
        component.FileName = args.Name;
        component.FilesRemaining = args.Number;
        if(component.PrintingTimeLeft == 0f)
            StartPrinting(uid, component);
    }

    private void StartPrinting(EntityUid uid, PrinterComponent component)
    {
        if(_random.Prob(0.05f))
        {
            MakePaperStuck(uid, component);
            return;
        }
        component.PrintingTimeLeft = 2.2f;
        component.IsPrinting = true;
        UpdateAppearance(uid, component);
    }

    private void UnstuckPaper(EntityUid uid, PrinterComponent component, EntityUid user)
    {
        var doAfterArgs = new DoAfterArgs(
            _entityManager,
            user,
            TimeSpan.FromSeconds(3),
            new PrinterUnstuckEvent(),
            uid
        );
        doAfterArgs.BreakOnUserMove = true;
        doAfterArgs.NeedHand = true;
        
        _doAfterSystem.TryStartDoAfter(doAfterArgs);
        _popupSystem.PopupEntity("You try to remove the stuck paper!", uid, user);
    }

    private void OnUnstuckDoAfter(EntityUid uid, PrinterComponent component, PrinterUnstuckEvent args)
    {
        component.IsStuck = false;

        if(!TryComp<ContainerManagerComponent>(uid, out var containerManager))
            return;
        if(!containerManager.HasContainer(component.TrayId))
            return;

        var paperTray = containerManager.GetContainer(component.TrayId);
        var paperList = paperTray.ContainedEntities;
        if(paperList.Count > 0)
            containerManager.Remove(paperList[0]);
    }

    private void OnInteract(EntityUid uid, PrinterComponent component, InteractHandEvent args)
    {
        if(component.IsStuck)
        {
            UnstuckPaper(uid, component, args.User);
            args.Handled = true;
        }
    }

    private void OnInteractUsing(EntityUid uid, PrinterComponent component, InteractUsingEvent args)
    {
        if(_tagSystem.HasTag(args.Used, "Document") && component.IsStuck)
        {
            _popupSystem.PopupEntity("Unstuck the paper first!", uid, args.User);
            args.Handled = true;
        }
    }

    private void OnInsertAttempt(EntityUid uid, PrinterComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if(component.IsStuck)
        {
            RaiseLocalEvent(new PrinterPaperStuck(uid));
            args.Cancel();
        }
        if(component.IsPrinting)
        {
            RaiseLocalEvent(new PrinterPaperInteractStuck(uid));
            MakePaperStuck(uid, component);
        }
    }

    private void OnRemoveAttempt(EntityUid uid, PrinterComponent component, ContainerIsRemovingAttemptEvent args)
    {
        if(component.IsStuck)
        {
            RaiseLocalEvent(new PrinterPaperStuck(uid));
            args.Cancel();
        }
        if(component.IsPrinting)
        {
            RaiseLocalEvent(new PrinterPaperInteractStuck(uid));
            args.Cancel();
            MakePaperStuck(uid, component);
        }
    }

    private void OnItemInserted(EntityUid uid, PrinterComponent component, EntInsertedIntoContainerMessage args)
    {
        component.PaperQuantity++;
        UpdateAppearance(uid, component);
    }

    private void OnItemRemoved(EntityUid uid, PrinterComponent component, EntRemovedFromContainerMessage args)
    {
        component.PaperQuantity--;
        UpdateAppearance(uid, component);
    }

    private void OnPowerChanged(EntityUid uid, PrinterComponent component, ref PowerChangedEvent args)
    {
        if(component.IsPrinting && !args.Powered)
        {
            MakePaperStuck(uid, component);
            return;
        }
        UpdateAppearance(uid, component);
    }

    private void MakePaperStuck(EntityUid uid, PrinterComponent component)
    {
        component.IsPrinting = false;
        component.PrintingTimeLeft = 0;
        component.IsStuck = true;
        UpdateAppearance(uid, component);
        _popupSystem.PopupEntity("The paper got stuck!", uid);
    }

    private void UpdateAppearance(EntityUid uid, PrinterComponent component)
    {
        var state = DetermineAppearance(uid, component);
        if(component.VisualState == state)
            return;
        _appearanceSystem.SetData(uid, PrinterVisuals.VisualState, state);
        component.VisualState = state;
    }

    private PrinterVisualState DetermineAppearance(EntityUid uid, PrinterComponent component)
    {
        if(component.IsPrinting)
        {
            if(component.PaperQuantity > 1)
                return PrinterVisualState.Printing;
            else
                return PrinterVisualState.PrintingLast;
        }

        TryComp<ApcPowerReceiverComponent>(uid, out var power);
        if(power == null)
            return PrinterVisualState.Empty;

        if(component.IsStuck)
        {
            if(component.PaperQuantity > 1) 
                return PrinterVisualState.CrumpledPaper;
            return PrinterVisualState.Crumpled;
        }
        if(component.PaperQuantity > 0) 
            return PrinterVisualState.Paper;
        return PrinterVisualState.Empty;
    }
}