using System.Linq;
using System.Text; // todo: remove this stinky LINQy
using System.Threading;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Server.DoAfter;
using Content.Server.Paper;
using Content.Server.Popups;
using Content.Server.UserInterface;
using Content.Shared.Forensics;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Verbs;

namespace Content.Server.Forensics
{
    public sealed class ForensicScannerSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly PaperSystem _paperSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

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
            SubscribeLocalEvent<TargetScanSuccessfulEvent>(OnTargetScanSuccessful);
            SubscribeLocalEvent<ScanCancelledEvent>(OnScanCancelled);
        }

        private void UpdateUserInterface(EntityUid uid, ForensicScannerComponent component)
        {
            var state = new ForensicScannerBoundUserInterfaceState(
                component.Fingerprints,
                component.Fibers,
                component.LastScannedName,
                component.PrintCooldown,
                component.PrintReadyAt);

            if (!_uiSystem.TrySetUiState(uid, ForensicScannerUiKey.Key, state))
            {
                _sawmill.Warning($"{ToPrettyString(uid)} was unable to set UI state.");
                return;
            }
        }

        private void OnScanCancelled(ScanCancelledEvent ev)
        {
            if (!EntityManager.TryGetComponent(ev.Scanner, out ForensicScannerComponent? scanner))
                return;

            scanner.CancelToken = null;
        }

        private void OnTargetScanSuccessful(TargetScanSuccessfulEvent ev)
        {
            if (!EntityManager.TryGetComponent(ev.Scanner, out ForensicScannerComponent? scanner))
                return;

            scanner.CancelToken = null;

            if (!TryComp<ForensicsComponent>(ev.Target, out var forensics))
            {
                scanner.Fingerprints = new();
                scanner.Fibers = new();
            }
            else
            {
                scanner.Fingerprints = forensics.Fingerprints.ToList();
                scanner.Fibers = forensics.Fibers.ToList();
            }

            scanner.LastScannedName = MetaData(ev.Target).EntityName;

            OpenUserInterface(ev.User, scanner);
        }

        /// <remarks>
        /// Hosts logic common between OnUtilityVerb and OnAfterInteract.
        /// </remarks>
        private void StartScan(EntityUid uid, ForensicScannerComponent component, EntityUid user, EntityUid target)
        {
            component.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(user, component.ScanDelay, component.CancelToken.Token, target: target)
            {
                BroadcastFinishedEvent = new TargetScanSuccessfulEvent(user, (EntityUid) target, component.Owner),
                BroadcastCancelledEvent = new ScanCancelledEvent(component.Owner),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
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
                IconEntity = uid,
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

        private void OpenUserInterface(EntityUid user, ForensicScannerComponent component)
        {
            if (!TryComp<ActorComponent>(user, out var actor))
                return;

            UpdateUserInterface(component.Owner, component);

            _uiSystem.TryOpen(component.Owner, ForensicScannerUiKey.Key, actor.PlayerSession);
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
            var printed = EntityManager.SpawnEntity("Paper", Transform(uid).Coordinates);
            _handsSystem.PickupOrDrop(args.Session.AttachedEntity, printed, checkActionBlocker: false);

            if (!TryComp<PaperComponent>(printed, out var paper))
            {
                _sawmill.Error("Printed paper did not have PaperComponent.");
                return;
            }

            MetaData(printed).EntityName = Loc.GetString("forensic-scanner-report-title", ("entity", component.LastScannedName));

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
            component.LastScannedName = string.Empty;

            UpdateUserInterface(uid, component);
        }

        private sealed class ScanCancelledEvent : EntityEventArgs
        {
            public EntityUid Scanner;

            public ScanCancelledEvent(EntityUid scanner)
            {
                Scanner = scanner;
            }
        }

        private sealed class TargetScanSuccessfulEvent : EntityEventArgs
        {
            public EntityUid User;
            public EntityUid Target;
            public EntityUid Scanner;
            public TargetScanSuccessfulEvent(EntityUid user, EntityUid target, EntityUid scanner)
            {
                User = user;
                Target = target;
                Scanner = scanner;
            }
        }
    }
}
