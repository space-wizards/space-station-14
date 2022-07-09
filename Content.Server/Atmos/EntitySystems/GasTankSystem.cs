using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared.Actions;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Audio;
using Content.Shared.Interaction.Events;
using Content.Shared.Toggleable;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasTankSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly ExplosionSystem _explosions = default!;
        [Dependency] private readonly InternalsSystem _internals = default!;
        [Dependency] private readonly SharedContainerSystem _containers = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;

        private const float TimerDelay = 0.5f;
        private float _timer = 0f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GasTankComponent, ComponentShutdown>(OnGasShutdown);
            SubscribeLocalEvent<GasTankComponent, BeforeActivatableUIOpenEvent>(BeforeUiOpen);
            SubscribeLocalEvent<GasTankComponent, GetItemActionsEvent>(OnGetActions);
            SubscribeLocalEvent<GasTankComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<GasTankComponent, ToggleActionEvent>(OnActionToggle);
            SubscribeLocalEvent<GasTankComponent, DroppedEvent>(OnDropped);

            SubscribeLocalEvent<GasTankComponent, GasTankSetPressureMessage>(OnGasTankSetPressure);
            SubscribeLocalEvent<GasTankComponent, GasTankToggleInternalsMessage>(OnGasTankToggleInternals);
        }

        private void OnGasShutdown(EntityUid uid, GasTankComponent component, ComponentShutdown args)
        {
            DisconnectFromInternals(component);
        }

        private void OnGasTankToggleInternals(EntityUid uid, GasTankComponent component, GasTankToggleInternalsMessage args)
        {
            if (args.Session is not IPlayerSession playerSession ||
                playerSession.AttachedEntity is not {} player) return;

            ToggleInternals(component);
        }

        private void OnGasTankSetPressure(EntityUid uid, GasTankComponent component, GasTankSetPressureMessage args)
        {
            component.OutputPressure = args.Pressure;
        }

        public void UpdateUserInterface(GasTankComponent component, bool initialUpdate = false)
        {
            var internals = GetInternalsComponent(component);
            _ui.GetUiOrNull(component.Owner, SharedGasTankUiKey.Key)?.SetState(
                new GasTankBoundUserInterfaceState
                {
                    TankPressure = component.Air?.Pressure ?? 0,
                    OutputPressure = initialUpdate ? component.OutputPressure : null,
                    InternalsConnected = component.IsConnected,
                    CanConnectInternals = IsFunctional(component) && internals != null
                });
        }

        private void BeforeUiOpen(EntityUid uid, GasTankComponent component, BeforeActivatableUIOpenEvent args)
        {
            // Only initial update includes output pressure information, to avoid overwriting client-input as the updates come in.
            UpdateUserInterface(component, true);
        }

        private void OnDropped(EntityUid uid, GasTankComponent component, DroppedEvent args)
        {
            DisconnectFromInternals(component, args.User);
        }

        private void OnGetActions(EntityUid uid, GasTankComponent component, GetItemActionsEvent args)
        {
            args.Actions.Add(component.ToggleAction);
        }

        private void OnExamined(EntityUid uid, GasTankComponent component, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
                args.PushMarkup(Loc.GetString("comp-gas-tank-examine", ("pressure", Math.Round(component.Air?.Pressure ?? 0))));
            if (component.IsConnected)
                args.PushMarkup(Loc.GetString("comp-gas-tank-connected"));
        }

        private void OnActionToggle(EntityUid uid, GasTankComponent component, ToggleActionEvent args)
        {
            if (args.Handled)
                return;

            ToggleInternals(component);
            args.Handled = true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _timer += frameTime;

            if (_timer < TimerDelay) return;
            _timer -= TimerDelay;

            foreach (var gasTank in EntityManager.EntityQuery<GasTankComponent>())
            {
                _atmosphereSystem.React(gasTank.Air, gasTank);
                CheckStatus(gasTank);
                if (_ui.IsUiOpen(gasTank.Owner, SharedGasTankUiKey.Key))
                {
                    UpdateUserInterface(gasTank);
                }
            }
        }

        private void ToggleInternals(GasTankComponent component)
        {
            if (component.IsConnected)
            {
                DisconnectFromInternals(component);
            }
            else
            {
                ConnectToInternals(component);
            }
        }

        public GasMixture? RemoveAir(GasTankComponent component, float amount)
        {
            var gas = component.Air?.Remove(amount);
            CheckStatus(component);
            return gas;
        }

        public GasMixture RemoveAirVolume(GasTankComponent component, float volume)
        {
            if (component.Air == null)
                return new GasMixture(volume);

            var tankPressure = component.Air.Pressure;
            if (tankPressure < component.OutputPressure)
            {
                component.OutputPressure = tankPressure;
                UpdateUserInterface(component);
            }

            var molesNeeded = component.OutputPressure * volume / (Atmospherics.R * component.Air.Temperature);

            var air = RemoveAir(component, molesNeeded);

            if (air != null)
                air.Volume = volume;
            else
                return new GasMixture(volume);

            return air;
        }

        public bool CanConnectToInternals(GasTankComponent component)
        {
            return !component.IsConnected && IsFunctional(component);
        }

        public void ConnectToInternals(GasTankComponent component)
        {
            if (!CanConnectToInternals(component)) return;
            var internals = GetInternalsComponent(component);
            if (internals == null) return;
            component.IsConnected = _internals.TryConnectTank(internals, component.Owner);
            _actions.SetToggled(component.ToggleAction, component.IsConnected);

            // Couldn't toggle!
            if (!component.IsConnected) return;

            component.ConnectStream?.Stop();

            if (component.ConnectSound != null)
                component.ConnectStream = SoundSystem.Play(component.ConnectSound.GetSound(), Filter.Pvs(component.Owner, entityManager: EntityManager), component.Owner, component.ConnectSound.Params);

            UpdateUserInterface(component);
        }

        public void DisconnectFromInternals(GasTankComponent component, EntityUid? owner = null)
        {
            if (!component.IsConnected) return;
            component.IsConnected = false;
            _actions.SetToggled(component.ToggleAction, false);

            _internals.DisconnectTank(GetInternalsComponent(component, owner));
            component.DisconnectStream?.Stop();

            if (component.DisconnectSound != null)
                component.DisconnectStream = SoundSystem.Play(component.DisconnectSound.GetSound(), Filter.Pvs(component.Owner, entityManager: EntityManager), component.Owner, component.DisconnectSound.Params);

            UpdateUserInterface(component);
        }

        private InternalsComponent? GetInternalsComponent(GasTankComponent component, EntityUid? owner = null)
        {
            if (Deleted(component.Owner)) return null;
            if (owner != null) return CompOrNull<InternalsComponent>(owner.Value);
            return _containers.TryGetContainingContainer(component.Owner, out var container)
                ? CompOrNull<InternalsComponent>(container.Owner)
                : null;
        }

        public void AssumeAir(GasTankComponent component, GasMixture giver)
        {
            _atmosphereSystem.Merge(component.Air, giver);
            CheckStatus(component);
        }

        public void CheckStatus(GasTankComponent component)
        {
            if (component.Air == null)
                return;

            var pressure = component.Air.Pressure;

            if (pressure > component.TankFragmentPressure)
            {
                // Give the gas a chance to build up more pressure.
                for (var i = 0; i < 3; i++)
                {
                    _atmosphereSystem.React(component.Air, component);
                }

                pressure = component.Air.Pressure;
                var range = (pressure - component.TankFragmentPressure) / component.TankFragmentScale;

                // Let's cap the explosion, yeah?
                // !1984
                if (range > GasTankComponent.MaxExplosionRange)
                {
                    range = GasTankComponent.MaxExplosionRange;
                }

                _explosions.TriggerExplosive(component.Owner, radius: range);

                return;
            }

            if (pressure > component.TankRupturePressure)
            {
                if (component.Integrity <= 0)
                {
                    var environment = _atmosphereSystem.GetContainingMixture(component.Owner, false, true);
                    if(environment != null)
                        _atmosphereSystem.Merge(environment, component.Air);

                    SoundSystem.Play(component.RuptureSound.GetSound(), Filter.Pvs(component.Owner), Transform(component.Owner).Coordinates, AudioHelpers.WithVariation(0.125f));

                    QueueDel(component.Owner);
                    return;
                }

                component.Integrity--;
                return;
            }

            if (pressure > component.TankLeakPressure)
            {
                if (component.Integrity <= 0)
                {
                    var environment = _atmosphereSystem.GetContainingMixture(component.Owner, false, true);
                    if (environment == null)
                        return;

                    var leakedGas = component.Air.RemoveRatio(0.25f);
                    _atmosphereSystem.Merge(environment, leakedGas);
                }
                else
                {
                    component.Integrity--;
                }

                return;
            }

            if (component.Integrity < 3)
                component.Integrity++;
        }

        private bool IsFunctional(GasTankComponent component)
        {
            return GetInternalsComponent(component) != null;
        }
    }
}
