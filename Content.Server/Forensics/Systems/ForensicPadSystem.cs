using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;

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

        private const string Sample = "sample";
        private const string Pad = "pad";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ForensicPadComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<ForensicPadComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<DoAfterEvent>(OnDoAfter);
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
            if (!args.CanReach || args.Target == null)
                return;

            if (HasComp<ForensicScannerComponent>(args.Target))
                return;

            args.Handled = true;

            if (component.Used)
            {
                _popupSystem.PopupEntity(Loc.GetString("forensic-pad-already-used"), args.Target.Value, args.User);
                return;
            }

            if (_inventory.TryGetSlotEntity(args.Target.Value, "gloves", out var gloves))
            {
                _popupSystem.PopupEntity(Loc.GetString("forensic-pad-gloves", ("target", Identity.Entity(args.Target.Value, EntityManager))), args.Target.Value, args.User);
                return;
            }

            if (TryComp<FingerprintComponent>(args.Target, out var fingerprint) && fingerprint.Fingerprint != null)
            {
                if (args.User != args.Target)
                {
                    _popupSystem.PopupEntity(Loc.GetString("forensic-pad-start-scan-user", ("target", Identity.Entity(args.Target.Value, EntityManager))), args.Target.Value, args.User);
                    _popupSystem.PopupEntity(Loc.GetString("forensic-pad-start-scan-target", ("user", Identity.Entity(args.User, EntityManager))), args.Target.Value, args.Target.Value);
                }
                StartScan(args.User, args.Target.Value, component, fingerprint.Fingerprint);
                return;
            }

            if (TryComp<FiberComponent>(args.Target, out var fiber))
                StartScan(args.User, args.Target.Value, component, string.IsNullOrEmpty(fiber.FiberColor) ? Loc.GetString("forensic-fibers", ("material", fiber.FiberMaterial)) : Loc.GetString("forensic-fibers-colored", ("color", fiber.FiberColor), ("material", fiber.FiberMaterial)));
        }

        private void StartScan(EntityUid user, EntityUid target, ForensicPadComponent pad, string sample)
        {
            var additionalData = new Dictionary<string, object>
            {
                {Pad, pad.Owner},
                {Sample, sample}
            };

            var doAfterEventArgs = new DoAfterEventArgs(user, pad.ScanDelay, target: target)
            {
                Broadcast = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            };

            _doAfterSystem.SetAdditionalData(additionalData, doAfterEventArgs);
            _doAfterSystem.DoAfter(doAfterEventArgs);
        }

        private void OnDoAfter(DoAfterEvent ev)
        {
            if (ev.Handled
                || ev.Cancelled
                || !_doAfterSystem.TryGetAdditionalData(Pad, out EntityUid pad, ev.Args)
                || !_doAfterSystem.TryGetAdditionalData(Sample, out string sample, ev.Args)
                || !EntityManager.TryGetComponent(pad, out ForensicPadComponent? component))
            {
                return;
            }

            if (ev.Args.Target != null)
            {
                if (HasComp<FingerprintComponent>(ev.Args.Target))
                    MetaData(component.Owner).EntityName = Loc.GetString("forensic-pad-fingerprint-name", ("entity", ev.Args.Target));
                else
                    MetaData(component.Owner).EntityName = Loc.GetString("forensic-pad-gloves-name", ("entity", ev.Args.Target));
            }

            component.Sample = sample;
            component.Used = true;
        }
    }
}
