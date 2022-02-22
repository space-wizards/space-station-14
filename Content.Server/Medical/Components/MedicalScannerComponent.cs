using System;
using Content.Server.Climbing;
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
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Content.Server.Medical.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(SharedMedicalScannerComponent))]
    public sealed class MedicalScannerComponent : SharedMedicalScannerComponent, IActivate, IDestroyAct
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IServerPreferencesManager _prefsManager = null!;

        public static readonly TimeSpan InternalOpenAttemptDelay = TimeSpan.FromSeconds(0.5);
        public TimeSpan LastInternalOpenAttempt;

        private ContainerSlot _bodyContainer = default!;

        [ViewVariables]
        private bool Powered => !_entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;
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
                if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
                {
                    appearance?.SetData(MedicalScannerVisuals.Status, MedicalScannerStatus.Open);
                }

                return EmptyUIState;
            }

            if (!_entMan.TryGetComponent(body.Value, out DamageableComponent? damageable))
            {
                return EmptyUIState;
            }

            if (_bodyContainer.ContainedEntity == null)
            {
                return new MedicalScannerBoundUserInterfaceState(body, damageable, true);
            }

            var cloningSystem = EntitySystem.Get<CloningSystem>();
            var scanned = _entMan.TryGetComponent(_bodyContainer.ContainedEntity.Value, out MindComponent? mindComponent) &&
                         mindComponent.Mind != null &&
                         cloningSystem.HasDnaScan(mindComponent.Mind);

            return new MedicalScannerBoundUserInterfaceState(body, damageable, scanned);
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
                if (body == null)
                    return MedicalScannerStatus.Open;

                var state = _entMan.GetComponentOrNull<MobStateComponent>(body.Value);

                return state == null ? MedicalScannerStatus.Open : GetStatusFromDamageState(state);
            }

            return MedicalScannerStatus.Off;
        }

        private void UpdateAppearance()
        {
            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(MedicalScannerVisuals.Status, GetStatus());
            }
        }

        void IActivate.Activate(ActivateEventArgs args)
        {
            if (!_entMan.TryGetComponent(args.User, out ActorComponent? actor))
            {
                return;
            }

            if (!Powered)
                return;

            UserInterface?.Open(actor.PlayerSession);
        }

        public void InsertBody(EntityUid user)
        {
            _bodyContainer.Insert(user);
            UpdateUserInterface();
            UpdateAppearance();
        }

        public void EjectBody()
        {
            if (_bodyContainer.ContainedEntity is not {Valid: true} contained) return;
            _bodyContainer.Remove(contained);
            UpdateUserInterface();
            UpdateAppearance();
            EntitySystem.Get<ClimbSystem>().ForciblySetClimbing(contained);
        }

        public void Update(float frameTime)
        {
            UpdateUserInterface();
            UpdateAppearance();
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Message is not UiButtonPressedMessage message || obj.Session.AttachedEntity == null) return;

            switch (message.Button)
            {
                case UiButton.ScanDNA:
                    if (_bodyContainer.ContainedEntity != null)
                    {
                        var cloningSystem = EntitySystem.Get<CloningSystem>();

                        if (!_entMan.TryGetComponent(_bodyContainer.ContainedEntity.Value, out MindComponent? mindComp) || mindComp.Mind == null)
                        {
                            obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("medical-scanner-component-msg-no-soul"));
                            break;
                        }

                        // Null suppression based on above check. Yes, it's explicitly needed
                        var mind = mindComp.Mind!;

                        // We need the HumanoidCharacterProfile
                        // TODO: Move this further 'outwards' into a DNAComponent or somesuch.
                        // Ideally this ends with GameTicker & CloningSystem handing DNA to a function that sets up a body for that DNA.
                        var mindUser = mind.UserId;

                        if (mindUser.HasValue == false || mind.Session == null)
                        {
                            // For now assume this means soul departed
                            obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("medical-scanner-component-msg-soul-broken"));
                            break;
                        }

                        var profile = GetPlayerProfileAsync(mindUser.Value);
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
