using System.Linq;
using System.Text;
using Content.Shared.UserInterface;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Verbs;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Timing;
using Content.Shared.Forensics.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
// todo: remove this stinky LINQy

namespace Content.Shared.Forensics.Systems;

public sealed partial class SharedForensicScannerSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private PaperSystem _paperSystem = default!;
    [Dependency] private SharedHandsSystem _handsSystem = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedForensicsSystem _forensicsSystem = default!;
    [Dependency] private TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> DNASolutionScannableTag = "DNASolutionScannable";

    [SubscribeLocalEvent]
    private void OnScannerUpdate(Entity<ForensicScannerComponent> scanner, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(scanner);
    }

    private void UpdateUi(Entity<ForensicScannerComponent> scanner)
    {
        if (_uiSystem.TryGetOpenUi(scanner.Owner, ForensicScannerUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<ForensicScannerComponent> scanner, ref ForensicScannerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target != null)
        {
            if (TryComp<ForensicsComponent>(args.Args.Target, out var forensics))
            {
                scanner.Comp.Fingerprints = forensics.Fingerprints.ToList();
                scanner.Comp.Fibers = forensics.Fibers.ToList();
                scanner.Comp.DNAs = forensics.DNAs.ToList();
                scanner.Comp.Residues = forensics.Residues.ToList();
            }
            else
            {
                scanner.Comp.Fingerprints = [];
                scanner.Comp.Fibers = [];
                scanner.Comp.DNAs = [];
                scanner.Comp.Residues = [];
            }

            if (_tag.HasTag(args.Args.Target.Value, DNASolutionScannableTag))
            {
                scanner.Comp.DNAs.AddRange(_forensicsSystem.GetSolutionsDNA(args.Args.Target.Value));
            }

            scanner.Comp.LastScannedName = Identity.Name(args.Args.Target.Value, EntityManager, args.Args.User);

            DirtyFields(scanner.AsNullable(),
                null,
                nameof(ForensicScannerComponent.Fingerprints),
                nameof(ForensicScannerComponent.Fibers),
                nameof(ForensicScannerComponent.DNAs),
                nameof(ForensicScannerComponent.Residues),
                nameof(ForensicScannerComponent.LastScannedName));
        }

        OpenUserInterface(args.Args.User, scanner);
    }

    /// <remarks>
    /// Hosts logic common between OnUtilityVerb and OnAfterInteract.
    /// </remarks>
    private void StartScan(Entity<ForensicScannerComponent> scanner, EntityUid user, EntityUid target)
    {
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, scanner.Comp.ScanDelay, new ForensicScannerDoAfterEvent(), scanner, target: target, used: scanner)
        {
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    [SubscribeLocalEvent]
    private void OnUtilityVerb(Entity<ForensicScannerComponent> scanner, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
                return;

        var evArgs = args;

        var verb = new UtilityVerb()
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

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<ForensicScannerComponent> scanner, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach)
                return;

        StartScan(scanner, args.User, args.Target.Value);
    }

    [SubscribeLocalEvent]
    private void OnAfterInteractUsing(Entity<ForensicScannerComponent> scanner, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
                return;

        if (!TryComp<ForensicPadComponent>(args.Used, out var pad))
            return;

        foreach (var fiber in scanner.Comp.Fibers)
        {
            if (fiber != pad.Sample)
                continue;

            _audioSystem.PlayPredicted(scanner.Comp.SoundMatch, scanner.Owner, args.User);
            _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-fiber"), scanner, args.User);
            return;
        }

        foreach (var fingerprint in scanner.Comp.Fingerprints)
        {
            if (fingerprint != pad.Sample)
                continue;

            _audioSystem.PlayPredicted(scanner.Comp.SoundMatch, scanner.Owner, args.User);
            _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-fingerprint"), scanner, args.User);
            return;
        }

        _audioSystem.PlayPredicted(scanner.Comp.SoundNoMatch, scanner.Owner, args.User);
        _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-none"), scanner, args.User);
    }

    [SubscribeLocalEvent]
    private void OnBeforeActivatableUIOpen(Entity<ForensicScannerComponent> scanner, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUi(scanner);
    }

    private void OpenUserInterface(EntityUid user, Entity<ForensicScannerComponent> scanner)
    {
        _uiSystem.OpenUi(scanner.Owner, ForensicScannerUiKey.Key, user, true);

        UpdateUi(scanner);
    }

    [SubscribeLocalEvent]
    private void OnPrint(Entity<ForensicScannerComponent> scanner, ref ForensicScannerPrintMessage args)
    {
        var user = args.Actor;

        if (_gameTiming.CurTime < scanner.Comp.PrintReadyAt)
        {
            // This shouldn't occur due to the UI guarding against it, but
            // if it does, tell the user why nothing happened.
            _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-printer-not-ready"), scanner, user);
            return;
        }

        // Spawn a piece of paper.
        var printed = PredictedSpawnAtPosition(scanner.Comp.PaperPrototypeID, Transform(scanner).Coordinates);
        _handsSystem.PickupOrDrop(user, printed, checkActionBlocker: false);

        if (!TryComp<PaperComponent>(printed, out var paperComp))
        {
            Log.Error("Printed paper did not have PaperComponent.");
            return;
        }

        _metaData.SetEntityName(printed, Loc.GetString("forensic-scanner-report-title", ("entity", scanner.Comp.LastScannedName)));

        var text = new StringBuilder();

        text.AppendLine(Loc.GetString("forensic-scanner-interface-fingerprints"));
        foreach (var fingerprint in scanner.Comp.Fingerprints)
        {
            text.AppendLine(fingerprint);
        }
        text.AppendLine();
        text.AppendLine(Loc.GetString("forensic-scanner-interface-fibers"));
        foreach (var fiber in scanner.Comp.Fibers)
        {
            text.AppendLine(fiber);
        }
        text.AppendLine();
        text.AppendLine(Loc.GetString("forensic-scanner-interface-dnas"));
        foreach (var dna in scanner.Comp.DNAs)
        {
            text.AppendLine(dna);
        }
        foreach (var dna in scanner.Comp.SolutionDNAs)
        {
            if (scanner.Comp.DNAs.Contains(dna))
                continue;
            text.AppendLine(dna);
        }
        text.AppendLine();
        text.AppendLine(Loc.GetString("forensic-scanner-interface-residues"));
        foreach (var residue in scanner.Comp.Residues)
        {
            text.AppendLine(residue);
        }

        _paperSystem.SetContent((printed, paperComp), text.ToString());
        var audioParams = scanner.Comp.SoundPrint?.Params ?? AudioParams.Default;
        audioParams = audioParams.WithVariation(0.25f).AddVolume(3f).WithRolloffFactor(2.8f).WithMaxDistance(4.5f);
        _audioSystem.PlayPredicted(scanner.Comp.SoundPrint, scanner, user, audioParams);

        scanner.Comp.PrintReadyAt = _gameTiming.CurTime + scanner.Comp.PrintCooldown;

        DirtyField(scanner.AsNullable(), nameof(ForensicScannerComponent.PrintReadyAt));

        UpdateUi(scanner);
    }

    [SubscribeLocalEvent]
    private void OnClear(Entity<ForensicScannerComponent> scanner, ref ForensicScannerClearMessage args)
    {
        scanner.Comp.Fingerprints = [];
        scanner.Comp.Fibers = [];
        scanner.Comp.DNAs = [];
        scanner.Comp.SolutionDNAs = new();
        scanner.Comp.LastScannedName = string.Empty;

        DirtyFields(scanner.AsNullable(),
            null,
            nameof(ForensicScannerComponent.Fingerprints),
            nameof(ForensicScannerComponent.Fibers),
            nameof(ForensicScannerComponent.DNAs),
            nameof(ForensicScannerComponent.SolutionDNAs),
            nameof(ForensicScannerComponent.LastScannedName));

        UpdateUi(scanner);
    }
}
