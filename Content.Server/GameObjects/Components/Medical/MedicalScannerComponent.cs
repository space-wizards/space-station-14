#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Body;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Players;
using Content.Server.Utility;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Medical;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Medical
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(SharedMedicalScannerComponent))]
    public class MedicalScannerComponent : SharedMedicalScannerComponent, IActivate, IDragDropOn
    {
        private ContainerSlot _bodyContainer = default!;
        private readonly Vector2 _ejectOffset = new Vector2(-0.5f, 0f);

        [Dependency] private readonly IPlayerManager _playerManager = null!;
        public bool IsOccupied => _bodyContainer.ContainedEntity != null;

        [ViewVariables]
        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(MedicalScannerUiKey.Key);

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            _bodyContainer = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-bodyContainer", Owner);

            // TODO: write this so that it checks for a change in power events and acts accordingly.
            var newState = GetUserInterfaceState();
            UserInterface?.SetState(newState);

            UpdateUserInterface();
        }

        private static readonly MedicalScannerBoundUserInterfaceState EmptyUIState =
            new MedicalScannerBoundUserInterfaceState(
                null,
                new Dictionary<DamageClass, int>(),
                new Dictionary<DamageType, int>(),
                false);

        private MedicalScannerBoundUserInterfaceState GetUserInterfaceState()
        {
            var body = _bodyContainer.ContainedEntity;
            if (body == null)
            {
                if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                {
                    appearance?.SetData(MedicalScannerVisuals.Status, MedicalScannerStatus.Open);
                }

                return EmptyUIState;
            }

            if (!body.TryGetComponent(out IDamageableComponent? damageable))
            {
                return EmptyUIState;
            }

            var classes = new Dictionary<DamageClass, int>(damageable.DamageClasses);
            var types = new Dictionary<DamageType, int>(damageable.DamageTypes);

            if (_bodyContainer.ContainedEntity?.Uid == null)
            {
                return new MedicalScannerBoundUserInterfaceState(body.Uid, classes, types, true);
            }


            return new MedicalScannerBoundUserInterfaceState(body.Uid, classes, types,
                CloningSystem.HasDnaScan(_bodyContainer.ContainedEntity.GetComponent<MindComponent>().Mind));
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

        private MedicalScannerStatus GetStatusFromDamageState(DamageState damageState)
        {
            switch (damageState)
            {
                case DamageState.Alive: return MedicalScannerStatus.Green;
                case DamageState.Critical: return MedicalScannerStatus.Red;
                case DamageState.Dead: return MedicalScannerStatus.Death;
                default: throw new ArgumentException(nameof(damageState));
            }
        }

        private MedicalScannerStatus GetStatus()
        {
            if (Powered)
            {
                var body = _bodyContainer.ContainedEntity;
                return body == null
                    ? MedicalScannerStatus.Open
                    : GetStatusFromDamageState(body.GetComponent<IDamageableComponent>().CurrentState);
            }

            return MedicalScannerStatus.Off;
        }

        private void UpdateAppearance()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(MedicalScannerVisuals.Status, GetStatus());
            }
        }

        public void Activate(ActivateEventArgs args)
        {
            if (!args.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            if (!Powered)
                return;

            UserInterface?.Open(actor.playerSession);
        }

        [Verb]
        public sealed class EnterVerb : Verb<MedicalScannerComponent>
        {
            protected override void GetData(IEntity user, MedicalScannerComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Enter");
                data.Visibility = component.IsOccupied ? VerbVisibility.Invisible : VerbVisibility.Visible;
            }

            protected override void Activate(IEntity user, MedicalScannerComponent component)
            {
                component.InsertBody(user);
            }
        }

        [Verb]
        public sealed class EjectVerb : Verb<MedicalScannerComponent>
        {
            protected override void GetData(IEntity user, MedicalScannerComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Eject");
                data.Visibility = component.IsOccupied ? VerbVisibility.Visible : VerbVisibility.Invisible;
            }

            protected override void Activate(IEntity user, MedicalScannerComponent component)
            {
                component.EjectBody();
            }
        }

        public void InsertBody(IEntity user)
        {
            _bodyContainer.Insert(user);
            UpdateUserInterface();
            UpdateAppearance();
        }

        public void EjectBody()
        {
            var containedEntity = _bodyContainer.ContainedEntity;
            _bodyContainer.Remove(containedEntity);
            containedEntity.Transform.WorldPosition += _ejectOffset;
            UpdateUserInterface();
            UpdateAppearance();
        }

        public void Update(float frameTime)
        {
            UpdateUserInterface();
            UpdateAppearance();
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (!(obj.Message is UiButtonPressedMessage message)) return;

            switch (message.Button)
            {
                case UiButton.ScanDNA:
                    if (_bodyContainer.ContainedEntity != null)
                    {
                        //TODO: Show a 'ERROR: Body is completely devoid of soul' if no Mind owns the entity.
                        CloningSystem.AddToDnaScans(_playerManager
                            .GetPlayersBy(playerSession =>
                            {
                                var mindOwnedMob = playerSession.ContentData()?.Mind?.OwnedEntity;

                                return mindOwnedMob != null && mindOwnedMob ==
                                    _bodyContainer.ContainedEntity;
                            }).Single()
                            .ContentData()
                            ?.Mind);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool CanDragDropOn(DragDropEventArgs eventArgs)
        {
            return eventArgs.Dragged.HasComponent<IBody>();
        }

        public bool DragDropOn(DragDropEventArgs eventArgs)
        {
            _bodyContainer.Insert(eventArgs.Dragged);
            return true;
        }
    }
}
