using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Tools.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
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
using Robust.Shared.Utility;

namespace Content.Server.Tools
{
    public sealed partial class ToolSystem
    {
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
            SubscribeLocalEvent<WelderComponent, DoAfterAttemptEvent<ToolDoAfterEvent>>(OnWelderToolUseAttempt);
            SubscribeLocalEvent<WelderComponent, ComponentShutdown>(OnWelderShutdown);
            SubscribeLocalEvent<WelderComponent, ComponentGetState>(OnWelderGetState);
            SubscribeLocalEvent<WelderComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        }

        private void OnGetMeleeDamage(EntityUid uid, WelderComponent component, ref GetMeleeDamageEvent args)
        {
            if (component.Lit)
                args.Damage += component.LitMeleeDamageBonus;
        }

        public (FixedPoint2 fuel, FixedPoint2 capacity) GetWelderFuelAndCapacity(EntityUid uid, WelderComponent? welder = null, SolutionContainerManagerComponent? solutionContainer = null)
        {
            if (!Resolve(uid, ref welder, ref solutionContainer)
                || !_solutionContainerSystem.TryGetSolution(uid, welder.FuelSolution, out var fuelSolution, solutionContainer))
                return (FixedPoint2.Zero, FixedPoint2.Zero);

            return (_solutionContainerSystem.GetTotalPrototypeQuantity(uid, welder.FuelReagent), fuelSolution.MaxVolume);
        }

        public bool TryToggleWelder(EntityUid uid, EntityUid? user,
            WelderComponent? welder = null,
            SolutionContainerManagerComponent? solutionContainer = null,
            ItemComponent? item = null,
            SharedPointLightComponent? light = null,
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
            SharedPointLightComponent? light = null,
            AppearanceComponent? appearance = null,
            TransformComponent? transform = null)
        {
            if (!Resolve(uid, ref welder, ref solutionContainer, ref transform))
                return false;

            // Optional components.
            Resolve(uid, ref item, ref appearance, false);

            _light.ResolveLight(uid, ref light);

            if (!_solutionContainerSystem.TryGetSolution(uid, welder.FuelSolution, out var solution, solutionContainer))
                return false;

            var fuel = solution.GetTotalPrototypeQuantity(welder.FuelReagent);

            // Not enough fuel to lit welder.
            if (fuel == FixedPoint2.Zero || fuel < welder.FuelLitCost)
            {
                if(user != null)
                    _popupSystem.PopupEntity(Loc.GetString("welder-component-no-fuel-message"), uid, user.Value);
                return false;
            }

            solution.RemoveReagent(welder.FuelReagent, welder.FuelLitCost);

            welder.Lit = true;

            // Logging
            if (user != null)
                _adminLogger.Add(LogType.InteractActivate, LogImpact.Low, $"{ToPrettyString(user.Value):user} toggled {ToPrettyString(uid):welder} on");
            else
                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(uid):welder} toggled on");

            var ev = new WelderToggledEvent(true);
            RaiseLocalEvent(welder.Owner, ev, false);

            var hotEvent = new IsHotEvent() {IsHot = true};
            RaiseLocalEvent(uid, hotEvent);

            _appearanceSystem.SetData(uid, WelderVisuals.Lit, true);
            _appearanceSystem.SetData(uid, ToggleableLightVisuals.Enabled, true);

            if (light != null)
            {
                _light.SetEnabled(uid, true, light);
            }

            _audioSystem.PlayPvs(welder.WelderOnSounds, uid, AudioParams.Default.WithVariation(0.125f).WithVolume(-5f));

            if (transform.GridUid is {} gridUid)
            {
                var position = _transformSystem.GetGridOrMapTilePosition(uid, transform);
                _atmosphereSystem.HotspotExpose(gridUid, position, 700, 50, uid, true);
            }

            Dirty(uid, welder);

            _activeWelders.Add(uid);
            return true;
        }

        public bool TryTurnWelderOff(EntityUid uid, EntityUid? user,
            WelderComponent? welder = null,
            ItemComponent? item = null,
            SharedPointLightComponent? light = null,
            AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref welder))
                return false;

            // Optional components.
            Resolve(uid, ref item, ref appearance, false);

            _light.ResolveLight(uid, ref light);

            welder.Lit = false;

            // Logging
            if (user != null)
                _adminLogger.Add(LogType.InteractActivate, LogImpact.Low, $"{ToPrettyString(user.Value):user} toggled {ToPrettyString(uid):welder} off");
            else
                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(uid):welder} toggled off");

            var ev = new WelderToggledEvent(false);
            RaiseLocalEvent(uid, ev, false);

            var hotEvent = new IsHotEvent() {IsHot = false};
            RaiseLocalEvent(uid, hotEvent);

            // Layer 1 is the flame.
            _appearanceSystem.SetData(uid, WelderVisuals.Lit, false);
            _appearanceSystem.SetData(uid, ToggleableLightVisuals.Enabled, false);

            if (light != null)
            {
                _light.SetEnabled(uid, false, light);
            }

            _audioSystem.PlayPvs(welder.WelderOffSounds, uid, AudioParams.Default.WithVariation(0.125f).WithVolume(-5f));

            Dirty(uid, welder);

            _activeWelders.Remove(uid);
            return true;
        }

        private void OnWelderStartup(EntityUid uid, WelderComponent welder, ComponentStartup args)
        {
            // TODO: Delete this shit what
            Dirty(welder);
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
            // TODO what
            // ????
            Dirty(welder);
        }

        private void OnWelderActivate(EntityUid uid, WelderComponent welder, ActivateInWorldEvent args)
        {
            args.Handled = TryToggleWelder(uid, args.User, welder);
            if (args.Handled)
                args.WasLogged = true;
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
                var trans = FixedPoint2.Min(welderSolution.AvailableVolume, targetSolution.Volume);
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

        private void OnWelderToolUseAttempt(EntityUid uid, WelderComponent welder, DoAfterAttemptEvent<ToolDoAfterEvent> args)
        {
            var user = args.DoAfter.Args.User;

            if (!welder.Lit)
            {
                _popupSystem.PopupEntity(Loc.GetString("welder-component-welder-not-lit-message"), uid, user);
                args.Cancel();
                return;
            }
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

                solution.RemoveReagent(welder.FuelReagent, welder.FuelConsumption * _welderTimer);

                if (solution.GetTotalPrototypeQuantity(welder.FuelReagent) <= FixedPoint2.Zero)
                    TryTurnWelderOff(tool, null, welder);

                Dirty(welder);
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
