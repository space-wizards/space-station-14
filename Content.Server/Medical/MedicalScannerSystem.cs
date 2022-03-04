using Content.Shared.ActionBlocker;
using Content.Shared.Movement;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Content.Server.Climbing;
using Content.Shared.MobState.Components;
using Content.Shared.DragDrop;
using Content.Shared.Acts;
using Content.Server.Power.Components;
using Robust.Shared.Containers;
using Content.Server.Mind.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Content.Server.Popups;
using Robust.Shared.Player;
using Content.Shared.Preferences;
using Robust.Shared.Network;
using Content.Server.Preferences.Managers;
using Content.Server.Cloning;
using Content.Shared.CharacterAppearance.Components;

using static Content.Shared.MedicalScanner.SharedMedicalScannerComponent;

namespace Content.Server.MedicalScanner
{
    [UsedImplicitly]
    internal sealed class MedicalScannerSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly CloningSystem _cloningSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IServerPreferencesManager _prefsManager = null!;
        [Dependency] private readonly ClimbSystem _climbSystem = default!;

        private const float UpdateRate = 1f;
        private float _updateDif;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MedicalScannerComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<MedicalScannerComponent, ActivateInWorldEvent>(OnActivated);
            SubscribeLocalEvent<MedicalScannerComponent, RelayMovementEntityEvent>(OnRelayMovement);
            SubscribeLocalEvent<MedicalScannerComponent, GetVerbsEvent<InteractionVerb>>(AddInsertOtherVerb);
            SubscribeLocalEvent<MedicalScannerComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
            SubscribeLocalEvent<MedicalScannerComponent, DestructionEventArgs>(OnDestroyed);
            SubscribeLocalEvent<MedicalScannerComponent, DragDropEvent>(HandleDragDropOn);
            SubscribeLocalEvent<MedicalScannerComponent, ScanButtonPressedMessage>(OnScanButtonPressed);
        }

        private void OnComponentInit(EntityUid uid, MedicalScannerComponent scannerComponent, ComponentInit args)
        {
            base.Initialize();

            scannerComponent.BodyContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(scannerComponent.Owner, $"{Name}-bodyContainer");
            UpdateUserInterface(uid, scannerComponent);
        }

        private void OnActivated(EntityUid uid, MedicalScannerComponent scannerComponent, ActivateInWorldEvent args)
        {
            if (!TryComp<ActorComponent>(args.User, out var actor) || !IsPowered(scannerComponent))
                return;

            scannerComponent.UserInterface?.Toggle(actor.PlayerSession);
            UpdateUserInterface(uid, scannerComponent);
        }

        private void OnRelayMovement(EntityUid uid, MedicalScannerComponent scannerComponent, RelayMovementEntityEvent args)
        {
            if (!_blocker.CanInteract(args.Entity, scannerComponent.Owner))
                return;

            EjectBody(uid, scannerComponent);
        }

        private void AddInsertOtherVerb(EntityUid uid, MedicalScannerComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (args.Using == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                IsOccupied(component) ||
                !component.CanInsert(args.Using.Value))
                return;

            string name = "Unknown";
            if (TryComp<MetaDataComponent>(args.Using.Value, out MetaDataComponent? metadata))
                name = metadata.EntityName;

            InteractionVerb verb = new();
            verb.Act = () => InsertBody(component.Owner, args.Target, component);
            verb.Category = VerbCategory.Insert;
            verb.Text = name;
            args.Verbs.Add(verb);
        }

        private void AddAlternativeVerbs(EntityUid uid, MedicalScannerComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            // Eject verb
            if (IsOccupied(component))
            {
                AlternativeVerb verb = new();
                verb.Act = () => EjectBody(uid, component);
                verb.Category = VerbCategory.Eject;
                verb.Text = Loc.GetString("medical-scanner-verb-noun-occupant");
                args.Verbs.Add(verb);
            }

            if (component.BodyContainer == null) {
                return;
            }

            // Self-insert verb
            if (!IsOccupied(component) &&
                component.CanInsert(args.User) &&
                _actionBlockerSystem.CanMove(args.User))
            {
                AlternativeVerb verb = new();
                verb.Act = () => InsertBody(component.Owner, args.User, component);
                verb.Text = Loc.GetString("medical-scanner-verb-enter");
                args.Verbs.Add(verb);
            }
        }

        private void OnDestroyed(EntityUid uid, MedicalScannerComponent scannerComponent, DestructionEventArgs args)
        {
            EjectBody(uid, scannerComponent);
        }

        private void HandleDragDropOn(EntityUid uid, MedicalScannerComponent scannerComponent, DragDropEvent args)
        {
            InsertBody(uid, args.Dragged, scannerComponent);
        }

        private void OnScanButtonPressed(EntityUid uid, MedicalScannerComponent scannerComponent, ScanButtonPressedMessage args)
        {
            TrySaveCloningData(uid, scannerComponent);
        }

        private static readonly MedicalScannerBoundUserInterfaceState EmptyUIState =
            new(null, false);

        private MedicalScannerBoundUserInterfaceState GetUserInterfaceState(EntityUid uid,  MedicalScannerComponent scannerComponent)
        {
            EntityUid? containedBody = scannerComponent.BodyContainer.ContainedEntity;

            if (containedBody == null)
            {
                UpdateAppearance(uid, scannerComponent);
                return EmptyUIState;
            }

            if (!TryComp<DamageableComponent>(containedBody, out DamageableComponent? damageable))
                return EmptyUIState;

            if (!TryComp<HumanoidAppearanceComponent>(containedBody, out HumanoidAppearanceComponent? humanoid))
                return EmptyUIState;

            if (!TryComp<MindComponent>(containedBody, out MindComponent? mindComponent) || mindComponent.Mind == null)
                return EmptyUIState;

            bool isScanned = _cloningSystem.HasDnaScan(mindComponent.Mind);

            return new MedicalScannerBoundUserInterfaceState(containedBody, !isScanned);
        }

        private void UpdateUserInterface(EntityUid uid, MedicalScannerComponent scannerComponent)
        {
            if (!IsPowered(scannerComponent))
            {
                return;
            }

            var newState = GetUserInterfaceState(uid, scannerComponent);
            scannerComponent.UserInterface?.SetState(newState);
        }

        private MedicalScannerStatus GetStatus(MedicalScannerComponent scannerComponent)
        {
            if (IsPowered(scannerComponent))
            {
                var body = scannerComponent.BodyContainer.ContainedEntity;
                if (body == null)
                    return MedicalScannerStatus.Open;

                if (!TryComp<MobStateComponent>(body.Value, out var state))
                {
                    return MedicalScannerStatus.Open;
                }

                return GetStatusFromDamageState(state);
            }
            return MedicalScannerStatus.Off;
        }

        public bool IsPowered(MedicalScannerComponent scannerComponent)
        {
            if (TryComp<ApcPowerReceiverComponent>(scannerComponent.Owner, out var receiver))
            {
                return receiver.Powered;
            }
            return false;
        }

        public bool IsOccupied(MedicalScannerComponent scannerComponent)
        {
            if (scannerComponent.BodyContainer != null)
            {
                return scannerComponent.BodyContainer.ContainedEntity != null;
            }
            return false;
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

        private void UpdateAppearance(EntityUid uid, MedicalScannerComponent scannerComponent)
        {
            if (TryComp<AppearanceComponent>(scannerComponent.Owner, out var appearance))
            {
                appearance.SetData(MedicalScannerVisuals.Status, GetStatus(scannerComponent));
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _updateDif += frameTime;
            if (_updateDif < UpdateRate)
                return;
            _updateDif -= UpdateRate;

            foreach (var scanner in EntityQuery<MedicalScannerComponent>())
            {
                UpdateAppearance(scanner.Owner, scanner);
            }
        }

        public void InsertBody(EntityUid uid, EntityUid user, MedicalScannerComponent? scannerComponent)
        {
            if (!Resolve(uid, ref scannerComponent))
                return;

            if (scannerComponent.BodyContainer == null || scannerComponent.BodyContainer.ContainedEntity != null)
                return;

            if (!TryComp<MobStateComponent>(user, out MobStateComponent? comp))
                return;

            scannerComponent.BodyContainer.Insert(user);
            UpdateUserInterface(uid, scannerComponent);
            UpdateAppearance(scannerComponent.Owner, scannerComponent);
        }

        public void EjectBody(EntityUid uid, MedicalScannerComponent? scannerComponent)
        {
            if (!Resolve(uid, ref scannerComponent))
                return;

            if (scannerComponent.BodyContainer.ContainedEntity is not {Valid: true} contained) return;

            scannerComponent.BodyContainer.Remove(contained);
            _climbSystem.ForciblySetClimbing(contained);
            UpdateUserInterface(uid, scannerComponent);
            UpdateAppearance(scannerComponent.Owner, scannerComponent);
        }

        public void TrySaveCloningData(EntityUid uid, MedicalScannerComponent? scannerComponent)
        {
            if (!Resolve(uid, ref scannerComponent))
                return;

            EntityUid? body = scannerComponent.BodyContainer.ContainedEntity;

            if (body == null)
                return;

            // Check to see if they are humanoid
            if (!TryComp<HumanoidAppearanceComponent>(body, out HumanoidAppearanceComponent? humanoid))
            {
                _popupSystem.PopupEntity(Loc.GetString("medical-scanner-component-msg-no-humanoid-component"), uid, Filter.Pvs(uid));
                return;
            }

            if (!TryComp<MindComponent>(body, out MindComponent? mindComp) || mindComp.Mind == null)
            {
                _popupSystem.PopupEntity(Loc.GetString("medical-scanner-component-msg-no-soul"), uid, Filter.Pvs(uid));
                return;
            }

            // Null suppression based on above check. Yes, it's explicitly needed
            var mind = mindComp.Mind;
            // We need the HumanoidCharacterProfile
            // TODO: Move this further 'outwards' into a DNAComponent or somesuch.
            // Ideally this ends with GameTicker & CloningSystem handing DNA to a function that sets up a body for that DNA.
            var mindUser = mind.UserId;

            if (mindUser.HasValue == false || mind.Session == null)
            {
                // For now assume this means soul departed
                _popupSystem.PopupEntity(Loc.GetString("medical-scanner-component-msg-soul-broken"), uid, Filter.Pvs(uid));
                return;
            }

             // TODO get synchronously
             //  This must be changed to grab the details of the mob itself, not session preferences
            var profile = GetPlayerProfileAsync(mindUser.Value);
            _cloningSystem.AddToDnaScans(new ClonerDNAEntry(mind, profile));
            UpdateUserInterface(uid, scannerComponent);
        }

        private HumanoidCharacterProfile GetPlayerProfileAsync(NetUserId userId)
        {
            return (HumanoidCharacterProfile) _prefsManager.GetPreferences(userId).SelectedCharacter;
        }
    }
}
