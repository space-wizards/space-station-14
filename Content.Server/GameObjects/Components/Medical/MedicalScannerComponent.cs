using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Medical;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Content.Shared.Damage;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.Components.Medical
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class MedicalScannerComponent : SharedMedicalScannerComponent, IActivate
    {
        private AppearanceComponent _appearance;
        private BoundUserInterface _userInterface;
        private ContainerSlot _bodyContainer;
        private readonly Vector2 _ejectOffset = new Vector2(-0.5f, 0f);
        public bool IsOccupied => _bodyContainer.ContainedEntity != null;

        private PowerReceiverComponent _powerReceiver;
        private bool Powered => _powerReceiver.Powered;

        public override void Initialize()
        {
            base.Initialize();

            _appearance = Owner.GetComponent<AppearanceComponent>();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(MedicalScannerUiKey.Key);
            _bodyContainer = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-bodyContainer", Owner);
            _powerReceiver = Owner.GetComponent<PowerReceiverComponent>();

            //TODO: write this so that it checks for a change in power events and acts accordingly.
            var newState = GetUserInterfaceState();
            _userInterface.SetState(newState);
            UpdateUserInterface();
        }

        private static readonly MedicalScannerBoundUserInterfaceState EmptyUIState =
            new MedicalScannerBoundUserInterfaceState(
                null,
                new Dictionary<DamageClass, int>(),
                new Dictionary<DamageType, int>());

        private MedicalScannerBoundUserInterfaceState GetUserInterfaceState()
        {
            var body = _bodyContainer.ContainedEntity;
            if (body == null)
            {
                _appearance.SetData(MedicalScannerVisuals.Status, MedicalScannerStatus.Open);
                return EmptyUIState;
            }

            if (!body.TryGetComponent(out IDamageableComponent damageable) ||
                damageable.CurrentDamageState == DamageState.Dead)
            {
                return EmptyUIState;
            }

            var classes = new Dictionary<DamageClass, int>(damageable.DamageClasses);
            var types = new Dictionary<DamageType, int>(damageable.DamageTypes);

            return new MedicalScannerBoundUserInterfaceState(body.Uid, classes, types);
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
            var body = _bodyContainer.ContainedEntity;
            return body == null
                ? MedicalScannerStatus.Open
                : GetStatusFromDamageState(body.GetComponent<IDamageableComponent>().CurrentDamageState);
        }

        private void UpdateAppearance()
        {
            _appearance.SetData(MedicalScannerVisuals.Status, GetStatus());
        }

        public void Activate(ActivateEventArgs args)
        {
            if (!args.User.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            if (!Powered)
                return;

            _userInterface.Open(actor.playerSession);
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
            if (_bodyContainer.ContainedEntity == null)
            {
                // There's no need to update if there's no one inside
                return;
            }

            UpdateUserInterface();
            UpdateAppearance();
        }
    }
}
