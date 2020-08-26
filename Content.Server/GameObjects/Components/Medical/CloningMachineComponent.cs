using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Mobs;
using Content.Shared.GameObjects.Components.Medical;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Server.GameObjects.Components.Medical
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class CloningMachineComponent : SharedCloningMachineComponent, IActivate
    {
        private AppearanceComponent _appearance;
        private BoundUserInterface _userInterface;
        private ContainerSlot _bodyContainer;
        private CloningMachineStatus _status;
        [Dependency] private readonly IServerPreferencesManager _prefsManager;

        private PowerReceiverComponent _powerReceiver;
        private bool Powered => _powerReceiver.Powered;

        public override void Initialize()
        {
            base.Initialize();

            _appearance = Owner.GetComponent<AppearanceComponent>();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(CloningMachineUIKey.Key);
            _userInterface.OnReceiveMessage += OnUiReceiveMessage;
            _bodyContainer = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-bodyContainer", Owner);
            _powerReceiver = Owner.GetComponent<PowerReceiverComponent>();

            //TODO: write this so that it checks for a change in power events and acts accordingly.
            var newState = GetUserInterfaceState();
            _userInterface.SetState(newState);

            UpdateUserInterface();
        }

        private void UpdateUserInterface()
        {
            if (!Powered)
            {
                return;
            }

            var newState = GetUserInterfaceState();
            _userInterface.SetState(newState);
        }


        private static readonly CloningMachineBoundUserInterfaceState EmptyUIState =
            new CloningMachineBoundUserInterfaceState(new Dictionary<int, string>(), 0, false);

        private CloningMachineBoundUserInterfaceState GetUserInterfaceState()
        {
            return new CloningMachineBoundUserInterfaceState(CloningSystem.getIdToUser(), 0, false);
        }


        public void Activate(ActivateEventArgs eventArgs)
        {
            if (!Powered ||
                !eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return;
            }


            _userInterface.Open(actor.playerSession);
        }

        private async void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            int Id;
            if (!(obj.Message is UiButtonPressedMessage message) || message.ScanId == null)
            {
                return;
            }
            else
            {
                Id = message.ScanId ?? default(int);
            }

            switch (message.Button)
            {
                case UiButton.Clone:
                    var entityManager = IoCManager.Resolve<IEntityManager>();
                    var _playerManager = IoCManager.Resolve<IPlayerManager>();

                    var mob = entityManager.SpawnEntity("HumanMob_Content", Owner.Transform.MapPosition);
                    var mind = CloningSystem.Minds[Id];

                    var client = _playerManager
                        .GetPlayersBy(x => x.SessionId == mind.SessionId).First();
                    mob.GetComponent<HumanoidAppearanceComponent>()
                        .UpdateFromProfile(GetPlayerProfileAsync(client.Name).Result);
                    mob.Name = GetPlayerProfileAsync(client.Name).Result.Name;

                    _bodyContainer.Insert(mob);
                    _bodyContainer.Remove(mob);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task<HumanoidCharacterProfile> GetPlayerProfileAsync(string username) =>
            (HumanoidCharacterProfile) (await _prefsManager.GetPreferencesAsync(username))
            .SelectedCharacter;
    }
}
