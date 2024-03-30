using Content.Server.Chemistry.Components;
using Content.Server.IgnitionSource;
using Content.Server.Tools.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Tools.Components;
using Robust.Shared.GameStates;
using System.Linq;
using Content.Shared.Item.ItemToggle.Components;

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
            SubscribeLocalEvent<WelderComponent, ItemToggledEvent>(OnToggle);
            SubscribeLocalEvent<WelderComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        }

        public (FixedPoint2 fuel, FixedPoint2 capacity) GetWelderFuelAndCapacity(EntityUid uid, WelderComponent? welder = null, SolutionContainerManagerComponent? solutionContainer = null)
        {
            if (!Resolve(uid, ref welder, ref solutionContainer)
                || !_solutionContainer.ResolveSolution((uid, solutionContainer), welder.FuelSolutionName, ref welder.FuelSolution, out var fuelSolution))
                return (FixedPoint2.Zero, FixedPoint2.Zero);

            return (fuelSolution.GetTotalPrototypeQuantity(welder.FuelReagent), fuelSolution.MaxVolume);
        }

        private void OnToggle(Entity<WelderComponent> entity, ref ItemToggledEvent args)
        {
            if (args.Activated)
                TurnOn(entity, args.User);
            else
                TurnOff(entity, args.User);
        }

        private void OnActivateAttempt(Entity<WelderComponent> entity, ref ItemToggleActivateAttemptEvent args)
        {
            if (!_solutionContainer.ResolveSolution(entity.Owner, entity.Comp.FuelSolutionName, ref entity.Comp.FuelSolution, out var solution))
            {
                args.Cancelled = true;
                args.Popup = Loc.GetString("welder-component-no-fuel-message");
                return;
            }

            var fuel = solution.GetTotalPrototypeQuantity(entity.Comp.FuelReagent);
            if (fuel == FixedPoint2.Zero || fuel < entity.Comp.FuelLitCost)
            {
                args.Popup = Loc.GetString("welder-component-no-fuel-message");
                args.Cancelled = true;
            }
        }

        public void TurnOn(Entity<WelderComponent> entity, EntityUid? user)
        {
            if (!_solutionContainer.ResolveSolution(entity.Owner, entity.Comp.FuelSolutionName, ref entity.Comp.FuelSolution))
                return;

            _solutionContainer.RemoveReagent(entity.Comp.FuelSolution.Value, entity.Comp.FuelReagent, entity.Comp.FuelLitCost);
            AdminLogger.Add(LogType.InteractActivate, LogImpact.Low,
                $"{ToPrettyString(user):user} toggled {ToPrettyString(entity.Owner):welder} on");

            var xform = Transform(entity);
            if (xform.GridUid is { } gridUid)
            {
                var position = _transformSystem.GetGridOrMapTilePosition(entity.Owner, xform);
                _atmosphereSystem.HotspotExpose(gridUid, position, 700, 50, entity.Owner, true);
            }

            _activeWelders.Add(entity);
        }

        public void TurnOff(Entity<WelderComponent> entity, EntityUid? user)
        {
            AdminLogger.Add(LogType.InteractActivate, LogImpact.Low,
                $"{ToPrettyString(user):user} toggled {ToPrettyString(entity.Owner):welder} off");
            _activeWelders.Remove(entity);
        }

        private void OnWelderExamine(Entity<WelderComponent> entity, ref ExaminedEvent args)
        {
            using (args.PushGroup(nameof(WelderComponent)))
            {
                if (_itemToggle.IsActivated(entity.Owner))
                {
                    args.PushMarkup(Loc.GetString("welder-component-on-examine-welder-lit-message"));
                }
                else
                {
                    args.PushMarkup(Loc.GetString("welder-component-on-examine-welder-not-lit-message"));
                }

                if (args.IsInDetailsRange)
                {
                    var (fuel, capacity) = GetWelderFuelAndCapacity(entity.Owner, entity.Comp);

                    args.PushMarkup(Loc.GetString("welder-component-on-examine-detailed-message",
                        ("colorName", fuel < capacity / FixedPoint2.New(4f) ? "darkorange" : "orange"),
                        ("fuelLeft", fuel),
                        ("fuelCapacity", capacity),
                        ("status", string.Empty))); // Lit status is handled above
                }
            }
        }

        private void OnWelderAfterInteract(Entity<WelderComponent> entity, ref AfterInteractEvent args)
        {
            if (args.Handled)
                return;

            if (args.Target is not { Valid: true } target || !args.CanReach)
                return;

            if (TryComp(target, out ReagentTankComponent? tank)
                && tank.TankType == ReagentTankType.Fuel
                && _solutionContainer.TryGetDrainableSolution(target, out var targetSoln, out var targetSolution)
                && _solutionContainer.ResolveSolution(entity.Owner, entity.Comp.FuelSolutionName, ref entity.Comp.FuelSolution, out var welderSolution))
            {
                var trans = FixedPoint2.Min(welderSolution.AvailableVolume, targetSolution.Volume);
                if (trans > 0)
                {
                    var drained = _solutionContainer.Drain(target, targetSoln.Value, trans);
                    _solutionContainer.TryAddSolution(entity.Comp.FuelSolution.Value, drained);
                    _audio.PlayPvs(entity.Comp.WelderRefill, entity);
                    _popup.PopupEntity(Loc.GetString("welder-component-after-interact-refueled-message"), entity, args.User);
                }
                else if (welderSolution.AvailableVolume <= 0)
                {
                    _popup.PopupEntity(Loc.GetString("welder-component-already-full"), entity, args.User);
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("welder-component-no-fuel-in-tank", ("owner", args.Target)), entity, args.User);
                }

                args.Handled = true;
            }
        }

        private void OnWelderToolUseAttempt(Entity<WelderComponent> entity, ref DoAfterAttemptEvent<ToolDoAfterEvent> args)
        {
            var user = args.DoAfter.Args.User;

            if (!_itemToggle.IsActivated(entity.Owner))
            {
                _popup.PopupEntity(Loc.GetString("welder-component-welder-not-lit-message"), entity, user);
                args.Cancel();
            }
        }

        private void OnWelderShutdown(Entity<WelderComponent> entity, ref ComponentShutdown args)
        {
            _activeWelders.Remove(entity);
        }

        private void OnWelderGetState(Entity<WelderComponent> entity, ref ComponentGetState args)
        {
            var (fuel, capacity) = GetWelderFuelAndCapacity(entity.Owner, entity.Comp);
            args.State = new WelderComponentState(capacity.Float(), fuel.Float());
        }

        private void UpdateWelders(float frameTime)
        {
            _welderTimer += frameTime;

            if (_welderTimer < WelderUpdateTimer)
                return;

            // TODO Serialization. _activeWelders is not serialized.
            // Need to use some "active" component, and EntityQuery over that.
            // Probably best to generalize it to a "ToggleableFuelDrain" component.
            foreach (var tool in _activeWelders.ToArray())
            {
                if (!TryComp(tool, out WelderComponent? welder)
                    || !TryComp(tool, out SolutionContainerManagerComponent? solutionContainer))
                    continue;

                if (!_solutionContainer.ResolveSolution((tool, solutionContainer), welder.FuelSolutionName, ref welder.FuelSolution, out var solution))
                    continue;

                _solutionContainer.RemoveReagent(welder.FuelSolution.Value, welder.FuelReagent, welder.FuelConsumption * _welderTimer);

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
