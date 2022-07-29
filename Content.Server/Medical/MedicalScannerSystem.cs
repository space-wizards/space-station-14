using Content.Server.Climbing;
using Content.Server.Cloning;
using Content.Server.Medical.Components;
using Content.Server.Mind.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Preferences.Managers;
using Content.Shared.ActionBlocker;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
using Content.Shared.Movement;
using Content.Shared.Movement.Events;
using Content.Shared.Preferences;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using static Content.Shared.MedicalScanner.SharedMedicalScannerComponent;

namespace Content.Server.Medical
{
    public sealed class MedicalScannerSystem : EntitySystem
    {
        [Dependency] private readonly IServerPreferencesManager _prefsManager = null!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly ClimbSystem _climbSystem = default!;
        [Dependency] private readonly CloningSystem _cloningSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        private const float UpdateRate = 1f;
        private float _updateDif;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MedicalScannerComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<MedicalScannerComponent, ActivateInWorldEvent>(OnActivated);
            SubscribeLocalEvent<MedicalScannerComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
            SubscribeLocalEvent<MedicalScannerComponent, GetVerbsEvent<InteractionVerb>>(AddInsertOtherVerb);
            SubscribeLocalEvent<MedicalScannerComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
            SubscribeLocalEvent<MedicalScannerComponent, DestructionEventArgs>(OnDestroyed);
            SubscribeLocalEvent<MedicalScannerComponent, DragDropEvent>(HandleDragDropOn);
            SubscribeLocalEvent<MedicalScannerComponent, ScanButtonPressedMessage>(OnScanButtonPressed);
        }

        private void OnComponentInit(EntityUid uid, MedicalScannerComponent scannerComponent, ComponentInit args)
        {
            base.Initialize();

            scannerComponent.BodyContainer = scannerComponent.Owner.EnsureContainer<ContainerSlot>($"{scannerComponent.Name}-bodyContainer");
            UpdateUserInterface(uid, scannerComponent);
        }

        private void OnActivated(EntityUid uid, MedicalScannerComponent scannerComponent, ActivateInWorldEvent args)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            UpdateUserInterface(uid, scannerComponent);
        }

        private void OnRelayMovement(EntityUid uid, MedicalScannerComponent scannerComponent, ref ContainerRelayMovementEntityEvent args)
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
            if (TryComp<MetaDataComponent>(args.Using.Value, out var metadata))
                name = metadata.EntityName;

            InteractionVerb verb = new()
            {
                Act = () => InsertBody(component.Owner, args.Target, component),
                Category = VerbCategory.Insert,
                Text = name
            };
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

            // Self-insert verb
            if (!IsOccupied(component) &&
                component.CanInsert(args.User) &&
                _blocker.CanMove(args.User))
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
            new(false);

        private MedicalScannerBoundUserInterfaceState GetUserInterfaceState(EntityUid uid,  MedicalScannerComponent scannerComponent)
        {
            EntityUid? containedBody = scannerComponent.BodyContainer.ContainedEntity;

            if (containedBody == null)
            {
                UpdateAppearance(uid, scannerComponent);
                return EmptyUIState;
            }

            if (!HasComp<DamageableComponent>(containedBody))
                return EmptyUIState;

            if (!HasComp<HumanoidAppearanceComponent>(containedBody))
                return EmptyUIState;

            if (!TryComp<MindComponent>(containedBody, out var mindComponent) || mindComponent.Mind == null)
                return EmptyUIState;

            bool isScanned = _cloningSystem.HasDnaScan(mindComponent.Mind);

            return new MedicalScannerBoundUserInterfaceState(!isScanned);
        }

        private void UpdateUserInterface(EntityUid uid, MedicalScannerComponent scannerComponent)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            var newState = GetUserInterfaceState(uid, scannerComponent);
            scannerComponent.UserInterface?.SetState(newState);
        }

        private MedicalScannerStatus GetStatus(MedicalScannerComponent scannerComponent)
        {
            if (this.IsPowered(scannerComponent.Owner, EntityManager))
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

        public bool IsOccupied(MedicalScannerComponent scannerComponent)
        {
            return scannerComponent.BodyContainer.ContainedEntity != null;
        }

        private MedicalScannerStatus GetStatusFromDamageState(MobStateComponent state)
        {
            if (state.IsAlive())
                return MedicalScannerStatus.Green;

            if (state.IsCritical())
                return MedicalScannerStatus.Red;

            if (state.IsDead())
                return MedicalScannerStatus.Death;

            return MedicalScannerStatus.Yellow;
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

            if (scannerComponent.BodyContainer.ContainedEntity != null)
                return;

            if (!TryComp<MobStateComponent>(user, out var comp))
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
            _climbSystem.ForciblySetClimbing(contained, uid);
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
            if (!TryComp<HumanoidAppearanceComponent>(body, out var humanoid))
            {
                _popupSystem.PopupEntity(Loc.GetString("medical-scanner-component-msg-no-humanoid-component"), uid, Filter.Pvs(uid));
                return;
            }

            if (!TryComp<MindComponent>(body, out var mindComp) || mindComp.Mind == null)
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
