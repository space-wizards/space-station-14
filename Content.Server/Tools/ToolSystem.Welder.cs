using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Server.IgnitionSource;
using Content.Server.Tools.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Tools.Components;
using Robust.Shared.GameStates;

namespace Content.Server.Tools
{
    public sealed partial class ToolSystem
    {
        [Dependency] private readonly SharedItemToggleSystem _itemToggle = default!;
        [Dependency] private readonly IgnitionSourceSystem _ignitionSource = default!;
        private readonly HashSet<EntityUid> _activeWelders = new();

        private const float WelderUpdateTimer = 1f;
        private float _welderTimer;

        public void InitializeWelders()
        {
            SubscribeLocalEvent<WelderComponent, ExaminedEvent>(OnWelderExamine);
            SubscribeLocalEvent<WelderComponent, AfterInteractEvent>(OnWelderAfterInteract);
            SubscribeLocalEvent<WelderComponent, DoAfterAttemptEvent<ToolDoAfterEvent>>(OnWelderToolUseAttempt);
            SubscribeLocalEvent<WelderComponent, ComponentShutdown>(OnWelderShutdown);
            SubscribeLocalEvent<WelderComponent, ComponentGetState>(OnWelderGetState);
            SubscribeLocalEvent<WelderComponent, ItemToggleActivateAttemptEvent>(TryTurnOn);
            SubscribeLocalEvent<WelderComponent, ItemToggleDeactivateAttemptEvent>(TurnOff);
        }

        public (FixedPoint2 fuel, FixedPoint2 capacity) GetWelderFuelAndCapacity(EntityUid uid, WelderComponent? welder = null, SolutionContainerManagerComponent? solutionContainer = null)
        {
            if (!Resolve(uid, ref welder, ref solutionContainer)
                || !_solutionContainer.TryGetSolution(uid, welder.FuelSolution, out var fuelSolution, solutionContainer))
                return (FixedPoint2.Zero, FixedPoint2.Zero);

            return (_solutionContainer.GetTotalPrototypeQuantity(uid, welder.FuelReagent), fuelSolution.MaxVolume);
        }

        public void TryTurnOn(EntityUid uid, WelderComponent welder, ref ItemToggleActivateAttemptEvent args)
        {
            if (!_solutionContainer.TryGetSolution(uid, welder.FuelSolution, out var solution) ||
                !TryComp<TransformComponent>(uid, out var transform))
            {
                args.Cancelled = true;
                return;
            }
            var fuel = solution.GetTotalPrototypeQuantity(welder.FuelReagent);

            // Not enough fuel to lit welder.
            if (fuel == FixedPoint2.Zero || fuel < welder.FuelLitCost)
            {
                if (args.User != null)
                {
                    _popup.PopupEntity(Loc.GetString("welder-component-no-fuel-message"), uid, (EntityUid) args.User);
                }
                args.Cancelled = true;
                return;
            }

            solution.RemoveReagent(welder.FuelReagent, welder.FuelLitCost);

            // Logging
            _adminLogger.Add(LogType.InteractActivate, LogImpact.Low, $"{ToPrettyString(args.User):user} toggled {ToPrettyString(uid):welder} on");

            _ignitionSource.SetIgnited(uid);

            if (transform.GridUid is { } gridUid)
            {
                var position = _transformSystem.GetGridOrMapTilePosition(uid, transform);
                _atmosphereSystem.HotspotExpose(gridUid, position, 700, 50, uid, true);
            }

            Dirty(uid, welder);

            _activeWelders.Add(uid);
        }

        public void TurnOff(EntityUid uid, WelderComponent welder, ref ItemToggleDeactivateAttemptEvent args)
        {
            // Logging
            _adminLogger.Add(LogType.InteractActivate, LogImpact.Low, $"{ToPrettyString(args.User):user} toggled {ToPrettyString(uid):welder} off");

            _ignitionSource.SetIgnited(uid, false);

            Dirty(uid, welder);

            _activeWelders.Remove(uid);
        }

