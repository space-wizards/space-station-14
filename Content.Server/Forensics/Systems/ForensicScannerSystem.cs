using System.Linq;
using System.Text; // todo: remove this stinky LINQy
using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Paper;
using Content.Server.Popups;
using Content.Shared.Forensics;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Forensics
{
    public sealed class ForensicScannerSystem : EntitySystem
    {
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly PaperSystem _paperSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ForensicScannerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ForensicScannerComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerPrintMessage>(OnPrint);
            SubscribeLocalEvent<TargetScanSuccessfulEvent>(OnTargetScanSuccessful);
            SubscribeLocalEvent<ScanCancelledEvent>(OnScanCancelled);
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
              return;

            scanner.Fingerprints = forensics.Fingerprints.ToList();
            scanner.Fibers = forensics.Fibers.ToList();
            scanner.LastScanned = MetaData(ev.Target).EntityName;
            OpenUserInterface(ev.User, scanner);
        }

        private void OnAfterInteract(EntityUid uid, ForensicScannerComponent component, AfterInteractEvent args)
        {
            if (component.CancelToken != null || args.Target == null || !args.CanReach)
                return;

            component.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, component.ScanDelay, component.CancelToken.Token, target: args.Target)
            {
                BroadcastFinishedEvent = new TargetScanSuccessfulEvent(args.User, (EntityUid) args.Target, component.Owner),
                BroadcastCancelledEvent = new ScanCancelledEvent(component.Owner),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
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
                    SoundSystem.Play("/Audio/Machines/Nuke/angry_beep.ogg", Filter.Pvs(uid), uid);
                    _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-fiber"), uid, Filter.Entities(args.User));
                    return;
                }
            }

            foreach (var fingerprint in component.Fingerprints)
            {
                if (fingerprint == pad.Sample)
                {
                    SoundSystem.Play("/Audio/Machines/Nuke/angry_beep.ogg", Filter.Pvs(uid), uid);
                    _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-fingerprint"), uid, Filter.Entities(args.User));
                    return;
                }
            }
            SoundSystem.Play("/Audio/Machines/airlock_deny.ogg", Filter.Pvs(uid), uid);
            _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-none"), uid, Filter.Entities(args.User));
        }

        private void OpenUserInterface(EntityUid user, ForensicScannerComponent component)
        {
            if (!TryComp<ActorComponent>(user, out var actor))
                return;

            var ui = _uiSystem.GetUi(component.Owner, ForensicScannerUiKey.Key);

            ui.Open(actor.PlayerSession);
            ui.SendMessage(new ForensicScannerUserMessage(component.Fingerprints, component.Fibers, component.LastScanned));
        }

        private void OnPrint(EntityUid uid, ForensicScannerComponent component, ForensicScannerPrintMessage args)
        {
            if (!args.Session.AttachedEntity.HasValue || (component.Fibers.Count == 0 && component.Fingerprints.Count == 0)) return;

            // spawn a piece of paper.
            var printed = EntityManager.SpawnEntity("Paper", Transform(args.Session.AttachedEntity.Value).Coordinates);
            _handsSystem.PickupOrDrop(args.Session.AttachedEntity, printed, checkActionBlocker: false);

            if (!TryComp<PaperComponent>(printed, out var paper))
                return;

            MetaData(printed).EntityName = Loc.GetString("forensic-scanner-report-title", ("entity", component.LastScanned));

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
