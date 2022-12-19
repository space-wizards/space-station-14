using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Tools.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Temperature;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Server.Tools
{
    public sealed partial class ToolSystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

        private readonly HashSet<EntityUid> _activeWelders = new();

        private const float WelderUpdateTimer = 1f;
        private float _welderTimer = 0f;

        public void InitializeWelders()
        {
            SubscribeLocalEvent<WelderComponent, ComponentStartup>(OnWelderStartup);
            SubscribeLocalEvent<WelderComponent, IsHotEvent>(OnWelderIsHotEvent);
            SubscribeLocalEvent<WelderComponent, ExaminedEvent>(OnWelderExamine);
            SubscribeLocalEvent<WelderComponent, SolutionChangedEvent>(OnWelderSolutionChange);
            SubscribeLocalEvent<WelderComponent, ActivateInWorldEvent>(OnWelderActivate);
            SubscribeLocalEvent<WelderComponent, AfterInteractEvent>(OnWelderAfterInteract);
            SubscribeLocalEvent<WelderComponent, ToolUseAttemptEvent>(OnWelderToolUseAttempt);
            SubscribeLocalEvent<WelderComponent, ToolUseFinishAttemptEvent>(OnWelderToolUseFinishAttempt);
            SubscribeLocalEvent<WelderComponent, ComponentShutdown>(OnWelderShutdown);
            SubscribeLocalEvent<WelderComponent, ComponentGetState>(OnWelderGetState);
            SubscribeLocalEvent<WelderComponent, MeleeHitEvent>(OnMeleeHit);
        }

        private void OnMeleeHit(EntityUid uid, WelderComponent component, MeleeHitEvent args)
        {
            if (!args.Handled && component.Lit)
                args.BonusDamage += component.LitMeleeDamageBonus;
        }

        public (FixedPoint2 fuel, FixedPoint2 capacity) GetWelderFuelAndCapacity(EntityUid uid, WelderComponent? welder = null, SolutionContainerManagerComponent? solutionContainer = null)
        {
            if (!Resolve(uid, ref welder, ref solutionContainer)
                || !_solutionContainerSystem.TryGetSolution(uid, welder.FuelSolution, out var fuelSolution, solutionContainer))
                return (FixedPoint2.Zero, FixedPoint2.Zero);

            return (_solutionContainerSystem.GetReagentQuantity(uid, welder.FuelReagent), fuelSolution.MaxVolume);
        }

        public bool TryToggleWelder(EntityUid uid, EntityUid? user,
            WelderComponent? welder = null,
            SolutionContainerManagerComponent? solutionContainer = null,
            ItemComponent? item = null,
            PointLightComponent? light = null,
            AppearanceComponent? appearance = null)
        {
            // Right now, we only need the welder.
            // So let's not unnecessarily resolve components
            if (!Resolve(uid, ref welder))
                return false;

            return !welder.Lit
                ? TryTurnWelderOn(uid, user, welder, solutionContainer, item, light, appearance)
                : TryTurnWelderOff(uid, user, welder, item, light, appearance);
        }

        public bool TryTurnWelderOn(EntityUid uid, EntityUid? user,
            WelderComponent? welder = null,
            SolutionContainerManagerComponent? solutionContainer = null,
            ItemComponent? item = null,
            PointLightComponent? light = null,
            AppearanceComponent? appearance = null,
            TransformComponent? transform = null)
        {
            if (!Resolve(uid, ref welder, ref solutionContainer, ref transform))
                return false;

            // Optional components.
            Resolve(uid, ref item, ref light, ref appearance, false);

            if (!_solutionContainerSystem.TryGetSolution(uid, welder.FuelSolution, out var solution, solutionContainer))
                return false;

            var fuel = solution.GetReagentQuantity(welder.FuelReagent);

            // Not enough fuel to lit welder.
            if (fuel == FixedPoint2.Zero || fuel < welder.FuelLitCost)
            {
                if(user != null)
                    _popupSystem.PopupEntity(Loc.GetString("welder-component-no-fuel-message"), uid, user.Value);
                return false;
            }

            solution.RemoveReagent(welder.FuelReagent, welder.FuelLitCost);

            welder.Lit = true;

            var ev = new WelderToggledEvent(true);
            RaiseLocalEvent(welder.Owner, ev, false);

            _appearanceSystem.SetData(uid, WelderVisuals.Lit, true);
            _appearanceSystem.SetData(uid, ToggleableLightVisuals.Enabled, true);

            if (light != null)
                light.Enabled = true;

            _audioSystem.PlayPvs(welder.WelderOnSounds, uid, AudioParams.Default.WithVariation(0.125f).WithVolume(-5f));

            if (transform.GridUid is {} gridUid)
            {
                var position = _transformSystem.GetGridOrMapTilePosition(uid, transform);
                _atmosphereSystem.HotspotExpose(gridUid, position, 700, 50, true);
            }

            _entityManager.Dirty(welder);

            _activeWelders.Add(uid);
            return true;
        }

        public bool TryTurnWelderOff(EntityUid uid, EntityUid? user,
            WelderComponent? welder = null,
            ItemComponent? item = null,
            PointLightComponent? light = null,
            AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref welder))
                return false;

            // Optional components.
            Resolve(uid, ref item, ref light, ref appearance, false);

            welder.Lit = false;

            var ev = new WelderToggledEvent(false);
            RaiseLocalEvent(welder.Owner, ev, false);

            // Layer 1 is the flame.
            _appearanceSystem.SetData(uid, WelderVisuals.Lit, false);
            _appearanceSystem.SetData(uid, ToggleableLightVisuals.Enabled, false);

            if (light != null)
                light.Enabled = false;

            _audioSystem.PlayPvs(welder.WelderOffSounds, uid, AudioParams.Default.WithVariation(0.125f).WithVolume(-5f));

            _entityManager.Dirty(welder);

            _activeWelders.Remove(uid);
            return true;
        }

        private void OnWelderStartup(EntityUid uid, WelderComponent welder, ComponentStartup args)
        {
            _entityManager.Dirty(welder);
        }

        private void OnWelderIsHotEvent(EntityUid uid, WelderComponent welder, IsHotEvent args)
        {
            args.IsHot = welder.Lit;
        }

        private void OnWelderExamine(EntityUid uid, WelderComponent welder, ExaminedEvent args)
        {
            if (welder.Lit)
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

        private void OnWelderSolutionChange(EntityUid uid, WelderComponent welder, SolutionChangedEvent args)
        {
            _entityManager.Dirty(welder);
        }

        private void OnWelderActivate(EntityUid uid, WelderComponent welder, ActivateInWorldEvent args)
        {
            args.Handled = TryToggleWelder(uid, args.User, welder);
        }

        private void OnWelderAfterInteract(EntityUid uid, WelderComponent welder, AfterInteractEvent args)
        {
            if (args.Handled)
                return;

            if (args.Target is not {Valid: true} target || !args.CanReach)
                return;

            // TODO: Clean up this inherited oldcode.

            if (EntityManager.TryGetComponent(target, out ReagentTankComponent? tank)
                && tank.TankType == ReagentTankType.Fuel
                && _solutionContainerSystem.TryGetDrainableSolution(target, out var targetSolution)
                && _solutionContainerSystem.TryGetSolution(uid, welder.FuelSolution, out var welderSolution))
            {
                var trans = FixedPoint2.Min(welderSolution.AvailableVolume, targetSolution.DrainAvailable);
                if (trans > 0)
                {
                    var drained = _solutionContainerSystem.Drain(target, targetSolution,  trans);
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

        private void OnWelderToolUseAttempt(EntityUid uid, WelderComponent welder, ToolUseAttemptEvent args)
        {
            if (args.Cancelled)
                return;

            if (!welder.Lit)
            {
                _popupSystem.PopupEntity(Loc.GetString("welder-component-welder-not-lit-message"), uid, args.User);
                args.Cancel();
                return;
            }

            var (fuel, _) = GetWelderFuelAndCapacity(uid, welder);

            if (FixedPoint2.New(args.Fuel) > fuel)
            {
                _popupSystem.PopupEntity(Loc.GetString("welder-component-cannot-weld-message"), uid, args.User);
                args.Cancel();
                return;
            }
        }

        private void OnWelderToolUseFinishAttempt(EntityUid uid, WelderComponent welder, ToolUseFinishAttemptEvent args)
        {
            if (args.Cancelled)
                return;

            if (!welder.Lit)
            {
                _popupSystem.PopupEntity(Loc.GetString("welder-component-welder-not-lit-message"), uid, args.User);
                args.Cancel();
                return;
            }

            var (fuel, _) = GetWelderFuelAndCapacity(uid, welder);

            var neededFuel = FixedPoint2.New(args.Fuel);

            if (neededFuel > fuel)
            {
                args.Cancel();
            }

            if (!_solutionContainerSystem.TryGetSolution(uid, welder.FuelSolution, out var solution))
            {
                args.Cancel();
                return;
            }

            solution.RemoveReagent(welder.FuelReagent, neededFuel);
            _entityManager.Dirty(welder);
        }

        private void OnWelderShutdown(EntityUid uid, WelderComponent welder, ComponentShutdown args)
        {
            _activeWelders.Remove(uid);
        }

        private void OnWelderGetState(EntityUid uid, WelderComponent welder, ref ComponentGetState args)
        {
            var (fuel, capacity) = GetWelderFuelAndCapacity(uid, welder);
            args.State = new WelderComponentState(capacity.Float(), fuel.Float(), welder.Lit);
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

                if (transform.GridUid is { } gridUid)
                {
                    var position = _transformSystem.GetGridOrMapTilePosition(tool, transform);
                    _atmosphereSystem.HotspotExpose(gridUid, position, 700, 50, true);
                }

                solution.RemoveReagent(welder.FuelReagent, welder.FuelConsumption * _welderTimer);

                if (solution.GetReagentQuantity(welder.FuelReagent) <= FixedPoint2.Zero)
                    TryTurnWelderOff(tool, null, welder);

                _entityManager.Dirty(welder);
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
