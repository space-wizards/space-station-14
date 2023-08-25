using Content.Server.Medical.Components;
using Content.Server.PowerCell;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Components;
using Robust.Server.GameObjects;
using Content.Server.Temperature.Components;
using Content.Server.Body.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Components;

namespace Content.Server.Medical
{
    public sealed class HealthAnalyzerSystem : EntitySystem
    {
        [Dependency] private readonly PowerCellSystem _cell = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HealthAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<HealthAnalyzerComponent, HealthAnalyzerDoAfterEvent>(OnDoAfter);
        }

        private void OnAfterInteract(EntityUid uid, HealthAnalyzerComponent healthAnalyzer, AfterInteractEvent args)
        {
            if (args.Target == null || !args.CanReach || !HasComp<MobStateComponent>(args.Target) || !_cell.HasActivatableCharge(uid, user: args.User))
                return;

            _audio.PlayPvs(healthAnalyzer.ScanningBeginSound, uid);

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(args.User, healthAnalyzer.ScanDelay, new HealthAnalyzerDoAfterEvent(), uid, target: args.Target, used: uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            });
        }

        private void OnDoAfter(EntityUid uid, HealthAnalyzerComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Args.Target == null || !_cell.TryUseActivatableCharge(uid, user: args.User))
                return;

            _audio.PlayPvs(component.ScanningEndSound, args.Args.User);

            UpdateScannedUser(uid, args.Args.User, args.Args.Target.Value, component);
            args.Handled = true;
        }

        private void OpenUserInterface(EntityUid user, HealthAnalyzerComponent healthAnalyzer)
        {
            if (!TryComp<ActorComponent>(user, out var actor) || healthAnalyzer.UserInterface == null)
                return;

            _uiSystem.OpenUi(healthAnalyzer.UserInterface ,actor.PlayerSession);
        }

        public void UpdateScannedUser(EntityUid uid, EntityUid user, EntityUid? target, HealthAnalyzerComponent? healthAnalyzer)
        {
            if (!Resolve(uid, ref healthAnalyzer))
                return;

            if (target == null || healthAnalyzer.UserInterface == null)
                return;

            if (!HasComp<DamageableComponent>(target))
                return;

            TryComp<TemperatureComponent>(target, out var temp);
            TryComp<BloodstreamComponent>(target, out var bloodstream);

            Dictionary<string, FixedPoint2> chemicals = new();

            //get the chemicals in the target
            if (TryComp<SolutionContainerManagerComponent>(target, out var solutionContainerManagerComponent) && solutionContainerManagerComponent != null)
            {
                foreach (KeyValuePair<string, Solution> pair in solutionContainerManagerComponent.Solutions)
                {
                    foreach (Solution.ReagentQuantity reagentQuantity in pair.Value.Contents)
                    {
                        //if it always exists then just increase the quantity
                        if (chemicals.ContainsKey(reagentQuantity.ReagentId))
                        {
                            chemicals[reagentQuantity.ReagentId] += reagentQuantity.Quantity;
                        }
                        else
                        {
                            chemicals.Add(reagentQuantity.ReagentId, reagentQuantity.Quantity);
                        }
                    }
                }
            }

            OpenUserInterface(user, healthAnalyzer);

            _uiSystem.SendUiMessage(healthAnalyzer.UserInterface, new HealthAnalyzerScannedUserMessage(target, temp != null ? temp.CurrentTemperature : float.NaN,
                bloodstream != null ? bloodstream.BloodSolution.FillFraction : float.NaN, chemicals));
        }
    }
}
