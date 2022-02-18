using System;
using Content.Server.Climbing;
using Content.Server.EUI;
using Content.Server.Mind.Components;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.Cloning;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Content.Shared.Species;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Cloning.Components
{
    [RegisterComponent]
    public sealed class CloningPodComponent : SharedCloningPodComponent
    {
        [Dependency] private readonly IPlayerManager _playerManager = null!;
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;

        [Dependency] private readonly EuiManager _euiManager = null!;

        [ViewVariables]
        public bool Powered => !_entities.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables]
        public BoundUserInterface? UserInterface =>
            Owner.GetUIOrNull(CloningPodUIKey.Key);

        [ViewVariables] public ContainerSlot BodyContainer = default!;
        [ViewVariables] public Mind.Mind? CapturedMind;
        [ViewVariables] public float CloningProgress = 0;
        [DataField("cloningTime")]
        [ViewVariables] public float CloningTime = 30f;
        // Used to prevent as many duplicate UI messages as possible
        [ViewVariables] public bool UiKnownPowerState = false;

        [ViewVariables]
        public CloningPodStatus Status;

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            BodyContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-bodyContainer");

            //TODO: write this so that it checks for a change in power events for GORE POD cases
            EntitySystem.Get<CloningSystem>().UpdateUserInterface(this);
        }

        protected override void OnRemove()
        {
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage -= OnUiReceiveMessage;
            }

            base.OnRemove();
        }

        private void UpdateAppearance()
        {
            if (_entities.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(CloningPodVisuals.Status, Status);
            }
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Message is not CloningPodUiButtonPressedMessage message || obj.Session.AttachedEntity == null)
                return;

            switch (message.Button)
            {
                case UiButton.Clone:
                    if (BodyContainer.ContainedEntity != null)
                    {
                        obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("cloning-pod-component-msg-occupied"));
                        return;
                    }

                    if (message.ScanId == null)
                    {
                        obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("cloning-pod-component-msg-no-selection"));
                        return;
                    }

                    var cloningSystem = EntitySystem.Get<CloningSystem>();

                    if (!cloningSystem.IdToDNA.TryGetValue(message.ScanId.Value, out var dna))
                    {
                        obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("cloning-pod-component-msg-bad-selection"));
                        return; // ScanId is not in database
                    }

                    var mind = dna.Mind;

                    if (cloningSystem.ClonesWaitingForMind.TryGetValue(mind, out var clone))
                    {
                        if (_entities.EntityExists(clone) &&
                            _entities.TryGetComponent<MobStateComponent?>(clone, out var cloneState) &&
                            !cloneState.IsDead() &&
                            _entities.TryGetComponent(clone, out MindComponent? cloneMindComp) &&
                            (cloneMindComp.Mind == null || cloneMindComp.Mind == mind))
                        {
                            obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("cloning-pod-component-msg-already-cloning"));
                            return; // Mind already has clone
                        }

                        cloningSystem.ClonesWaitingForMind.Remove(mind);
                    }

                    if (mind.OwnedEntity != null &&
                        _entities.TryGetComponent<MobStateComponent?>(mind.OwnedEntity.Value, out var state) &&
                        !state.IsDead())
                    {
                        obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("cloning-pod-component-msg-already-alive"));
                        return; // Body controlled by mind is not dead
                    }

                    // Yes, we still need to track down the client because we need to open the Eui
                    if (mind.UserId == null || !_playerManager.TryGetSessionById(mind.UserId.Value, out var client))
                    {
                        obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("cloning-pod-component-msg-user-offline"));
                        return; // If we can't track down the client, we can't offer transfer. That'd be quite bad.
                    }

                    var speciesProto = _prototype.Index<SpeciesPrototype>(dna.Profile.Species).Prototype;
                    var mob = _entities.SpawnEntity(speciesProto, _entities.GetComponent<TransformComponent>(Owner).MapPosition);


                    EntitySystem.Get<SharedHumanoidAppearanceSystem>().UpdateFromProfile(mob, dna.Profile);
                    _entities.GetComponent<MetaDataComponent>(mob).EntityName = dna.Profile.Name;

                    var cloneMindReturn = _entities.AddComponent<BeingClonedComponent>(mob);
                    cloneMindReturn.Mind = mind;
                    cloneMindReturn.Parent = Owner;

                    BodyContainer.Insert(mob);
                    CapturedMind = mind;
                    cloningSystem.ClonesWaitingForMind.Add(mind, mob);

                    UpdateStatus(CloningPodStatus.NoMind);

                    var acceptMessage = new AcceptCloningEui(mind);
                    _euiManager.OpenEui(acceptMessage, client);

                    break;

                case UiButton.Eject:
                    if (BodyContainer.ContainedEntity == null)
                    {
                        obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("cloning-pod-component-msg-empty"));
                        return;
                    }
                    if (CloningProgress < CloningTime)
                    {
                        obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("cloning-pod-component-msg-incomplete"));
                        return;
                    }
                    Eject();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Eject()
        {
            if (BodyContainer.ContainedEntity is not {Valid: true} entity || CloningProgress < CloningTime)
                return;

            _entities.RemoveComponent<BeingClonedComponent>(entity);
            BodyContainer.Remove(entity);
            CapturedMind = null;
            CloningProgress = 0f;
            UpdateStatus(CloningPodStatus.Idle);
            EntitySystem.Get<ClimbSystem>().ForciblySetClimbing(entity);
        }

        public void UpdateStatus(CloningPodStatus status)
        {
            Status = status;
            UpdateAppearance();
            EntitySystem.Get<CloningSystem>().UpdateUserInterface(this);
        }
    }
}