        private void OnWelderExamine(EntityUid uid, WelderComponent welder, ExaminedEvent args)
        {
            if (_itemToggle.IsActivated(uid))
            {
                args.PushMarkup(Loc.GetString("welder-component-on-examine-welder-lit-message"));
            }
            else
            {
                args.PushMarkup(Loc.GetString("welder-component-on-examine-welder-not-lit-message"));
            }

            if (args.IsInDetailsRange)
            {
                var (fuel, capacity) = GetWelderFuelAndCapacity(uid, welder);

                args.PushMarkup(Loc.GetString("welder-component-on-examine-detailed-message",
                    ("colorName", fuel < capacity / FixedPoint2.New(4f) ? "darkorange" : "orange"),
                    ("fuelLeft", fuel),
                    ("fuelCapacity", capacity),
                    ("status", string.Empty))); // Lit status is handled above
            }
        }

        private void OnWelderAfterInteract(EntityUid uid, WelderComponent welder, AfterInteractEvent args)
        {
            if (args.Handled)
                return;

            if (args.Target is not { Valid: true } target || !args.CanReach)
                return;

            if (TryComp(target, out ReagentTankComponent? tank)
                && tank.TankType == ReagentTankType.Fuel
                && _solutionContainer.TryGetDrainableSolution(target, out var targetSolution)
                && _solutionContainer.TryGetSolution(uid, welder.FuelSolution, out var welderSolution))
            {
                var trans = FixedPoint2.Min(welderSolution.AvailableVolume, targetSolution.Volume);
                if (trans > 0)
                {
                    var drained = _solutionContainer.Drain(target, targetSolution, trans);
                    _solutionContainer.TryAddSolution(uid, welderSolution, drained);
                    _audio.PlayPvs(welder.WelderRefill, uid);
                    _popup.PopupEntity(Loc.GetString("welder-component-after-interact-refueled-message"), uid, args.User);
                }
                else if (welderSolution.AvailableVolume <= 0)
                {
                    _popup.PopupEntity(Loc.GetString("welder-component-already-full"), uid, args.User);
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("welder-component-no-fuel-in-tank", ("owner", args.Target)), uid, args.User);
                }
            }

            args.Handled = true;
        }

        private void OnWelderToolUseAttempt(EntityUid uid, WelderComponent welder, DoAfterAttemptEvent<ToolDoAfterEvent> args)
        {
            var user = args.DoAfter.Args.User;

            if (!_itemToggle.IsActivated(uid))
            {
                _popup.PopupEntity(Loc.GetString("welder-component-welder-not-lit-message"), uid, user);
                args.Cancel();
            }
        }

        private void OnWelderShutdown(EntityUid uid, WelderComponent welder, ComponentShutdown args)
        {
            _activeWelders.Remove(uid);
        }

        private void OnWelderGetState(EntityUid uid, WelderComponent welder, ref ComponentGetState args)
        {
            var (fuel, capacity) = GetWelderFuelAndCapacity(uid, welder);
            args.State = new WelderComponentState(capacity.Float(), fuel.Float());
        }

        private void UpdateWelders(float frameTime)
        {
            _welderTimer += frameTime;

            if (_welderTimer < WelderUpdateTimer)
                return;

            // TODO Use an "active welder" component instead, EntityQuery over that.
            foreach (var tool in _activeWelders.ToArray())
            {
                if (!TryComp(tool, out WelderComponent? welder)
                    || !TryComp(tool, out SolutionContainerManagerComponent? solutionContainer))
                    continue;

                if (!_solutionContainer.TryGetSolution(tool, welder.FuelSolution, out var solution, solutionContainer))
                    continue;

                solution.RemoveReagent(welder.FuelReagent, welder.FuelConsumption * _welderTimer);

                if (solution.GetTotalPrototypeQuantity(welder.FuelReagent) <= FixedPoint2.Zero)
                {
                    _itemToggle.Toggle(tool, predicted: false);
                }

                Dirty(tool, welder);
            }
            _welderTimer -= WelderUpdateTimer;
        }
    }
}
