using System;
using Content.Server.GameObjects.Components.Damage;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Medical;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Shared.Interfaces.GameObjects.Components;

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
            UpdateUserInterface();
        }

        private static readonly MedicalScannerBoundUserInterfaceState EmptyUIState =
            new MedicalScannerBoundUserInterfaceState(
                0,
                0,
                null);

        private MedicalScannerBoundUserInterfaceState GetUserInterfaceState()
        {
            var body = _bodyContainer.ContainedEntity;
            if (body == null)
            {
                _appearance.SetData(MedicalScannerVisuals.Status, MedicalScannerStatus.Open);
                return EmptyUIState;
            }

            //TODO: make work with BodyManagerComponent
            var damageable = body.GetComponent<BaseDamageableComponent>();
            //if(damageable.CurrentDamageState == DamageState.Dead)
                return EmptyUIState;

            /*
            var currentHealth = damageable.CurrentDamage[DamageType.Total];

            var dmgDict = new Dictionary<string, int>();

            foreach (var dmgType in (DamageType[]) Enum.GetValues(typeof(DamageType)))
            {
                if (damageable.CurrentDamage.TryGetValue(dmgType, out var amount))
                {
                    dmgDict[dmgType.ToString()] = amount;
                }
            }

            return new MedicalScannerBoundUserInterfaceState(
                deathThresholdValue - currentHealth,
                deathThresholdValue,
                dmgDict);
            */
        }

        private void UpdateUserInterface()
        {
            if (!Powered)
                return;
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
                : GetStatusFromDamageState(body.GetComponent<BaseDamageableComponent>().CurrentDamageState);
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

                data.Text = "Enter";
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

                data.Text = "Eject";
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
