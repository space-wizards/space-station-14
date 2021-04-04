using System;
using Content.Server.Eui;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Mobs;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Medical;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Medical
{
    [RegisterComponent]
    public class CloningPodComponent : SharedCloningPodComponent
    {
        [Dependency] private readonly IServerPreferencesManager _prefsManager = null!;
        [Dependency] private readonly IPlayerManager _playerManager = null!;
        [Dependency] private readonly EuiManager _euiManager = null!;

        [ViewVariables]
        public bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables]
        public BoundUserInterface? UserInterface =>
            Owner.GetUIOrNull(CloningPodUIKey.Key);

        [ViewVariables] public ContainerSlot BodyContainer = default!;
        [ViewVariables] public Mind? CapturedMind;
        [ViewVariables] public float CloningProgress = 0;
        [DataField("cloningTime")]
        [ViewVariables] public float CloningTime = 30f;

        [ViewVariables]
        public CloningPodStatus Status;

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            BodyContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-bodyContainer");

            //TODO: write this so that it checks for a change in power events for GORE POD cases
            if (UserInterface != null)
                EntitySystem.Get<CloningSystem>().UpdateUserInterface(this);
        }

        public override void OnRemove()
        {
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage -= OnUiReceiveMessage;
            }

            base.OnRemove();
        }

        private void UpdateAppearance()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(CloningPodVisuals.Status, Status);
            }
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Message is not CloningPodUiButtonPressedMessage message) return;

            switch (message.Button)
            {
                case UiButton.Clone:
                    if (message.ScanId == null || BodyContainer.ContainedEntity != null)
                        return;

                    var cloningSystem = EntitySystem.Get<CloningSystem>();

                    if (!cloningSystem.Minds.TryGetValue(message.ScanId.Value, out var mind))
                    {
                        return; // ScanId is not in database
                    }

                    if (cloningSystem.ClonesWaitingForMind.TryGetValue(mind, out var cloneUid))
                    {
                        if (Owner.EntityManager.TryGetEntity(cloneUid, out var clone) &&
                            clone.TryGetComponent<IMobStateComponent>(out var cloneState) &&
                            !cloneState.IsDead() &&
                            clone.TryGetComponent(out MindComponent? cloneMindComp) &&
                            (cloneMindComp.Mind == null || cloneMindComp.Mind == mind))
                            return; // Mind already has clone

                        cloningSystem.ClonesWaitingForMind.Remove(mind);
                    }

                    if (mind.OwnedEntity != null &&
                        mind.OwnedEntity.TryGetComponent<IMobStateComponent>(out var state) &&
                        !state.IsDead())
                        return; // Body controlled by mind is not dead

                    // TODO: Implement ClonerDNAEntry and get the profile appearance and name when scanned
                    if (mind.UserId == null || !_playerManager.TryGetSessionById(mind.UserId.Value, out var client))
                        return;

                    var mob = Owner.EntityManager.SpawnEntity("HumanMob_Content", Owner.Transform.MapPosition);

                    var profile = GetPlayerProfileAsync(client.UserId);
                    mob.GetComponent<HumanoidAppearanceComponent>().UpdateFromProfile(profile);
                    mob.Name = profile.Name;

                    var cloneMindReturn = mob.AddComponent<BeingClonedComponent>();
                    cloneMindReturn.Mind = mind;
                    cloneMindReturn.Parent = Owner.Uid;

                    BodyContainer.Insert(mob);
                    CapturedMind = mind;
                    cloningSystem.ClonesWaitingForMind.Add(mind, mob.Uid);

                    UpdateStatus(CloningPodStatus.NoMind);

                    var acceptMessage = new AcceptCloningEui(mind);
                    _euiManager.OpenEui(acceptMessage, client);

                    break;

                case UiButton.Eject:
                    Eject();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Eject()
        {
            var entity = BodyContainer.ContainedEntity;
            if (entity == null || CloningProgress < CloningTime)
                return;

            entity.RemoveComponent<BeingClonedComponent>();
            BodyContainer.Remove(entity!);
            CapturedMind = null;
            CloningProgress = 0f;
            UpdateStatus(CloningPodStatus.Idle);
        }

        public void UpdateStatus(CloningPodStatus status)
        {
            Status = status;
            UpdateAppearance();
            EntitySystem.Get<CloningSystem>().UpdateUserInterface(this);
        }

        private HumanoidCharacterProfile GetPlayerProfileAsync(NetUserId userId)
        {
            return (HumanoidCharacterProfile) _prefsManager.GetPreferences(userId).SelectedCharacter;
        }
    }
}
