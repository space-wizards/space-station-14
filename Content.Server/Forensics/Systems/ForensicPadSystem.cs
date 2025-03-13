using System.Linq; // imp
using Content.Server.Labels;
using Content.Server.Popups;
using Content.Shared.Chemistry.EntitySystems; // imp
using Content.Shared.Chemistry.Reagent; // imp
using Content.Shared.Contraband; // imp
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Forensics;
using Content.Shared.Forensics.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes; // imp

namespace Content.Server.Forensics
{
    /// <summary>
    /// Used to transfer fingerprints from entities to forensic pads.
    /// </summary>
    public sealed class ForensicPadSystem : EntitySystem
    {
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!; // imp
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!; // imp edit
        [Dependency] private readonly LabelSystem _label = default!;

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
                StartScan(uid, args.User, args.Target.Value, component, fingerprint.Fingerprint);
                return;
            }

            if (TryComp<FiberComponent>(args.Target, out var fiber))
            {
                StartScan(uid, args.User, args.Target.Value, component, string.IsNullOrEmpty(fiber.FiberColor) ? Loc.GetString("forensic-fibers", ("material", fiber.FiberMaterial)) : Loc.GetString("forensic-fibers-colored", ("color", fiber.FiberColor), ("material", fiber.FiberMaterial)));
                return;
            }

            if (_solutionContainerSystem.TryGetDrainableSolution(args.Target.Value, out _, out var solution) || // imp edit beginning
                _solutionContainerSystem.TryGetDrawableSolution(args.Target.Value, out _, out solution) ||
                _solutionContainerSystem.TryGetInjectorSolution(args.Target.Value, out _, out solution))
            {
                if (solution.Contents.Count == 0)
                {
                    return;
                }

                var sample = solution.Contents.Select(x =>
                {
                    if (_prototypeManager.TryIndex(x.Reagent.Prototype, out ReagentPrototype? reagent))
                    {
                        var localizedName = Loc.GetString(reagent.LocalizedName);
                        if (_prototypeManager.TryIndex(reagent.Contraband, out var contraband))
                        {
                            localizedName = $"[color={contraband.ExamineColor}]{localizedName}[/color]";
                        }
                        return localizedName;
                    }
                    return "???";
                }).Aggregate((x, y) => x + ", " + y);
                StartScan(uid, args.User, args.Target.Value, component, sample);
                return;
            } // imp edit end
        }

        private void StartScan(EntityUid used, EntityUid user, EntityUid target, ForensicPadComponent pad, string sample)
        {
            var ev = new ForensicPadDoAfterEvent(sample);

            var doAfterEventArgs = new DoAfterArgs(EntityManager, user, pad.ScanDelay, ev, used, target: target, used: used)
            {
                NeedHand = true,
                BreakOnMove = true,
            };

            _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
        }

        private void OnDoAfter(EntityUid uid, ForensicPadComponent padComponent, ForensicPadDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
            {
                return;
            }

            if (args.Args.Target != null)
            {
                string label = Identity.Name(args.Args.Target.Value, EntityManager);
                _label.Label(uid, label);
            }

            padComponent.Sample = args.Sample;
            padComponent.Used = true;

            args.Handled = true;
        }
    }
}
