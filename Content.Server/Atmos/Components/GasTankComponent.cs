using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
#pragma warning disable 618
    public sealed class GasTankComponent : Component, IExamine, IGasMixtureHolder, IDropped, IActivate
#pragma warning restore 618
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        private const float MaxExplosionRange = 14f;
        private const float DefaultOutputPressure = Atmospherics.OneAtmosphere;

        private int _integrity = 3;

        [Dependency] private readonly IEntityManager _entityManager = default!;

        [ViewVariables] private BoundUserInterface? _userInterface;

        [DataField("ruptureSound")] private SoundSpecifier _ruptureSound = new SoundPathSpecifier("Audio/Effects/spray.ogg");

        [DataField("air")] [ViewVariables] public GasMixture Air { get; set; } = new();

        /// <summary>
        ///     Distributed pressure.
        /// </summary>
        [DataField("outputPressure")]
        [ViewVariables]
        public float OutputPressure { get; private set; } = DefaultOutputPressure;

        /// <summary>
        ///     Tank is connected to internals.
        /// </summary>
        [ViewVariables] public bool IsConnected { get; set; }

        /// <summary>
        ///     Represents that tank is functional and can be connected to internals.
        /// </summary>
        public bool IsFunctional => GetInternalsComponent() != null;

        /// <summary>
        ///     Pressure at which tanks start leaking.
        /// </summary>
        [DataField("tankLeakPressure")]
        public float TankLeakPressure { get; set; }     = 30 * Atmospherics.OneAtmosphere;

        /// <summary>
        ///     Pressure at which tank spills all contents into atmosphere.
        /// </summary>
        [DataField("tankRupturePressure")]
        public float TankRupturePressure { get; set; }  = 40 * Atmospherics.OneAtmosphere;

        /// <summary>
        ///     Base 3x3 explosion.
        /// </summary>
        [DataField("tankFragmentPressure")]
        public float TankFragmentPressure { get; set; } = 50 * Atmospherics.OneAtmosphere;

        /// <summary>
        ///     Increases explosion for each scale kPa above threshold.
        /// </summary>
        [DataField("tankFragmentScale")]
        public float TankFragmentScale { get; set; }    = 10 * Atmospherics.OneAtmosphere;

        [DataField("toggleAction", required: true)]
        public InstantAction ToggleAction = new();

        protected override void Initialize()
        {
            base.Initialize();
            _userInterface = Owner.GetUIOrNull(SharedGasTankUiKey.Key);
            if (_userInterface != null)
            {
                _userInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }
        }

        public void OpenInterface(IPlayerSession session)
        {
            _userInterface?.Open(session);
            UpdateUserInterface(true);
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("comp-gas-tank-examine", ("pressure", Math.Round(Air?.Pressure ?? 0))));
            if (IsConnected)
            {
                message.AddText("\n");
                message.AddMarkup(Loc.GetString("comp-gas-tank-connected"));
            }
        }

        protected override void Shutdown()
        {
            base.Shutdown();
            DisconnectFromInternals();
        }

        public GasMixture? RemoveAir(float amount)
        {
            var gas = Air?.Remove(amount);
            CheckStatus();
            return gas;
        }

        public GasMixture RemoveAirVolume(float volume)
        {
            if (Air == null)
                return new GasMixture(volume);

            var tankPressure = Air.Pressure;
            if (tankPressure < OutputPressure)
            {
                OutputPressure = tankPressure;
                UpdateUserInterface();
            }

            var molesNeeded = OutputPressure * volume / (Atmospherics.R * Air.Temperature);

            var air = RemoveAir(molesNeeded);

            if (air != null)
                air.Volume = volume;
            else
                return new GasMixture(volume);

            return air;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!_entMan.TryGetComponent(eventArgs.User, out ActorComponent? actor)) return;
            OpenInterface(actor.PlayerSession);
        }

        public void ConnectToInternals()
        {
            if (IsConnected || !IsFunctional) return;
            var internals = GetInternalsComponent();
            if (internals == null) return;
            IsConnected = internals.TryConnectTank(Owner);
            EntitySystem.Get<SharedActionsSystem>().SetToggled(ToggleAction, IsConnected);
            UpdateUserInterface();
        }

        public void DisconnectFromInternals(EntityUid? owner = null)
        {
            if (!IsConnected) return;
            IsConnected = false;
            EntitySystem.Get<SharedActionsSystem>().SetToggled(ToggleAction, false);
            GetInternalsComponent(owner)?.DisconnectTank();
            UpdateUserInterface();
        }

        public void UpdateUserInterface(bool initialUpdate = false)
        {
            var internals = GetInternalsComponent();
            _userInterface?.SetState(
                new GasTankBoundUserInterfaceState
                {
                    TankPressure = Air?.Pressure ?? 0,
                    OutputPressure = initialUpdate ? OutputPressure : null,
                    InternalsConnected = IsConnected,
                    CanConnectInternals = IsFunctional && internals != null
                });
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case GasTankSetPressureMessage msg:
                    OutputPressure = msg.Pressure;
                    break;
                case GasTankToggleInternalsMessage _:
                    ToggleInternals();
                    break;
            }
        }

        internal void ToggleInternals()
        {
            if (IsConnected)
            {
                DisconnectFromInternals();
                return;
            }

            ConnectToInternals();
        }

        private InternalsComponent? GetInternalsComponent(EntityUid? owner = null)
        {
            if (_entMan.Deleted(Owner)) return null;
            if (owner != null) return _entMan.GetComponentOrNull<InternalsComponent>(owner.Value);
            return Owner.TryGetContainer(out var container)
                ? _entMan.GetComponentOrNull<InternalsComponent>(container.Owner)
                : null;
        }

        public void AssumeAir(GasMixture giver)
        {
            EntitySystem.Get<AtmosphereSystem>().Merge(Air, giver);
            CheckStatus();
        }

        public void CheckStatus()
        {
            if (Air == null)
                return;

            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            var pressure = Air.Pressure;

            if (pressure > TankFragmentPressure)
            {
                // Give the gas a chance to build up more pressure.
                for (var i = 0; i < 3; i++)
                {
                    atmosphereSystem.React(Air, this);
                }

                pressure = Air.Pressure;
                var range = (pressure - TankFragmentPressure) / TankFragmentScale;

                // Let's cap the explosion, yeah?
                // !1984
                if (range > MaxExplosionRange)
                {
                    range = MaxExplosionRange;
                }

                EntitySystem.Get<ExplosionSystem>().TriggerExplosive(Owner, radius: range);

                return;
            }

            if (pressure > TankRupturePressure)
            {
                if (_integrity <= 0)
                {
                    var environment = atmosphereSystem.GetTileMixture(_entMan.GetComponent<TransformComponent>(Owner).Coordinates, true);
                    if(environment != null)
                        atmosphereSystem.Merge(environment, Air);

                    SoundSystem.Play(Filter.Pvs(Owner), _ruptureSound.GetSound(), _entMan.GetComponent<TransformComponent>(Owner).Coordinates, AudioHelpers.WithVariation(0.125f));

                    _entMan.QueueDeleteEntity(Owner);
                    return;
                }

                _integrity--;
                return;
            }

            if (pressure > TankLeakPressure)
            {
                if (_integrity <= 0)
                {
                    var environment = atmosphereSystem.GetTileMixture(_entMan.GetComponent<TransformComponent>(Owner).Coordinates, true);
                    if (environment == null)
                        return;

                    var leakedGas = Air.RemoveRatio(0.25f);
                    atmosphereSystem.Merge(environment, leakedGas);
                }
                else
                {
                    _integrity--;
                }

                return;
            }

            if (_integrity < 3)
                _integrity++;
        }

        void IDropped.Dropped(DroppedEventArgs eventArgs)
        {
            DisconnectFromInternals(eventArgs.User);
        }
    }
}
