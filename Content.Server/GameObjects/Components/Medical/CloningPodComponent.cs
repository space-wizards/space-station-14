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
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Medical
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class CloningPodComponent : SharedCloningPodComponent, IActivate
    {
        [Dependency] private readonly IServerPreferencesManager _prefsManager = null!;

        [ViewVariables]
        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables]
        private BoundUserInterface? UserInterface =>
            Owner.GetUIOrNull(CloningPodUIKey.Key);

        private ContainerSlot _bodyContainer = default!;
        private Mind? _capturedMind;
        private CloningPodStatus _status;
        private float _clonningProgress = 0;
        private readonly IEntityManager _entityManager = IoCManager.Resolve<IEntityManager>();
        private readonly IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();


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
            if (_bodyContainer.ContainedEntity != null &&
                Powered)
            {
                _clonningProgress += frametime;
                _clonningProgress = MathHelper.Clamp(_clonningProgress, 0f, 10f);
            }

            if (_clonningProgress >= 10.0 &&
                _bodyContainer.ContainedEntity != null &&
                _capturedMind?.Session.AttachedEntity == _bodyContainer.ContainedEntity &&
                Powered)
            {
                _bodyContainer.Remove(_bodyContainer.ContainedEntity);
                _capturedMind = null;
                _clonningProgress = 0f;

                _status = CloningPodStatus.Idle;
                UpdateAppearance();
            }

            UpdateUserInterface();
        }

        private void UpdateUserInterface()
        {
            if (!Powered) return;

            UserInterface?.SetState(GetUserInterfaceState());
        }

        private CloningPodBoundUserInterfaceState GetUserInterfaceState()
        {
            return new CloningPodBoundUserInterfaceState(CloningSystem.getIdToUser(), _clonningProgress,
                (_status == CloningPodStatus.Cloning));
        }

        private void UpdateAppearance()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(CloningPodVisuals.Status, _status);
            }
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
            if (!(obj.Message is CloningPodUiButtonPressedMessage message)) return;

            switch (message.Button)
            {
                case UiButton.Clone:

                    if (message.ScanId == null) return;

                    if (_bodyContainer.ContainedEntity != null ||
                        !CloningSystem.Minds.TryGetValue((int) message.ScanId, out var mind))
                    {
                        return;
                    }

                    var dead =
                        mind.OwnedEntity.TryGetComponent<IDamageableComponent>(out var damageable) &&
                        damageable.CurrentDamageState == DamageState.Dead;

                    if (!dead)
                    {
                        break;
                    }

                    var mob = _entityManager.SpawnEntity("HumanMob_Content", Owner.Transform.MapPosition);
                    var client = _playerManager
                        .GetPlayersBy(x => x.SessionId == mind.SessionId).First();
                    mob.GetComponent<HumanoidAppearanceComponent>()
                        .UpdateFromProfile(GetPlayerProfileAsync(client.Name).Result);
                    mob.Name = GetPlayerProfileAsync(client.Name).Result.Name;

                    _bodyContainer.Insert(mob);
                    _capturedMind = mind;

                    Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local,
                        new CloningStartedMessage(_capturedMind));
                    _status = CloningPodStatus.NoMind;
                    UpdateAppearance();

                    break;

                case UiButton.Eject:
                    if (_bodyContainer.ContainedEntity == null || _clonningProgress < 10f) break;

                    _bodyContainer.Remove(_bodyContainer.ContainedEntity!);
                    _capturedMind = null;
                    _clonningProgress = 0f;
                    _status = CloningPodStatus.Idle;
                    UpdateAppearance();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public class CloningStartedMessage : EntitySystemMessage
        {
            public CloningStartedMessage(Mind capturedMind)
            {
                CapturedMind = capturedMind;
            }

            public Mind CapturedMind { get; }
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

                _status = CloningPodStatus.Cloning;
                UpdateAppearance();
            }
        }
    }
}
