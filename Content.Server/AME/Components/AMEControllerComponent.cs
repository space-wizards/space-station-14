using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Mind.Components;
using Content.Server.NodeContainer;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.AME;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.AME.Components
{
    [RegisterComponent]
    public sealed class AMEControllerComponent : SharedAMEControllerComponent
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IEntitySystemManager _sysMan = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(AMEControllerUiKey.Key);
        private bool _injecting;
        [ViewVariables] public bool Injecting => _injecting;
        [ViewVariables] public int InjectionAmount;

        private AppearanceComponent? _appearance;
        private PowerSupplierComponent? _powerSupplier;
        [DataField("clickSound")] private SoundSpecifier _clickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
        [DataField("injectSound")] private SoundSpecifier _injectSound = new SoundPathSpecifier("/Audio/Effects/bang.ogg");

        private bool Powered => !_entities.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables]
        private int _stability = 100;

        public ContainerSlot JarSlot = default!;
        [ViewVariables] public bool HasJar => JarSlot.ContainedEntity != null;

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            _entities.TryGetComponent(Owner, out _appearance);

            _entities.TryGetComponent(Owner, out _powerSupplier);

            _injecting = false;
            InjectionAmount = 2;
            // TODO: Fix this bad name. I'd update maps but then people get mad.
            JarSlot = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"AMEController-fuelJarContainer");
        }

        internal void OnUpdate(float frameTime)
        {
            if (!_injecting)
            {
                return;
            }

            var group = GetAMENodeGroup();

            if (group == null)
            {
                return;
            }

            if (JarSlot.ContainedEntity is not {Valid: true} jar)
                return;

            _entities.TryGetComponent<AMEFuelContainerComponent?>(jar, out var fuelJar);
            if (fuelJar != null && _powerSupplier != null)
            {
                var availableInject = fuelJar.FuelAmount >= InjectionAmount ? InjectionAmount : fuelJar.FuelAmount;
                _powerSupplier.MaxSupply = group.InjectFuel(availableInject, out var overloading);
                fuelJar.FuelAmount -= availableInject;
                InjectSound(overloading);
                UpdateUserInterface();
            }

            _stability = group.GetTotalStability();

            UpdateDisplay(_stability);

            if (_stability <= 0) { group.ExplodeCores(); }

        }

        // Used to update core count
        public void OnAMENodeGroupUpdate()
        {
            UpdateUserInterface();
        }

        private AMEControllerBoundUserInterfaceState GetUserInterfaceState()
        {
            if (JarSlot.ContainedEntity is not {Valid: true} jar)
            {
                return new AMEControllerBoundUserInterfaceState(Powered, IsMasterController(), false, HasJar, 0, InjectionAmount, GetCoreCount());
            }

            var jarComponent = _entities.GetComponent<AMEFuelContainerComponent>(jar);
            return new AMEControllerBoundUserInterfaceState(Powered, IsMasterController(), _injecting, HasJar, jarComponent.FuelAmount, InjectionAmount, GetCoreCount());
        }

        /// <summary>
        /// Checks whether the player entity is able to use the controller.
        /// </summary>
        /// <param name="playerEntity">The player entity.</param>
        /// <returns>Returns true if the entity can use the controller, and false if it cannot.</returns>
        private bool PlayerCanUseController(EntityUid playerEntity, bool needsPower = true)
        {
            //Need player entity to check if they are still able to use the dispenser
            if (playerEntity == default)
                return false;

            //Check if device is powered
            if (needsPower && !Powered)
                return false;

            return true;
        }

        public void UpdateUserInterface()
        {
            var state = GetUserInterfaceState();
            UserInterface?.SetState(state);
        }

        /// <summary>
        /// Handles ui messages from the client. For things such as button presses
        /// which interact with the world and require server action.
        /// </summary>
        /// <param name="obj">A user interface message from the client.</param>
        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity is not {Valid: true} player)
            {
                return;
            }

            var msg = (UiButtonPressedMessage) obj.Message;
            var needsPower = msg.Button switch
            {
                UiButton.Eject => false,
                _ => true,
            };

            if (!PlayerCanUseController(player, needsPower))
                return;

            switch (msg.Button)
            {
                case UiButton.Eject:
                    TryEject(player);
                    break;
                case UiButton.ToggleInjection:
                    ToggleInjection();
                    break;
                case UiButton.IncreaseFuel:
                    InjectionAmount += 2;
                    break;
                case UiButton.DecreaseFuel:
                    InjectionAmount = InjectionAmount > 0 ? InjectionAmount -= 2 : 0;
                    break;
            }

            // Logging
            _entities.TryGetComponent(player, out MindComponent? mindComponent);
            if (mindComponent != null)
            {
                var humanReadableState = _injecting ? "Inject" : "Not inject";

                if (msg.Button == UiButton.IncreaseFuel || msg.Button == UiButton.DecreaseFuel)
                    _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{_entities.ToPrettyString(mindComponent.Owner):player} has set the AME to inject {InjectionAmount} while set to {humanReadableState}");

                if (msg.Button == UiButton.ToggleInjection)
                    _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{_entities.ToPrettyString(mindComponent.Owner):player} has set the AME to {humanReadableState}");
            }

            GetAMENodeGroup()?.UpdateCoreVisuals();

            UpdateUserInterface();
            ClickSound();
        }

        private void TryEject(EntityUid user)
        {
            if (!HasJar || _injecting)
                return;

            if (JarSlot.ContainedEntity is not {Valid: true} jar)
                return;

            JarSlot.Remove(jar);
            UpdateUserInterface();

            _sysMan.GetEntitySystem<SharedHandsSystem>().PickupOrDrop(user, jar);
        }

        private void ToggleInjection()
        {
            if (!_injecting)
            {
                _appearance?.SetData(AMEControllerVisuals.DisplayState, "on");
            }
            else
            {
                _appearance?.SetData(AMEControllerVisuals.DisplayState, "off");
                if (_powerSupplier != null)
                {
                    _powerSupplier.MaxSupply = 0;
                }
            }
            _injecting = !_injecting;
            UpdateUserInterface();
        }


        private void UpdateDisplay(int stability)
        {
            if (_appearance == null) { return; }

            _appearance.TryGetData<string>(AMEControllerVisuals.DisplayState, out var state);

            var newState = "on";
            if (stability < 50) { newState = "critical"; }
            if (stability < 10) { newState = "fuck"; }

            if (state != newState)
            {
                _appearance?.SetData(AMEControllerVisuals.DisplayState, newState);
            }

        }

        private AMENodeGroup? GetAMENodeGroup()
        {
            _entities.TryGetComponent(Owner, out NodeContainerComponent? nodeContainer);

            var engineNodeGroup = nodeContainer?.Nodes.Values
            .Select(node => node.NodeGroup)
            .OfType<AMENodeGroup>()
            .FirstOrDefault();

            return engineNodeGroup;
        }

        private bool IsMasterController()
        {
            if (GetAMENodeGroup()?.MasterController == this)
            {
                return true;
            }

            return false;
        }

        private int GetCoreCount()
        {
            var coreCount = 0;
            var group = GetAMENodeGroup();

            if (group != null)
            {
                coreCount = group.CoreCount;
            }

            return coreCount;
        }


        private void ClickSound()
        {
            SoundSystem.Play(_clickSound.GetSound(), Filter.Pvs(Owner), Owner, AudioParams.Default.WithVolume(-2f));
        }

        private void InjectSound(bool overloading)
        {
            SoundSystem.Play(_injectSound.GetSound(), Filter.Pvs(Owner), Owner, AudioParams.Default.WithVolume(overloading ? 10f : 0f));
        }
    }

}
