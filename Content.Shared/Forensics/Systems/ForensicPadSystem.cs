using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Forensics.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Popups;

namespace Content.Shared.Forensics.Systems
{
    /// <summary>
    /// Used to transfer fingerprints from entities to forensic pads.
    /// </summary>
    public sealed class ForensicPadSystem : EntitySystem
    {
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly ForensicsSystem _forensics = default!;
        [Dependency] private readonly LabelSystem _label = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ForensicPadComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<ForensicPadComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ForensicPadComponent, ForensicPadDoAfterEvent>(OnDoAfter);
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

        private void OnAfterInteract(Entity<ForensicPadComponent> pad, ref AfterInteractEvent args)
        {
            if (!args.CanReach || args.Target == null || HasComp<ForensicScannerComponent>(args.Target))
                return;

            args.Handled = true;

            if (pad.Comp.Used)
            {
                _popup.PopupClient(Loc.GetString("forensic-pad-already-used"), args.Target.Value, args.User);
                return;
            }

            if (!_forensics.CanAccessFingerprint(args.Target.Value, out var blocker))
            {
                var message = blocker is { } item
                    ? Loc.GetString("forensic-pad-no-access-due", ("entity", Identity.Entity(item, EntityManager)))
                    : Loc.GetString("forensic-pad-no-access");

                _popup.PopupClient(message, args.Target.Value, args.User);
                return;
            }

            if (TryComp<FingerprintComponent>(args.Target, out var fingerprint) && fingerprint.Fingerprint != null)
            {
                var skipDelay = true;
                if (args.User != args.Target)
                {
                    var userMessage = Loc.GetString("forensic-pad-start-scan-user", ("target", Identity.Entity(args.Target.Value, EntityManager)));
                    var targetMessage = Loc.GetString("forensic-pad-start-scan-target", ("user", Identity.Entity(args.User, EntityManager)));

                    skipDelay = false;
                    _popup.PopupClient(userMessage, args.Target.Value, args.User);
                    _popup.PopupEntity(targetMessage, args.Target.Value, args.Target.Value);
                }
                StartScan(pad, args.User, args.Target.Value, pad.Comp, fingerprint.Fingerprint, skipDelay);
                return;
            }

            if (TryComp<FiberComponent>(args.Target, out var fiber))
            {
                var fiberString = string.IsNullOrEmpty(fiber.FiberColor)
                    ? Loc.GetString("forensic-fibers", ("material", fiber.FiberMaterial))
                    : Loc.GetString("forensic-fibers-colored", ("color", fiber.FiberColor), ("material", fiber.FiberMaterial));

                StartScan(pad, args.User, args.Target.Value, pad.Comp, fiberString, true);
            }
        }

        private void StartScan(EntityUid used, EntityUid user, EntityUid target, ForensicPadComponent pad, string sample, bool skipDelay = false)
        {
            var ev = new ForensicPadDoAfterEvent(sample);
            var delay = skipDelay ? TimeSpan.Zero : pad.ScanDelay;

            var doAfterEventArgs = new DoAfterArgs(EntityManager, user, delay, ev, used, target: target, used: used)
            {
                NeedHand = true,
                BreakOnMove = true,
            };

            _doAfter.TryStartDoAfter(doAfterEventArgs);
        }

        private void OnDoAfter(Entity<ForensicPadComponent> pad, ref ForensicPadDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;

            if (args.Args.Target != null)
            {
                var label = Identity.Name(args.Args.Target.Value, EntityManager);
                _label.Label(pad, label);
            }

            pad.Comp.Sample = args.Sample;
            pad.Comp.Used = true;

            args.Handled = true;
        }
    }
}
