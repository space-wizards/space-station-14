using System.Linq;
using System.Text;
using Content.Server.Paper;
using Content.Server.Popups;
using Content.Server.UserInterface;
using Content.Shared.DoAfter;
using Content.Shared.Forensics;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;
// todo: remove this stinky LINQy

namespace Content.Server.Forensics
{
    public sealed class ForensicScannerSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly PaperSystem _paperSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("forensics.scanner");

            SubscribeLocalEvent<ForensicScannerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ForensicScannerComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<ForensicScannerComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);
            SubscribeLocalEvent<ForensicScannerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
            SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerPrintMessage>(OnPrint);
            SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerClearMessage>(OnClear);
            SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerDoAfterEvent>(OnDoAfter);
        }

        private void UpdateUserInterface(EntityUid uid, ForensicScannerComponent component)
        {
            var state = new ForensicScannerBoundUserInterfaceState(
                component.Fingerprints,
                component.Fibers,
                component.DNAs,
                component.LastScannedName,
                component.PrintCooldown,
                component.PrintReadyAt);

            if (!_uiSystem.TrySetUiState(uid, ForensicScannerUiKey.Key, state))
                _sawmill.Warning($"{ToPrettyString(uid)} was unable to set UI state.");
        }

        private void OnDoAfter(EntityUid uid, ForensicScannerComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;

            if (!EntityManager.TryGetComponent(uid, out ForensicScannerComponent? scanner))
                return;

            if (args.Args.Target != null)
            {
                if (!TryComp<ForensicsComponent>(args.Args.Target, out var forensics))
                {
                    scanner.Fingerprints = new();
                    scanner.Fibers = new();
                    scanner.DNAs = new();
                }

                else
                {
                    scanner.Fingerprints = forensics.Fingerprints.ToList();
                    scanner.Fibers = forensics.Fibers.ToList();
                    scanner.DNAs = forensics.DNAs.ToList();
                }

                scanner.LastScannedName = MetaData(args.Args.Target.Value).EntityName;
            }

            OpenUserInterface(args.Args.User, (uid, scanner));
        }

        /// <remarks>
        /// Hosts logic common between OnUtilityVerb and OnAfterInteract.
        /// </remarks>
        private void StartScan(EntityUid uid, ForensicScannerComponent component, EntityUid user, EntityUid target)
        {
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, component.ScanDelay, new ForensicScannerDoAfterEvent(), uid, target: target, used: uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            });
        }

        private void OnUtilityVerb(EntityUid uid, ForensicScannerComponent component, GetVerbsEvent<UtilityVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess || component.CancelToken != null)
                return;

            var verb = new UtilityVerb()
            {
                Act = () => StartScan(uid, component, args.User, args.Target),
                IconEntity = GetNetEntity(uid),
                Text = Loc.GetString("forensic-scanner-verb-text"),
                Message = Loc.GetString("forensic-scanner-verb-message")
            };

            args.Verbs.Add(verb);
        }

        private void OnAfterInteract(EntityUid uid, ForensicScannerComponent component, AfterInteractEvent args)
        {
            if (component.CancelToken != null || args.Target == null || !args.CanReach)
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
                    _audioSystem.PlayPvs(component.SoundMatch, uid);
                    _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-fiber"), uid, args.User);
                    return;
                }
            }

            foreach (var fingerprint in component.Fingerprints)
            {
                if (fingerprint == pad.Sample)
                {
                    _audioSystem.PlayPvs(component.SoundMatch, uid);
                    _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-fingerprint"), uid, args.User);
                    return;
                }
            }

            _audioSystem.PlayPvs(component.SoundNoMatch, uid);
            _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-none"), uid, args.User);
        }

        private void OnBeforeActivatableUIOpen(EntityUid uid, ForensicScannerComponent component, BeforeActivatableUIOpenEvent args)
        {
            UpdateUserInterface(uid, component);
        }

        private void OpenUserInterface(EntityUid user, Entity<ForensicScannerComponent> scanner)
        {
            if (!TryComp<ActorComponent>(user, out var actor))
                return;

            UpdateUserInterface(scanner, scanner.Comp);

            _uiSystem.TryOpen(scanner, ForensicScannerUiKey.Key, actor.PlayerSession);
        }

        private void OnPrint(EntityUid uid, ForensicScannerComponent component, ForensicScannerPrintMessage args)
        {
            if (!args.Session.AttachedEntity.HasValue)
            {
                _sawmill.Warning($"{ToPrettyString(uid)} got OnPrint without Session.AttachedEntity");
                return;
            }

            var user = args.Session.AttachedEntity.Value;

            if (_gameTiming.CurTime < component.PrintReadyAt)
            {
                // This shouldn't occur due to the UI guarding against it, but
                // if it does, tell the user why nothing happened.
                _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-printer-not-ready"), uid, user);
                return;
            }

            // Spawn a piece of paper.
            var printed = EntityManager.SpawnEntity(component.MachineOutput, Transform(uid).Coordinates);
            _handsSystem.PickupOrDrop(args.Session.AttachedEntity, printed, checkActionBlocker: false);

            if (!HasComp<PaperComponent>(printed))
            {
                _sawmill.Error("Printed paper did not have PaperComponent.");
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

            _paperSystem.SetContent(printed, text.ToString());
            _audioSystem.PlayPvs(component.SoundPrint, uid,
                AudioParams.Default
                .WithVariation(0.25f)
                .WithVolume(3f)
                .WithRolloffFactor(2.8f)
                .WithMaxDistance(4.5f));

            component.PrintReadyAt = _gameTiming.CurTime + component.PrintCooldown;
        }

        private void OnClear(EntityUid uid, ForensicScannerComponent component, ForensicScannerClearMessage args)
        {
            if (!args.Session.AttachedEntity.HasValue)
                return;

            component.Fingerprints = new();
            component.Fibers = new();
            component.DNAs = new();
            component.LastScannedName = string.Empty;

            UpdateUserInterface(uid, component);
        }
    }
}
