#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Mobs;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Damage;
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
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Medical
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class CloningMachineComponent : SharedCloningMachineComponent, IActivate
    {
        private ContainerSlot _bodyContainer = default!;
        private CloningMachineStatus _status;
        [Dependency] private readonly IServerPreferencesManager _prefsManager = null!;

        [ViewVariables]
        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables]
        private BoundUserInterface? UserInterface =>
            Owner.GetUIOrNull(SharedCloningMachineComponent.CloningMachineUIKey.Key);

        private Mind? _capturedMind;


        public override void Initialize()
        {
            base.Initialize();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            _bodyContainer = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-bodyContainer", Owner);

            //TODO: write this so that it checks for a change in power events and acts accordingly.
            var newState = GetUserInterfaceState();
            UserInterface?.SetState(newState);

            UpdateUserInterface();

            Owner.EntityManager.EventBus.SubscribeEvent<GhostComponent.GhostReturnMessage>(EventSource.Local, this,
                HandleGhostReturn);
        }

        public void Update(float frametime)
        {
            UpdateUserInterface();
        }

        private void UpdateUserInterface()
        {
            if (!Powered)
            {
                return;
            }

            var newState = GetUserInterfaceState();
            UserInterface?.SetState(newState);
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
                !eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            UserInterface?.Open(actor.playerSession);
        }

        private async void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (!(obj.Message is UiButtonPressedMessage message) || message.ScanId == null) return;

            switch (message.Button)
            {
                case UiButton.Clone:
                    var mind = CloningSystem.Minds[(int) message.ScanId];

                    var dead =
                        mind.OwnedEntity.TryGetComponent<IDamageableComponent>(out var damageable) &&
                        damageable.CurrentDamageState == DamageState.Dead;

                    if (!dead)
                    {
                        break;
                    }

                    var entityManager = IoCManager.Resolve<IEntityManager>();
                    var _playerManager = IoCManager.Resolve<IPlayerManager>();

                    var mob = entityManager.SpawnEntity("HumanMob_Content", Owner.Transform.MapPosition);
                    var client = _playerManager
                        .GetPlayersBy(x => x.SessionId == mind.SessionId).First();
                    mob.GetComponent<HumanoidAppearanceComponent>()
                        .UpdateFromProfile(GetPlayerProfileAsync(client.Name).Result);
                    mob.Name = GetPlayerProfileAsync(client.Name).Result.Name;

                    _bodyContainer.Insert(mob);
                    _capturedMind = mind;

                    Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local,
                        new CloningStartedMessage(_capturedMind, (int) message.ScanId));

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public class CloningStartedMessage : EntitySystemMessage
        {
            public CloningStartedMessage(Mind capturedMind, int cloningIndex)
            {
                CapturedMind = capturedMind;
                CloningIndex = cloningIndex;
            }

            public Mind CapturedMind { get; }
            public int CloningIndex { get; }
        }


        private async Task<HumanoidCharacterProfile> GetPlayerProfileAsync(string username)
        {
            return (HumanoidCharacterProfile) (await _prefsManager.GetPreferencesAsync(username))
                .SelectedCharacter;
        }

        private void HandleGhostReturn(GhostComponent.GhostReturnMessage message)
        {
            if (message.Sender == _capturedMind)
            {
                //If the captured mind is in a ghost, we want to get rid of it.
                _capturedMind.VisitingEntity?.Delete();

                //Transfer the mind to the new mob
                _capturedMind.TransferTo(_bodyContainer.ContainedEntity);
                _bodyContainer.Remove(_bodyContainer.ContainedEntity);
                _capturedMind = null;
            }
        }
    }
}
