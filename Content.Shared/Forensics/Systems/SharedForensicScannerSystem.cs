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
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
// todo: remove this stinky LINQy

namespace Content.Shared.Forensics.Systems
{
    public abstract class SharedForensicScannerSystem : EntitySystem
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

            SubscribeLocalEvent<ForensicScannerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ForensicScannerComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<ForensicScannerComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);
            SubscribeLocalEvent<ForensicScannerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
            SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerPrintMessage>(OnPrint);
            SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerClearMessage>(OnClear);
            SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerDoAfterEvent>(OnDoAfter);
        }

        protected virtual void UpdateUi(Entity<ForensicScannerComponent> ent)
        {
        }

        private void UpdateUserInterface(Entity<ForensicScannerComponent> ent)
        {
            UpdateUi((ent, ent.Comp));

        }

        private void OnDoAfter(EntityUid uid, ForensicScannerComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;

            if (args.Args.Target != null)
            {
                if (!TryComp<ForensicsComponent>(args.Args.Target, out var forensics))
                {
                    component.Fingerprints = new();
                    component.Fibers = new();
                    component.DNAs = new();
                    component.Residues = new();
                }
                else
                {
                    component.Fingerprints = forensics.Fingerprints.ToList();
                    component.Fibers = forensics.Fibers.ToList();
                    component.DNAs = forensics.DNAs.ToList();
                    component.Residues = forensics.Residues.ToList();
                }

                if (_tag.HasTag(args.Args.Target.Value, DNASolutionScannableTag))
                {
                    component.SolutionDNAs = _forensicsSystem.GetSolutionsDNA(args.Args.Target.Value);
                } else
                {
                    component.SolutionDNAs = new();
                }

                component.LastScannedName = MetaData(args.Args.Target.Value).EntityName;
                Dirty(uid, component);
            }

            OpenUserInterface(args.Args.User, (uid, component));
        }

        /// <remarks>
        /// Hosts logic common between OnUtilityVerb and OnAfterInteract.
        /// </remarks>
        private void StartScan(EntityUid uid, ForensicScannerComponent component, EntityUid user, EntityUid target)
        {
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, component.ScanDelay, new ForensicScannerDoAfterEvent(), uid, target: target, used: uid)
            {
                BreakOnMove = true,
                NeedHand = true
            });
        }

        private void OnUtilityVerb(EntityUid uid, ForensicScannerComponent component, GetVerbsEvent<UtilityVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            var verb = new UtilityVerb()
            {
                Act = () => StartScan(uid, component, args.User, args.Target),
                IconEntity = GetNetEntity(uid),
                Text = Loc.GetString("forensic-scanner-verb-text"),
                Message = Loc.GetString("forensic-scanner-verb-message"),
                // This is important because if its true using the scanner will count as touching the object.
                DoContactInteraction = false
            };

            args.Verbs.Add(verb);
        }

        private void OnAfterInteract(EntityUid uid, ForensicScannerComponent component, AfterInteractEvent args)
        {
            if (args.Target == null || !args.CanReach)
                return;

            StartScan(uid, component, args.User, args.Target.Value);
        }

        private void OnAfterInteractUsing(EntityUid uid, ForensicScannerComponent component, AfterInteractUsingEvent args)
        {
            if (args.Handled || !args.CanReach)
                return;

            if (!TryComp<ForensicPadComponent>(args.Used, out var pad))
                return;

            foreach (var fiber in component.Fibers)
            {
                if (fiber == pad.Sample)
                {
                    _audioSystem.PlayPredicted(component.SoundMatch, uid, args.User);
                    _popupSystem.PopupClient(Loc.GetString("forensic-scanner-match-fiber"), uid, args.User);
                    return;
                }
            }

            foreach (var fingerprint in component.Fingerprints)
            {
                if (fingerprint == pad.Sample)
                {
                    _audioSystem.PlayPredicted(component.SoundMatch, uid, args.User);
                    _popupSystem.PopupClient(Loc.GetString("forensic-scanner-match-fingerprint"), uid, args.User);
                    return;
                }
            }

            _audioSystem.PlayPredicted(component.SoundNoMatch, uid, args.User);
            _popupSystem.PopupClient(Loc.GetString("forensic-scanner-match-none"), uid, args.User);
        }

        private void OnBeforeActivatableUIOpen(Entity<ForensicScannerComponent> ent, ref BeforeActivatableUIOpenEvent args)
        {
            UpdateUserInterface(ent);
        }

        private void OpenUserInterface(EntityUid user, Entity<ForensicScannerComponent> scanner)
        {
            UpdateUserInterface(scanner);
            _uiSystem.OpenUi(scanner.Owner, ForensicScannerUiKey.Key, user);
        }

        private void OnPrint(EntityUid uid, ForensicScannerComponent component, ForensicScannerPrintMessage args)
        {
            var user = args.Actor;

            if (_gameTiming.CurTime < component.PrintReadyAt)
            {
                // This shouldn't occur due to the UI guarding against it, but
                // if it does, tell the user why nothing happened.
                _popupSystem.PopupClient(Loc.GetString("forensic-scanner-printer-not-ready"), uid, user);
                return;
            }

            // Spawn a piece of paper.
            var printed = PredictedSpawnAtPosition(component.MachineOutput, Transform(uid).Coordinates);
            _handsSystem.PickupOrDrop(args.Actor, printed, checkActionBlocker: false);

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
            foreach (var dna in component.SolutionDNAs)
            {
                Log.Debug(dna);
                if (component.DNAs.Contains(dna))
                    continue;
                text.AppendLine(dna);
            }
            text.AppendLine();
            text.AppendLine(Loc.GetString("forensic-scanner-interface-residues"));
            foreach (var residue in component.Residues)
            {
                text.AppendLine(residue);
            }

            _paperSystem.SetContent((printed, paperComp), text.ToString());
            _audioSystem.PlayPredicted(component.SoundPrint, uid, args.Actor,
                AudioParams.Default
                .WithVariation(0.25f)
                .WithVolume(3f)
                .WithRolloffFactor(2.8f)
                .WithMaxDistance(4.5f));

            component.PrintReadyAt = _gameTiming.CurTime + component.PrintCooldown;
            Dirty(uid, component);
        }

        private void OnClear(Entity<ForensicScannerComponent> ent, ref ForensicScannerClearMessage args)
        {
            ent.Comp.Fingerprints = new();
            ent.Comp.Fibers = new();
            ent.Comp.DNAs = new();
            ent.Comp.SolutionDNAs = new();
            ent.Comp.LastScannedName = string.Empty;

            Dirty(ent, ent.Comp);
            UpdateUserInterface(ent);
        }
    }
}
