using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Server.Tools.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Tools.Components;
using Robust.Shared.GameStates;

namespace Content.Server.Tools
{
    public sealed partial class ToolSystem
    {
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
            SubscribeLocalEvent<WelderComponent, ItemToggleDeactivatedEvent>(TurnOff);

        }

        public (FixedPoint2 fuel, FixedPoint2 capacity) GetWelderFuelAndCapacity(EntityUid uid, WelderComponent? welder = null, SolutionContainerManagerComponent? solutionContainer = null)
        {
            if (!Resolve(uid, ref welder, ref solutionContainer)
                || !_solutionContainerSystem.TryGetSolution(uid, welder.FuelSolution, out var fuelSolution, solutionContainer))
                return (FixedPoint2.Zero, FixedPoint2.Zero);

            return (_solutionContainerSystem.GetTotalPrototypeQuantity(uid, welder.FuelReagent), fuelSolution.MaxVolume);
        }


        public void TryTurnOn(EntityUid uid, WelderComponent welder, ref ItemToggleActivateAttemptEvent args)
        {

            if (!_solutionContainerSystem.TryGetSolution(uid, welder.FuelSolution, out var solution) ||
                !TryComp<TransformComponent>(uid, out var transform))
            {
                args.Cancelled = true;
                return;
            }
            var fuel = solution.GetTotalPrototypeQuantity(welder.FuelReagent);

            if (TryComp<ItemToggleComponent>(uid, out var itemToggleComp))
            {
                // Not enough fuel to lit welder.
                if (fuel == FixedPoint2.Zero || fuel < welder.FuelLitCost)
                {
                    _popupSystem.PopupEntity(Loc.GetString("welder-component-no-fuel-message"), uid, itemToggleComp.User);
                    args.Cancelled = true;
                    return;
                }

                solution.RemoveReagent(welder.FuelReagent, welder.FuelLitCost);

                // Logging
                _adminLogger.Add(LogType.InteractActivate, LogImpact.Low, $"{ToPrettyString(itemToggleComp.User):user} toggled {ToPrettyString(uid):welder} on");
            }

            var ev = new WelderToggledEvent(true);
            RaiseLocalEvent(uid, ev);

            if (transform.GridUid is { } gridUid)
            {
                var position = _transformSystem.GetGridOrMapTilePosition(uid, transform);
                _atmosphereSystem.HotspotExpose(gridUid, position, 700, 50, uid, true);
            }

            Dirty(uid, welder);

            _activeWelders.Add(uid);
        }

        public void TurnOff(EntityUid uid, WelderComponent welder, ref ItemToggleDeactivatedEvent args)
        {
            if (TryComp<ItemToggleComponent>(uid, out var itemToggleComp))
            {
                // Logging
                _adminLogger.Add(LogType.InteractActivate, LogImpact.Low, $"{ToPrettyString(itemToggleComp.User):user} toggled {ToPrettyString(uid):welder} off");
            }
            var ev = new WelderToggledEvent(false);
            RaiseLocalEvent(uid, ev);

            Dirty(uid, welder);

            _activeWelders.Remove(uid);
        }

        private void OnWelderExamine(EntityUid uid, WelderComponent welder, ExaminedEvent args)
        {
            if (TryComp<ItemToggleComponent>(uid, out var itemToggleComp))
            {
                if (itemToggleComp.Activated)
                {
                    args.PushMarkup(Loc.GetString("welder-component-on-examine-welder-lit-message"));
                }
                else
                {
                    args.PushMarkup(Loc.GetString("welder-component-on-examine-welder-not-lit-message"));
                }
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

            // TODO: Clean up this inherited oldcode.

            if (EntityManager.TryGetComponent(target, out ReagentTankComponent? tank)
                && tank.TankType == ReagentTankType.Fuel
                && _solutionContainerSystem.TryGetDrainableSolution(target, out var targetSolution)
                && _solutionContainerSystem.TryGetSolution(uid, welder.FuelSolution, out var welderSolution))
            {
                var trans = FixedPoint2.Min(welderSolution.AvailableVolume, targetSolution.Volume);
                if (trans > 0)
                {
                    var drained = _solutionContainerSystem.Drain(target, targetSolution, trans);
                    _solutionContainerSystem.TryAddSolution(uid, welderSolution, drained);
                    _audioSystem.PlayPvs(welder.WelderRefill, uid);
                    _popupSystem.PopupEntity(Loc.GetString("welder-component-after-interact-refueled-message"), uid, args.User);
                }
                else if (welderSolution.AvailableVolume <= 0)
                {
                    _popupSystem.PopupEntity(Loc.GetString("welder-component-already-full"), uid, args.User);
                }
                else
                {
                    _popupSystem.PopupEntity(Loc.GetString("welder-component-no-fuel-in-tank", ("owner", args.Target)), uid, args.User);
                }
            }

            args.Handled = true;
        }

        private void OnWelderToolUseAttempt(EntityUid uid, WelderComponent welder, DoAfterAttemptEvent<ToolDoAfterEvent> args)
        {
            var user = args.DoAfter.Args.User;

            if (TryComp<ItemToggleComponent>(uid, out var itemToggleComp))
            {
                if (!itemToggleComp.Activated)
                {
                    _popupSystem.PopupEntity(Loc.GetString("welder-component-welder-not-lit-message"), uid, user);
                    args.Cancel();
                    return;
                }
            }
        }

        private void OnWelderShutdown(EntityUid uid, WelderComponent welder, ComponentShutdown args)
        {
            _activeWelders.Remove(uid);
        }

        private void OnWelderGetState(EntityUid uid, WelderComponent welder, ref ComponentGetState args)
        {
            var (fuel, capacity) = GetWelderFuelAndCapacity(uid, welder);
            if (TryComp<ItemToggleComponent>(uid, out var itemToggleComp))
            {
                args.State = new WelderComponentState(capacity.Float(), fuel.Float(), itemToggleComp.Activated);
            }
        }

        private void UpdateWelders(float frameTime)
        {
            _welderTimer += frameTime;

            if (_welderTimer < WelderUpdateTimer)
                return;

            // TODO Use an "active welder" component instead, EntityQuery over that.
            foreach (var tool in _activeWelders.ToArray())
            {
                if (!EntityManager.TryGetComponent(tool, out WelderComponent? welder)
                    || !EntityManager.TryGetComponent(tool, out SolutionContainerManagerComponent? solutionContainer)
                    || !EntityManager.TryGetComponent(tool, out TransformComponent? transform))
                    continue;

                if (!_solutionContainerSystem.TryGetSolution(tool, welder.FuelSolution, out var solution, solutionContainer))
                    continue;

                solution.RemoveReagent(welder.FuelReagent, welder.FuelConsumption * _welderTimer);

                if (solution.GetTotalPrototypeQuantity(welder.FuelReagent) <= FixedPoint2.Zero)
                {
                    var ev = new ItemToggleForceToggleEvent();
                    RaiseLocalEvent(tool, ref ev);
                }

                Dirty(tool, welder);
            }
            _welderTimer -= WelderUpdateTimer;
        }
    }

    public sealed class WelderToggledEvent : EntityEventArgs
    {
        public bool WelderOn;

        public WelderToggledEvent(bool welderOn)
        {
            WelderOn = welderOn;
        }
    }
}
