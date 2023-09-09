using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Cargo.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared.Actions;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Toggleable;
using Content.Shared.Examine;
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
        [Dependency] private readonly SharedAudioSystem _audioSys = default!;
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
            SubscribeLocalEvent<GasTankComponent, EntParentChangedMessage>(OnParentChange);
            SubscribeLocalEvent<GasTankComponent, GasTankSetPressureMessage>(OnGasTankSetPressure);
            SubscribeLocalEvent<GasTankComponent, GasTankToggleInternalsMessage>(OnGasTankToggleInternals);
            SubscribeLocalEvent<GasTankComponent, GasAnalyzerScanEvent>(OnAnalyzed);
            SubscribeLocalEvent<GasTankComponent, PriceCalculationEvent>(OnGasTankPrice);
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
            _ui.TrySetUiState(component.Owner, SharedGasTankUiKey.Key,
                new GasTankBoundUserInterfaceState
                {
                    TankPressure = component.Air?.Pressure ?? 0,
                    OutputPressure = initialUpdate ? component.OutputPressure : null,
                    InternalsConnected = component.IsConnected,
                    CanConnectInternals = CanConnectToInternals(component)
                });
        }

        private void BeforeUiOpen(EntityUid uid, GasTankComponent component, BeforeActivatableUIOpenEvent args)
        {
            // Only initial update includes output pressure information, to avoid overwriting client-input as the updates come in.
            UpdateUserInterface(component, true);
        }

        private void OnParentChange(EntityUid uid, GasTankComponent component, ref EntParentChangedMessage args)
        {
            // When an item is moved from hands -> pockets, the container removal briefly dumps the item on the floor.
            // So this is a shitty fix, where the parent check is just delayed. But this really needs to get fixed
            // properly at some point.
            component.CheckUser = true;
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
                if (gasTank.CheckUser)
                {
                    gasTank.CheckUser = false;
                    if (Transform(gasTank.Owner).ParentUid != gasTank.User)
                    {
                        DisconnectFromInternals(gasTank);
                        continue;
                    }
                }

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
            var internals = GetInternalsComponent(component);
            return internals != null && internals.BreathToolEntity != null;
        }

        public void ConnectToInternals(GasTankComponent component)
        {
            if (component.IsConnected || !CanConnectToInternals(component))
                return;

            var internals = GetInternalsComponent(component);
            if (internals == null)
                return;

            if (_internals.TryConnectTank(internals, component.Owner))
                component.User = internals.Owner;

            _actions.SetToggled(component.ToggleAction, component.IsConnected);

            // Couldn't toggle!
            if (!component.IsConnected)
                return;

            component.ConnectStream?.Stop();

            if (component.ConnectSound != null)
                component.ConnectStream = _audioSys.PlayPvs(component.ConnectSound, component.Owner);

            UpdateUserInterface(component);
        }

        public void DisconnectFromInternals(GasTankComponent component)
        {
            if (component.User == null)
                return;

            var internals = GetInternalsComponent(component);
            component.User = null;

            _actions.SetToggled(component.ToggleAction, false);

            _internals.DisconnectTank(internals);
            component.DisconnectStream?.Stop();

            if (component.DisconnectSound != null)
                component.DisconnectStream = _audioSys.PlayPvs(component.DisconnectSound, component.Owner);

            UpdateUserInterface(component);
        }

        private InternalsComponent? GetInternalsComponent(GasTankComponent component, EntityUid? owner = null)
        {
            owner ??= component.User;
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
                var range = MathF.Sqrt((pressure - component.TankFragmentPressure) / component.TankFragmentScale);

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

                    _audioSys.Play(component.RuptureSound, Filter.Pvs(component.Owner), Transform(component.Owner).Coordinates, true, AudioParams.Default.WithVariation(0.125f));

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

        /// <summary>
        /// Returns the gas mixture for the gas analyzer
        /// </summary>
        private void OnAnalyzed(EntityUid uid, GasTankComponent component, GasAnalyzerScanEvent args)
        {
            args.GasMixtures = new Dictionary<string, GasMixture?> { {Name(uid), component.Air} };
        }

        private void OnGasTankPrice(EntityUid uid, GasTankComponent component, ref PriceCalculationEvent args)
        {
            args.Price += _atmosphereSystem.GetPrice(component.Air);
        }
    }
}
