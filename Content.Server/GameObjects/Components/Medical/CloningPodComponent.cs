#nullable enable
using System;
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
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Medical
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class CloningPodComponent : SharedCloningPodComponent, IActivate
    {
        [Dependency] private readonly IServerPreferencesManager _prefsManager = null!;
        [Dependency] private readonly IPlayerManager _playerManager = null!;

        [ViewVariables]
        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables]
        private BoundUserInterface? UserInterface =>
            Owner.GetUIOrNull(CloningPodUIKey.Key);

        private ContainerSlot _bodyContainer = default!;
        private Mind? _capturedMind;
        private CloningPodStatus _status;
        private float _cloningProgress = 0;
        private float _cloningTime;


        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _cloningTime, "cloningTime", 10f);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            _bodyContainer = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-bodyContainer", Owner);

            //TODO: write this so that it checks for a change in power events for GORE POD cases
            var newState = GetUserInterfaceState();
            UserInterface?.SetState(newState);

            UpdateUserInterface();

            Owner.EntityManager.EventBus.SubscribeEvent<GhostComponent.GhostReturnMessage>(EventSource.Local, this,
                HandleGhostReturn);
        }

        public void Update(float frameTime)
        {
            if (_bodyContainer.ContainedEntity != null &&
                Powered)
            {
                _cloningProgress += frameTime;
                _cloningProgress = MathHelper.Clamp(_cloningProgress, 0f, _cloningTime);
            }

            if (_cloningProgress >= _cloningTime &&
                _bodyContainer.ContainedEntity != null &&
                _capturedMind?.Session.AttachedEntity == _bodyContainer.ContainedEntity &&
                Powered)
            {
                _bodyContainer.Remove(_bodyContainer.ContainedEntity);
                _capturedMind = null;
                _cloningProgress = 0f;

                _status = CloningPodStatus.Idle;
                UpdateAppearance();
            }

            UpdateUserInterface();
        }

        public override void OnRemove()
        {
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage -= OnUiReceiveMessage;
            }

            Owner.EntityManager.EventBus.UnsubscribeEvent<GhostComponent.GhostReturnMessage>(EventSource.Local, this);

            base.OnRemove();
        }

        private void UpdateUserInterface()
        {
            if (!Powered) return;

            UserInterface?.SetState(GetUserInterfaceState());
        }

        private CloningPodBoundUserInterfaceState GetUserInterfaceState()
        {
            var idToUser = EntitySystem.Get<CloningSystem>().GetIdToUser();

            return new CloningPodBoundUserInterfaceState(idToUser, _cloningProgress,
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

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (!(obj.Message is CloningPodUiButtonPressedMessage message)) return;

            switch (message.Button)
            {
                case UiButton.Clone:
                    if (message.ScanId == null) return;

                    var cloningSystem = EntitySystem.Get<CloningSystem>();

                    if (_bodyContainer.ContainedEntity != null ||
                        !cloningSystem.Minds.TryGetValue(message.ScanId.Value, out var mind))
                    {
                        return;
                    }

                    var dead =
                        mind.OwnedEntity.TryGetComponent<IDamageableComponent>(out var damageable) &&
                        damageable.CurrentState == DamageState.Dead;
                    if (!dead) return;


                    var mob = Owner.EntityManager.SpawnEntity("HumanMob_Content", Owner.Transform.MapPosition);
                    var client = _playerManager.GetSessionByUserId(mind.UserId!.Value);
                    var profile = GetPlayerProfileAsync(client.UserId);
                    mob.GetComponent<HumanoidAppearanceComponent>().UpdateFromProfile(profile);
                    mob.Name = profile.Name;

                    _bodyContainer.Insert(mob);
                    _capturedMind = mind;

                    Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local,
                        new CloningStartedMessage(_capturedMind));
                    _status = CloningPodStatus.NoMind;
                    UpdateAppearance();

                    break;

                case UiButton.Eject:
                    if (_bodyContainer.ContainedEntity == null || _cloningProgress < _cloningTime) break;

                    _bodyContainer.Remove(_bodyContainer.ContainedEntity!);
                    _capturedMind = null;
                    _cloningProgress = 0f;
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


        private HumanoidCharacterProfile GetPlayerProfileAsync(NetUserId userId)
        {
            return (HumanoidCharacterProfile) _prefsManager.GetPreferences(userId).SelectedCharacter;
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
