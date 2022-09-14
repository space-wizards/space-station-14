using System.Threading;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.IdentityManagement;
using Robust.Shared.Player;

namespace Content.Server.Forensics
{
    /// <summary>
    /// Used to transfer fingerprints from entities to forensic pads.
    /// </summary>
    public sealed class ForensicPadSystem : EntitySystem
    {
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;

        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ForensicPadComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<ForensicPadComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<TargetPadSuccessfulEvent>(OnTargetPadSuccessful);
            SubscribeLocalEvent<PadCancelledEvent>(OnPadCancelled);
        }

        private void OnExamined(EntityUid uid, ForensicPadComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            if (!component.Used)
            {
                args.PushMarkup(Loc.GetString("forensic-pad-unused"));
                return;
            }

            args.PushMarkup(Loc.GetString("forensic-pad-sample", ("sample", component.Sample)));
        }

        private void OnAfterInteract(EntityUid uid, ForensicPadComponent component, AfterInteractEvent args)
        {
            if (component.CancelToken != null || !args.CanReach || args.Target == null)
                return;

            if (HasComp<ForensicScannerComponent>(args.Target))
                return;

            args.Handled = true;

            if (component.Used)
            {
                _popupSystem.PopupEntity(Loc.GetString("forensic-pad-already-used"), args.Target.Value, Filter.Entities(args.User));
                return;
            }

            if (_inventory.TryGetSlotEntity(args.Target.Value, "gloves", out var gloves))
            {
                _popupSystem.PopupEntity(Loc.GetString("forensic-pad-gloves", ("target", Identity.Entity(args.Target.Value, EntityManager))), args.Target.Value, Filter.Entities(args.User));
                return;
            }

            if (TryComp<FingerprintComponent>(args.Target, out var fingerprint) && fingerprint.Fingerprint != null)
            {
                if (args.User != args.Target)
                {
                    _popupSystem.PopupEntity(Loc.GetString("forensic-pad-start-scan-user", ("target", Identity.Entity(args.Target.Value, EntityManager))), args.Target.Value, Filter.Entities(args.User));
                    _popupSystem.PopupEntity(Loc.GetString("forensic-pad-start-scan-target", ("user", Identity.Entity(args.User, EntityManager))), args.Target.Value, Filter.Entities(args.Target.Value));
                }
                StartScan(args.User, args.Target.Value, component, fingerprint.Fingerprint);
                return;
            }

            if (TryComp<FiberComponent>(args.Target, out var fiber))
                StartScan(args.User, args.Target.Value, component, string.IsNullOrEmpty(fiber.FiberColor) ? Loc.GetString("forensic-fibers", ("material", fiber.FiberMaterial)) : Loc.GetString("forensic-fibers-colored", ("color", fiber.FiberColor), ("material", fiber.FiberMaterial)));
        }

        private void StartScan(EntityUid user, EntityUid target, ForensicPadComponent pad, string sample)
        {
            pad.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(user, pad.ScanDelay, pad.CancelToken.Token, target: target)
            {
                BroadcastFinishedEvent = new TargetPadSuccessfulEvent(user, target, pad.Owner, sample),
                BroadcastCancelledEvent = new PadCancelledEvent(pad.Owner),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }

        /// <summary>
        /// When the forensic pad is successfully used, take their fingerprint sample and flag the pad as used.
        /// </summary>
        private void OnTargetPadSuccessful(TargetPadSuccessfulEvent ev)
        {
            if (!EntityManager.TryGetComponent(ev.Pad, out ForensicPadComponent? component))
                return;

            if (HasComp<FingerprintComponent>(ev.Target))
                MetaData(component.Owner).EntityName = Loc.GetString("forensic-pad-fingerprint-name", ("entity", ev.Target));
            else
                MetaData(component.Owner).EntityName = Loc.GetString("forensic-pad-gloves-name", ("entity", ev.Target));

            component.CancelToken = null;
            component.Sample = ev.Sample;
            component.Used = true;
        }
        private void OnPadCancelled(PadCancelledEvent ev)
        {
            if (!EntityManager.TryGetComponent(ev.Pad, out ForensicPadComponent? component))
                return;
            component.CancelToken = null;
        }

        private sealed class PadCancelledEvent : EntityEventArgs
        {
            public EntityUid Pad;

            public PadCancelledEvent(EntityUid pad)
            {
                Pad = pad;
            }
        }

        private sealed class TargetPadSuccessfulEvent : EntityEventArgs
        {
            public EntityUid User;
            public EntityUid Target;
            public EntityUid Pad;
            public string Sample = string.Empty;

            public TargetPadSuccessfulEvent(EntityUid user, EntityUid target, EntityUid pad, string sample)
            {
                User = user;
                Target = target;
                Pad = pad;
                Sample = sample;
            }
        }
    }
}
