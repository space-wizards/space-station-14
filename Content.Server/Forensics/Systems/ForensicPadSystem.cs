using System.Threading;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Robust.Shared.Player;

namespace Content.Server.Forensics
{
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
            if (component.CancelToken != null)
            {
                component.CancelToken.Cancel();
                component.CancelToken = null;
            }

            if (!args.CanReach || args.Target == null)
                return;

            if (HasComp<ForensicScannerComponent>(args.Target))
                return;

            if (component.Used)
            {
                _popupSystem.PopupEntity(Loc.GetString("forensic-pad-already-used"), args.Target.Value, Filter.Entities(args.User));
                return;
            }

            if (_inventory.TryGetSlotEntity(args.Target.Value, "gloves", out var gloves))
            {
                _popupSystem.PopupEntity(Loc.GetString("forensic-pad-gloves", ("target", args.Target.Value)), args.Target.Value, Filter.Entities(args.User));
                return;
            }

            if (TryComp<FingerprintComponent>(args.Target, out var fingerprint) && fingerprint.Fingerprint != null)
            {
                if (args.User != args.Target)
                {
                    _popupSystem.PopupEntity(Loc.GetString("forensic-pad-start-scan-user", ("target", args.Target.Value)), args.Target.Value, Filter.Entities(args.User));
                    _popupSystem.PopupEntity(Loc.GetString("forensic-pad-start-scan-target", ("user", args.User)), args.Target.Value, Filter.Entities(args.Target.Value));
                }
                StartScan(args.User, args.Target.Value, component, fingerprint.Fingerprint);
                return;
            }

            if (TryComp<FiberComponent>(args.Target, out var fiber))
                StartScan(args.User, args.Target.Value, component, Loc.GetString(fiber.FiberDescription));
        }

        private void StartScan(EntityUid user, EntityUid target, ForensicPadComponent pad, string sample)
        {
            pad.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(user, pad.ScanDelay, pad.CancelToken.Token, target: target)
            {
                BroadcastFinishedEvent = new TargetPadSuccessfulEvent(user, target, pad, sample),
                BroadcastCancelledEvent = new PadCancelledEvent(user, pad),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }

        public void PressSample(EntityUid uid, string sample, ForensicPadComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.Sample = sample;
            component.Used = true;
        }

        private void OnTargetPadSuccessful(TargetPadSuccessfulEvent ev)
        {
            ev.Component.CancelToken = null;

            PressSample(ev.Component.Owner, ev.Sample, ev.Component);
        }
        private void OnPadCancelled(PadCancelledEvent ev)
        {
            if (ev.Component == null)
                return;
            ev.Component.CancelToken = null;
        }

        private sealed class PadCancelledEvent : EntityEventArgs
        {
            public EntityUid Uid;
            public ForensicPadComponent Component;

            public PadCancelledEvent(EntityUid uid, ForensicPadComponent component)
            {
                Uid = uid;
                Component = component;
            }
        }

        private sealed class TargetPadSuccessfulEvent : EntityEventArgs
        {
            public EntityUid User;
            public EntityUid? Target;
            public ForensicPadComponent Component;
            public string Sample = string.Empty;

            public TargetPadSuccessfulEvent(EntityUid user, EntityUid? target, ForensicPadComponent component, string sample)
            {
                User = user;
                Target = target;
                Component = component;
                Sample = sample;
            }
        }
    }
}
