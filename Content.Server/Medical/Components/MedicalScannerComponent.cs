using System;
using Content.Server.Cloning;
using Content.Server.Mind.Components;
using Content.Server.Power.Components;
using Content.Server.Preferences.Managers;
using Content.Server.UserInterface;
using Content.Shared.Acts;
using Content.Shared.Damage;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.MedicalScanner;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Content.Server.Medical.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(SharedMedicalScannerComponent))]
    public class MedicalScannerComponent : SharedMedicalScannerComponent, IActivate, IDestroyAct
    {
        [Dependency] private readonly IServerPreferencesManager _prefsManager = null!;

        public static readonly TimeSpan InternalOpenAttemptDelay = TimeSpan.FromSeconds(0.5);
        public TimeSpan LastInternalOpenAttempt;

        private ContainerSlot _bodyContainer = default!;
        private readonly Vector2 _ejectOffset = new(0f, 0f);

        [ViewVariables]
        private bool Powered => !Owner.TryGetComponent(out ApcPowerReceiverComponent? receiver) || receiver.Powered;
        [ViewVariables]
        private BoundUserInterface? UserInterface => Owner.GetUIOrNull(MedicalScannerUiKey.Key);

        public bool IsOccupied => _bodyContainer.ContainedEntity != null;

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            _bodyContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-bodyContainer");

            // TODO: write this so that it checks for a change in power events and acts accordingly.
            var newState = GetUserInterfaceState();
            UserInterface?.SetState(newState);

            UpdateUserInterface();
        }

        private static readonly MedicalScannerBoundUserInterfaceState EmptyUIState =
            new(
                null,
                null,
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

            if (!body.TryGetComponent(out DamageableComponent? damageable))
            {
                return EmptyUIState;
            }

            if (_bodyContainer.ContainedEntity?.Uid == null)
            {
                return new MedicalScannerBoundUserInterfaceState(body.Uid, damageable, true);
            }

            var cloningSystem = EntitySystem.Get<CloningSystem>();
            var scanned = _bodyContainer.ContainedEntity.TryGetComponent(out MindComponent? mindComponent) &&
                         mindComponent.Mind != null &&
                         cloningSystem.HasDnaScan(mindComponent.Mind);

            return new MedicalScannerBoundUserInterfaceState(body.Uid, damageable, scanned);
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

        private MedicalScannerStatus GetStatusFromDamageState(MobStateComponent state)
        {
            if (state.IsAlive())
            {
                return MedicalScannerStatus.Green;
            }
            else if (state.IsCritical())
            {
                return MedicalScannerStatus.Red;
            }
            else if (state.IsDead())
            {
                return MedicalScannerStatus.Death;
            }
            else
            {
                return MedicalScannerStatus.Yellow;
            }
        }

        private MedicalScannerStatus GetStatus()
        {
            if (Powered)
            {
                var body = _bodyContainer.ContainedEntity;
                var state = body?.GetComponentOrNull<MobStateComponent>();

                return state == null
                    ? MedicalScannerStatus.Open
                    : GetStatusFromDamageState(state);
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

        void IActivate.Activate(ActivateEventArgs args)
        {
            if (!args.User.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            if (!Powered)
                return;

            UserInterface?.Open(actor.PlayerSession);
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
            if (containedEntity == null) return;
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
            if (obj.Message is not UiButtonPressedMessage message) return;

            switch (message.Button)
            {
                case UiButton.ScanDNA:
                    if (_bodyContainer.ContainedEntity != null)
                    {
                        var cloningSystem = EntitySystem.Get<CloningSystem>();

                        if (!_bodyContainer.ContainedEntity.TryGetComponent(out MindComponent? mindComp) || mindComp.Mind == null)
                        {
                            obj.Session.AttachedEntity?.PopupMessageCursor(Loc.GetString("medical-scanner-component-msg-no-soul"));
                            break;
                        }

                        // Null suppression based on above check. Yes, it's explicitly needed
                        var mind = mindComp.Mind!;

                        // We need the HumanoidCharacterProfile
                        // TODO: Move this further 'outwards' into a DNAComponent or somesuch.
                        // Ideally this ends with GameTicker & CloningSystem handing DNA to a function that sets up a body for that DNA.
                        var mindUser = mind.UserId;

                        if (mindUser == null)
                        {
                            // For now assume this means soul departed
                            obj.Session.AttachedEntity?.PopupMessageCursor(Loc.GetString("medical-scanner-component-msg-soul-broken"));
                            break;
                        }

                        // has to be explicit cast like this, IDK why, null suppression operators seem to not work
                        var profile = GetPlayerProfileAsync((NetUserId) mindUser);
                        cloningSystem.AddToDnaScans(new ClonerDNAEntry(mind, profile));
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            _bodyContainer.Insert(eventArgs.Dragged);
            return true;
        }

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            EjectBody();
        }

        private HumanoidCharacterProfile GetPlayerProfileAsync(NetUserId userId)
        {
            return (HumanoidCharacterProfile) _prefsManager.GetPreferences(userId).SelectedCharacter;
        }
    }
}
