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

namespace Content.Shared.Forensics.Systems
{
    public partial class SharedForensicScannerSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly PaperSystem _paperSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;
        [Dependency] private readonly SharedForensicsSystem _forensicsSystem = default!;
        [Dependency] private readonly TagSystem _tag = default!;

        private static readonly ProtoId<TagPrototype> DNASolutionScannableTag = "DNASolutionScannable";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ForensicScannerComponent, AfterAutoHandleStateEvent>(OnScannerUpdate);
            SubscribeLocalEvent<ForensicScannerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ForensicScannerComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<ForensicScannerComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);
            SubscribeLocalEvent<ForensicScannerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
            SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerPrintMessage>(OnPrint);
            SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerClearMessage>(OnClear);
            SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerDoAfterEvent>(OnDoAfter);
        }

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
                Dirty(scanner);
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

            foreach (var fiber in scanner.Comp.Fibers)
            {
                if (fiber != pad.Sample)
                    continue;

                _audioSystem.PlayPredicted(scanner.Comp.SoundMatch, scanner.Owner, args.User);
                _popupSystem.PopupPredicted(Loc.GetString("forensic-scanner-match-fiber"), scanner, args.User);
                return;
            }

            foreach (var fingerprint in scanner.Comp.Fingerprints)
            {
                if (fingerprint != pad.Sample)
                    continue;

                _audioSystem.PlayPredicted(scanner.Comp.SoundMatch, scanner.Owner, args.User);
                _popupSystem.PopupPredicted(Loc.GetString("forensic-scanner-match-fingerprint"), scanner, args.User);
                return;
            }

            _audioSystem.PlayPredicted(scanner.Comp.SoundNoMatch, scanner.Owner, args.User);
            _popupSystem.PopupPredicted(Loc.GetString("forensic-scanner-match-none"), scanner, args.User);
        }

        private void OnBeforeActivatableUIOpen(Entity<ForensicScannerComponent> scanner, ref BeforeActivatableUIOpenEvent args)
        {
            UpdateUi(scanner);
        }

        private void OpenUserInterface(EntityUid user, Entity<ForensicScannerComponent> scanner)
        {
            _uiSystem.OpenUi(scanner.Owner, ForensicScannerUiKey.Key, user, true);
            UpdateUi(scanner);
        }

        private void OnPrint(Entity<ForensicScannerComponent> scanner, ref ForensicScannerPrintMessage args)
        {
            var user = args.Actor;

            if (_gameTiming.CurTime < scanner.Comp.PrintReadyAt)
            {
                // This shouldn't occur due to the UI guarding against it, but
                // if it does, tell the user why nothing happened.
                _popupSystem.PopupClient(Loc.GetString("forensic-scanner-printer-not-ready"), scanner, user);
                return;
            }

            // Spawn a piece of paper.
            var printed = PredictedSpawnAtPosition(scanner.Comp.MachineOutput, Transform(scanner).Coordinates);
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
                Log.Debug(dna);
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
            _audioSystem.PlayPredicted(scanner.Comp.SoundPrint, scanner, user,
                AudioParams.Default
                .WithVariation(0.25f)
                .WithVolume(3f)
                .WithRolloffFactor(2.8f)
                .WithMaxDistance(4.5f));

            scanner.Comp.PrintReadyAt = _gameTiming.CurTime + scanner.Comp.PrintCooldown;

            Dirty(scanner);
        }

        private void OnClear(Entity<ForensicScannerComponent> scanner, ref ForensicScannerClearMessage args)
        {
            scanner.Comp.Fingerprints = [];
            scanner.Comp.Fibers = [];
            scanner.Comp.DNAs = [];
            scanner.Comp.SolutionDNAs = new();
            scanner.Comp.LastScannedName = string.Empty;

            Dirty(scanner);
            UpdateUi(scanner);
        }
    }
}
