using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.NodeContainer;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.AME;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.AME.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IInteractUsing))]
    public class AMEControllerComponent : SharedAMEControllerComponent, IActivate, IInteractUsing
    {
        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(AMEControllerUiKey.Key);
        private bool _injecting;
        [ViewVariables] public bool Injecting => _injecting;
        [ViewVariables] public int InjectionAmount;

        private AppearanceComponent? _appearance;
        private PowerSupplierComponent? _powerSupplier;
        [DataField("clickSound")] private SoundSpecifier _clickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
        [DataField("injectSound")] private SoundSpecifier _injectSound = new SoundPathSpecifier("/Audio/Effects/bang.ogg");

        private bool Powered => !Owner.TryGetComponent(out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables]
        private int _stability = 100;

        private ContainerSlot _jarSlot = default!;
        [ViewVariables] private bool HasJar => _jarSlot.ContainedEntity != null;

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            Owner.TryGetComponent(out _appearance);

            Owner.TryGetComponent(out _powerSupplier);

            _injecting = false;
            InjectionAmount = 2;
            _jarSlot = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-fuelJarContainer");
        }

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
#pragma warning disable 618
            base.HandleMessage(message, component);
#pragma warning restore 618
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    OnPowerChanged(powerChanged);
                    break;
            }
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

            var jar = _jarSlot.ContainedEntity;
            if (jar is null)
                return;

            jar.TryGetComponent<AMEFuelContainerComponent>(out var fuelJar);
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

        /// <summary>
        /// Called when you click the owner entity with an empty hand. Opens the UI client-side if possible.
        /// </summary>
        /// <param name="args">Data relevant to the event such as the actor which triggered it.</param>
        void IActivate.Activate(ActivateEventArgs args)
        {
            if (!args.User.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            if (!args.User.TryGetComponent(out HandsComponent? hands))
            {
                Owner.PopupMessage(args.User, Loc.GetString("ame-controller-component-interact-no-hands-text"));
                return;
            }

            var activeHandEntity = hands.GetActiveHand?.Owner;
            if (activeHandEntity == null)
            {
                UserInterface?.Open(actor.PlayerSession);
            }
        }

        private void OnPowerChanged(PowerChangedMessage e)
        {
            UpdateUserInterface();
        }

        // Used to update core count
        public void OnAMENodeGroupUpdate()
        {
            UpdateUserInterface();
        }

        private AMEControllerBoundUserInterfaceState GetUserInterfaceState()
        {
            var jar = _jarSlot.ContainedEntity;
            if (jar == null)
            {
                return new AMEControllerBoundUserInterfaceState(Powered, IsMasterController(), false, HasJar, 0, InjectionAmount, GetCoreCount());
            }

            var jarcomponent = jar.GetComponent<AMEFuelContainerComponent>();
            return new AMEControllerBoundUserInterfaceState(Powered, IsMasterController(), _injecting, HasJar, jarcomponent.FuelAmount, InjectionAmount, GetCoreCount());
        }

        /// <summary>
        /// Checks whether the player entity is able to use the controller.
        /// </summary>
        /// <param name="playerEntity">The player entity.</param>
        /// <returns>Returns true if the entity can use the controller, and false if it cannot.</returns>
        private bool PlayerCanUseController(IEntity playerEntity, bool needsPower = true)
        {
            //Need player entity to check if they are still able to use the dispenser
            if (playerEntity == null)
                return false;

            var actionBlocker = EntitySystem.Get<ActionBlockerSystem>();

            //Check if player can interact in their current state
            if (!actionBlocker.CanInteract(playerEntity) || !actionBlocker.CanUse(playerEntity))
                return false;
            //Check if device is powered
            if (needsPower && !Powered)
                return false;

            return true;
        }

        private void UpdateUserInterface()
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
            if (obj.Session.AttachedEntity == null)
            {
                return;
            }

            var msg = (UiButtonPressedMessage) obj.Message;
            var needsPower = msg.Button switch
            {
                UiButton.Eject => false,
                _ => true,
            };

            if (!PlayerCanUseController(obj.Session.AttachedEntity, needsPower))
                return;

            switch (msg.Button)
            {
                case UiButton.Eject:
                    TryEject(obj.Session.AttachedEntity);
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

            GetAMENodeGroup()?.UpdateCoreVisuals();

            UpdateUserInterface();
            ClickSound();
        }

        private void TryEject(IEntity user)
        {
            if (!HasJar || _injecting)
                return;

            var jar = _jarSlot.ContainedEntity;
            if (jar is null)
                return;

            _jarSlot.Remove(jar);
            UpdateUserInterface();

            if (!user.TryGetComponent<HandsComponent>(out var hands) || !jar.TryGetComponent<ItemComponent>(out var item))
                return;
            if (hands.CanPutInHand(item))
                hands.PutInHand(item);
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
            Owner.TryGetComponent(out NodeContainerComponent? nodeContainer);

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
            SoundSystem.Play(Filter.Pvs(Owner), _clickSound.GetSound(), Owner, AudioParams.Default.WithVolume(-2f));
        }

        private void InjectSound(bool overloading)
        {
            SoundSystem.Play(Filter.Pvs(Owner), _injectSound.GetSound(), Owner, AudioParams.Default.WithVolume(overloading ? 10f : 0f));
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs args)
        {
            if (!args.User.TryGetComponent(out HandsComponent? hands))
            {
                Owner.PopupMessage(args.User, Loc.GetString("ame-controller-component-interact-using-no-hands-text"));
                return true;
            }

            if (hands.GetActiveHand == null)
            {
                Owner.PopupMessage(args.User, Loc.GetString("ame-controller-component-interact-using-nothing-in-hands-text"));
                return false;
            }

            var activeHandEntity = hands.GetActiveHand.Owner;
            if (activeHandEntity.TryGetComponent<AMEFuelContainerComponent>(out var fuelContainer))
            {
                if (HasJar)
                {
                    Owner.PopupMessage(args.User, Loc.GetString("ame-controller-component-interact-using-already-has-jar"));
                }

                else
                {
                    _jarSlot.Insert(activeHandEntity);
                    Owner.PopupMessage(args.User, Loc.GetString("ame-controller-component-interact-using-success"));
                    UpdateUserInterface();
                }
            }
            else
            {
                Owner.PopupMessage(args.User, Loc.GetString("ame-controller-component-interact-using-fail"));
            }

            return true;
        }
    }

}
