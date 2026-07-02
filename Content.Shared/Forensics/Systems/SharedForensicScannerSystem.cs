using System.Text;
using Content.Shared.DoAfter;
using Content.Shared.Forensics.Components;
using Content.Shared.Forensics.Events;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

// todo: remove this stinky LINQy

namespace Content.Shared.Forensics.Systems;

public abstract partial class SharedForensicScannerSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private SharedHandsSystem _handsSystem = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private PaperSystem _paperSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ForensicScannerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ForensicScannerComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<ForensicScannerComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);
        SubscribeLocalEvent<ForensicScannerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);

        SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerPrintMessage>(OnPrint);
        SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerClearMessage>(OnClear);
    }

    /// <remarks>
    /// Hosts logic common between OnUtilityVerb and OnAfterInteract.
    /// </remarks>
    protected virtual void StartScan(Entity<ForensicScannerComponent> scanner, EntityUid user, EntityUid target)
    {
        scanner.Comp.SuccessfulScanned = false;
        scanner.Comp.LastScannedName = Identity.Name(target, EntityManager, target);

        DirtyFields(scanner.AsNullable(), null, nameof(scanner.Comp.SuccessfulScanned), nameof(scanner.Comp.LastScannedName));

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, scanner.Comp.ScanDelay, new ForensicScannerDoAfterEvent(), scanner, target: target, used: scanner)
        {
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnUtilityVerb(Entity<ForensicScannerComponent> scanner, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var evArgs = args;

        var verb = new UtilityVerb
        {
            Act = () => StartScan(scanner, evArgs.User, evArgs.Target),
            IconEntity = GetNetEntity(scanner),
            Text = Loc.GetString("forensic-scanner-verb-text"),
            Message = Loc.GetString("forensic-scanner-verb-message"),
            // This is important because if its true using the scanner will count as touching the object.
            DoContactInteraction = false,
        };

        args.Verbs.Add(verb);
    }

    private void OnAfterInteract(Entity<ForensicScannerComponent> scanner, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach)
            return;

        StartScan(scanner, args.User, args.Target.Value);
    }

    private void OnAfterInteractUsing(Entity<ForensicScannerComponent> scanner, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!TryComp<ForensicPadComponent>(args.Used, out var pad))
            return;

        var component = scanner.Comp;

        foreach (var fiber in component.Fibers)
        {
            if (fiber != pad.Sample)
                continue;

            _audioSystem.PlayPredicted(component.SoundMatch, scanner.Owner, args.User);
            _popupSystem.PopupPredicted(Loc.GetString("forensic-scanner-match-fiber"), scanner, args.User);
            return;
        }

        foreach (var fingerprint in component.Fingerprints)
        {
            if (fingerprint != pad.Sample)
                continue;

            _audioSystem.PlayPredicted(component.SoundMatch, scanner.Owner, args.User);
            _popupSystem.PopupPredicted(Loc.GetString("forensic-scanner-match-fingerprint"), scanner, args.User);
            return;
        }

        _audioSystem.PlayPredicted(component.SoundNoMatch, scanner.Owner, args.User);
        _popupSystem.PopupPredicted(Loc.GetString("forensic-scanner-match-none"), scanner, args.User);
    }

    protected void OpenUi(EntityUid user, Entity<ForensicScannerComponent> scanner)
    {
        _ui.OpenUi(scanner.Owner, ForensicScannerUiKey.Key, user, true);
        UpdateUi(scanner);
    }

    private void OnDoAfter(Entity<ForensicScannerComponent> scanner, ref ForensicScannerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        scanner.Comp.SuccessfulScanned = true;
        DirtyField(scanner.AsNullable(), nameof(scanner.Comp.SuccessfulScanned));

        OpenUi(args.Args.User, scanner);
    }

    private void OnPrint(Entity<ForensicScannerComponent> scanner, ref ForensicScannerPrintMessage args)
    {
        var user = args.Actor;
        var component = scanner.Comp;

        if (_gameTiming.CurTime < component.PrintReadyAt)
        {
            // This shouldn't occur due to the UI guarding against it, but
            // if it does, tell the user why nothing happened.
            _popupSystem.PopupClient(Loc.GetString("forensic-scanner-printer-not-ready"), scanner, user);
            return;
        }

        // Spawn a piece of paper.
        var printed = Spawn(component.PaperPrototypeID, Transform(scanner).Coordinates);
        _handsSystem.PickupOrDrop(user, printed, checkActionBlocker: false);

        if (!TryComp<PaperComponent>(printed, out var paperComp))
        {
            Log.Error("Printed paper did not have PaperComponent.");
            return;
        }

        _metaData.SetEntityName(printed, Loc.GetString("forensic-scanner-report-title", ("entity", component.LastScannedName)));

        var text = new StringBuilder();

        text.AppendLine(Loc.GetString("forensic-scanner-interface-fingerprints"));
        foreach (var fingerprint in component.Fingerprints)
        {
            text.AppendLine(fingerprint);
        }
        text.AppendLine();
        text.AppendLine(Loc.GetString("forensic-scanner-interface-fibers"));
        foreach (var fiber in component.Fibers)
        {
            text.AppendLine(fiber);
        }
        text.AppendLine();
        text.AppendLine(Loc.GetString("forensic-scanner-interface-dnas"));
        foreach (var dna in component.DNAs)
        {
            text.AppendLine(dna);
        }
        text.AppendLine();
        text.AppendLine(Loc.GetString("forensic-scanner-interface-residues"));
        foreach (var residue in component.Residues)
        {
            text.AppendLine(residue);
        }

        _paperSystem.SetContent((printed, paperComp), text.ToString());
        _audioSystem.PlayPredicted(component.SoundPrint, scanner, user,
            AudioParams.Default
                .WithVariation(0.25f)
                .WithVolume(3f)
                .WithRolloffFactor(2.8f)
                .WithMaxDistance(4.5f));

        component.PrintReadyAt = _gameTiming.CurTime + component.PrintCooldown;
        DirtyField(scanner.AsNullable(), nameof(component.PrintReadyAt));
    }

    private void OnClear(Entity<ForensicScannerComponent> scanner, ref ForensicScannerClearMessage args)
    {
        var component = scanner.Comp;

        component.Fingerprints = [];
        component.Fibers = [];
        component.DNAs = [];
        component.LastScannedName = string.Empty;

        Dirty(scanner);
        UpdateUi(scanner);
    }

    private void OnBeforeActivatableUIOpen(Entity<ForensicScannerComponent> scanner, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUi(scanner);
    }

    private void UpdateUi(Entity<ForensicScannerComponent> scanner)
    {
        if (_ui.TryGetOpenUi(scanner.Owner, ForensicScannerUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}
