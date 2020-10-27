#nullable enable
using System;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Body.Respiratory;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.Interfaces;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Atmos.GasTank;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class GasTankComponent : SharedGasTankComponent, IExamine, IGasMixtureHolder, IUnequipped, IUse, IEquipped
    {
    	private const float MaxExplosionRange = 14f;
        private const float DefaultOutputPressure = 303.3f;
        private const float DefaultNozzleArea = 0.00039f;

		private float _pressureResistance;
        private float _distributePressure;

        private int _integrity = 3;
        
        /// <summary>
        /// Tank is functional only in allowed slots
        /// </summary>
        private EquipmentSlotDefines.SlotFlags _allowedSlots;


        [Dependency] private readonly IEntityManager _entityManager = default!;
        [ViewVariables] private BoundUserInterface? _userInterface;

        [ViewVariables] public GasMixture? Air { get; set; }

        /// <summary>
        /// Valve state. When tank is controlled by internals component this has no effect
        /// </summary>
        [ViewVariables] public bool IsOpen { get; private set; }

        /// <summary>
        /// Maximum output pressure when valve is open. When tank is controlled
        /// by internals component it has no effect.
        /// </summary>
        [ViewVariables] public float OutputPressure { get; private set; }

        /// <summary>
        /// Cross-sectional area of nozzle
        /// </summary>
        [ViewVariables] public float NozzleArea { get; private set; }

        /// <summary>
        /// Tank is connected to external system and valve is controlled automatically
        /// </summary>
        [ViewVariables] public bool IsConnected { get; set; }

        /// <summary>
        /// Represents that tank is functional and can be connected to internals
        /// </summary>
        public bool IsFunctional { get; private set; }


        public override void Initialize()
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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction("air", new GasMixture(), x => Air = x, () => Air);
            serializer.DataReadWriteFunction("isOpen", false, x => IsOpen = x, () => IsOpen);
            serializer.DataReadWriteFunction("outputPressure", DefaultOutputPressure, x => OutputPressure = x,
                () => OutputPressure);
            serializer.DataReadWriteFunction("nozzleArea", DefaultNozzleArea, x => NozzleArea = x, () => NozzleArea);
            serializer.DataField(ref _allowedSlots, "allowedSlots",
                EquipmentSlotDefines.SlotFlags.BACKPACK | EquipmentSlotDefines.SlotFlags.POCKET |
                EquipmentSlotDefines.SlotFlags.BELT);
			serializer.DataField(ref _pressureResistance, "pressureResistance", Atmospherics.OneAtmosphere * 5f);
            serializer.DataField(ref _distributePressure, "distributePressure", Atmospherics.OneAtmosphere);
        }

        public void SetValveState(bool valveState)
        {
            if (IsOpen == valveState) return;
            IsOpen = valveState;
            UpdateUserInterface();
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("Pressure: [color=orange]{0}[/color] kPa.",
                Math.Round(Air?.Pressure ?? 0)));
            message.AddMarkup(Loc.GetString("\nValve: [color={0}]{1}[/color]", IsOpen ? "green" : "red",
                IsOpen ? "Open" : "Closed"));
            if (IsConnected)
            {
                message.AddMarkup(Loc.GetString("\nConnected to external component"));
            }
        }

        public void Update(float deltaTime)
        {
        	Air?.React(this);
            CheckStatus();
            EmitContents(deltaTime);
            UpdateUserInterface();
        }

        public void Equipped(EquippedEventArgs eventArgs)
        {
            var flag = EquipmentSlotDefines.SlotMasks[eventArgs.Slot];
            IsFunctional = (_allowedSlots & flag) == flag;
            if (!IsFunctional) DisconnectFromInternals();
            UpdateUserInterface();
        }


        public void Unequipped(UnequippedEventArgs eventArgs)
        {
            IsFunctional = false;
            DisconnectFromInternals(eventArgs.User);
            UpdateUserInterface();
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor)) return false;
            OpenInterface(actor.playerSession);
            return true;
        }

        public void ConnectToInternals()
        {
            if (IsConnected || !IsFunctional) return;
            var internals = GetInternalsComponent();
            if (internals == null) return;
            IsConnected = true;
            IsOpen = false;
            internals.ConnectTank(Owner);
            UpdateUserInterface();
        }

        public void DisconnectFromInternals(IEntity? owner = null)
        {
            if (!IsConnected) return;
            IsOpen = false;
            IsConnected = false;
            GetInternalsComponent(owner)?.DisconnectTank();
            UpdateUserInterface();
        }

        private void UpdateUserInterface(bool initialUpdate = false)
        {
            _userInterface?.SetState(
                new GasTankBoundUserInterfaceState
                {
                    TankPressure = Air?.Pressure ?? 0,
                    OutputPressure = initialUpdate ? OutputPressure : (float?) null,
                    ValveOpen = IsOpen,
                    InternalsConnected = IsConnected,
                    CanConnectInternals = IsFunctional && GetInternalsComponent() != null
                });
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case GasTankSetPressureMessage msg:
                    OutputPressure = msg.Pressure;
                    break;
                case GasTankToggleValveMessage _:
                    SetValveState(!IsOpen);
                    break;
                case GasTankToggleInternalsMessage _:
                    ToggleInternals();
                    break;
            }
        }

        private void ToggleInternals()
        {
            if (IsConnected)
            {
                DisconnectFromInternals();
                return;
            }

            ConnectToInternals();
        }

        private InternalsComponent? GetInternalsComponent(IEntity? owner = null)
        {
            if (owner != null) return owner.GetComponentOrNull<InternalsComponent>();
            return ContainerHelpers.TryGetContainer(Owner, out var container)
                ? container.Owner.GetComponentOrNull<InternalsComponent>()
                : null;
        }

        private void EmitContents(float deltaTime)
        {
            if (!IsOpen || Air == null || Air.TotalMoles == 0 || IsConnected) return;
            var tile = Owner.Transform.Coordinates.GetTileAtmosphere(_entityManager);
            var amt = CalculateMolesFlowRate(tile, Air, NozzleArea, OutputPressure);
            var gas = Air.Remove(amt * deltaTime);
            if (tile?.Air == null) return;
            tile.AssumeAir(gas);
        }

        private static float CalculateMolesFlowRate(IGasMixtureHolder? tile, GasMixture air, float nozzleArea,
            float outputPressure)
        {
            // no gas = no flow
            if (air.Pressure <= 0) return 0;
            var targetPressure = tile?.Air?.Pressure ?? 0;
            var tankPressure = Math.Min(air.Pressure, outputPressure);
            // actually tank nozzle should become a diffuser for the outside atmosphere in that case
            // but to avoid madness we just do nothing
            if (tankPressure <= targetPressure) return 0;
            var mixtureHcr = air.HeatCapacityRatio;
            var criticalPressureRatio = MathF.Pow(2 / (mixtureHcr + 1),
                mixtureHcr / (mixtureHcr - 1));
            var pressureRatio = targetPressure / tankPressure;
            if (pressureRatio < criticalPressureRatio)
            {
                pressureRatio = criticalPressureRatio;
            }

            var mixtureMolarMass = air.MolarMass;
            return nozzleArea * tankPressure *
                   MathF.Sqrt(2 * mixtureHcr * mixtureMolarMass *
                              (MathF.Pow(pressureRatio, 2 / mixtureHcr) - MathF.Pow(pressureRatio,
                                  (mixtureHcr + 1) / mixtureHcr))) /
                   mixtureMolarMass;
        }

		public void AssumeAir(GasMixture giver)
        {
            Air.Merge(giver);

            CheckStatus();
        }

        private void CheckStatus()
        {
            if (Air == null)
                return;

            var pressure = Air.Pressure;

            if (pressure > Atmospherics.TankFragmentPressure)
            {
                // Give the gas a chance to build up more pressure.
                Air.React(this);
                Air.React(this);
                Air.React(this);
                pressure = Air.Pressure;
                var range = (pressure - Atmospherics.TankFragmentPressure) / Atmospherics.TankFragmentScale;

                // Let's cap the explosion, yeah?
                if (range > MaxExplosionRange)
                {
                    range = MaxExplosionRange;
                }

                Owner.SpawnExplosion((int) (range * 0.25f), (int) (range * 0.5f), (int) (range * 1.5f), 1);

                Owner.Delete();
                return;
            }

            if (pressure > Atmospherics.TankRupturePressure)
            {
                if (_integrity <= 0)
                {
                    var tileAtmos = Owner.Transform.Coordinates.GetTileAtmosphere();
                    tileAtmos?.AssumeAir(Air);

                    EntitySystem.Get<AudioSystem>().PlayAtCoords("Audio/Effects/spray.ogg", Owner.Transform.Coordinates,
                        AudioHelpers.WithVariation(0.125f));

                    Owner.Delete();
                    return;
                }

                _integrity--;
                return;
            }

            if (pressure > Atmospherics.TankLeakPressure)
            {
                if (_integrity <= 0)
                {
                    var tileAtmos = Owner.Transform.Coordinates.GetTileAtmosphere();
                    if (tileAtmos == null)
                        return;

                    var leakedGas = Air.RemoveRatio(0.25f);
                    tileAtmos.AssumeAir(leakedGas);
                } else
                {
                    _integrity--;
                }

                return;
            }

            if (_integrity < 3)
                _integrity++;
        }
        
        /// <summary>
        /// Open interaction window
        /// </summary>
        [Verb]
        private sealed class ControlVerb : Verb<GasTankComponent>
        {
            public override bool RequireInteractionRange => true;

            protected override void GetData(IEntity user, GasTankComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;
                if (!user.TryGetComponent<IActorComponent>(out _))
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = "Open Control Panel";
            }

            protected override void Activate(IEntity user, GasTankComponent component)
            {
                if (!user.TryGetComponent<IActorComponent>(out var actor))
                {
                    return;
                }

                component.OpenInterface(actor.playerSession);
            }
        }

        /// <summary>
        /// Verb to operate tank valve state
        /// </summary>
        [Verb]
        private sealed class ValveStateVerb : Verb<GasTankComponent>
        {
            protected override void GetData(IEntity user, GasTankComponent component, VerbData data)
            {
                data.Text = Loc.GetString(component.IsOpen
                    ? "Valve: Open"
                    : "Valve: Closed");
                data.Visibility = CheckVisibility(user, component.Owner);
            }

            protected override void Activate(IEntity user, GasTankComponent component)
            {
                if (CheckVisibility(user, component.Owner) == VerbVisibility.Invisible) return;
                component.SetValveState(!component.IsOpen);
            }

            private static VerbVisibility CheckVisibility(IEntity user, IEntity tank)
            {
                if (ContainerHelpers.TryGetContainer(tank, out var container))
                {
                    return container.Owner == user
                        ? VerbVisibility.Visible
                        : VerbVisibility.Invisible;
                }

                return user.InRangeUnobstructed(tank)
                    ? VerbVisibility.Visible
                    : VerbVisibility.Invisible;
            }
        }
    }
}
