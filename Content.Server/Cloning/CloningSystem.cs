using Content.Server.Cloning.Components;
using Content.Server.Mind.Components;
using Content.Server.Power.Components;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Server.Climbing;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.MobState.Components;
using Content.Shared.Species;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Content.Server.EUI;
using Robust.Shared.Containers;
using Content.Shared.Cloning;

namespace Content.Server.Cloning
{
    internal sealed class CloningSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = null!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly EuiManager _euiManager = null!;
        [Dependency] private readonly CloningSystem _cloningSystem = default!;
        [Dependency] private readonly ClimbSystem _climbSystem = default!;
        public readonly Dictionary<Mind.Mind, EntityUid> ClonesWaitingForMind = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CloningPodComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<BeingClonedComponent, MindAddedMessage>(HandleMindAdded);
        }

        private void OnComponentInit(EntityUid uid, CloningPodComponent clonePod, ComponentInit args)
        {
            clonePod.BodyContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(clonePod.Owner, $"{Name}-bodyContainer");
        }

        private void UpdateAppearance(CloningPodComponent clonePod)
        {
            if (TryComp<AppearanceComponent>(clonePod.Owner, out var appearance))
                appearance.SetData(CloningPodVisuals.Status, clonePod.Status);
        }

        internal void TransferMindToClone(Mind.Mind mind)
        {
            if (!ClonesWaitingForMind.TryGetValue(mind, out var entity) ||
                !EntityManager.EntityExists(entity) ||
                !TryComp<MindComponent>(entity, out var mindComp) ||
                mindComp.Mind != null)
                return;

            mind.TransferTo(entity, ghostCheckOverride: true);
            mind.UnVisit();
            ClonesWaitingForMind.Remove(mind);
        }

        private void HandleMindAdded(EntityUid uid, BeingClonedComponent clonedComponent, MindAddedMessage message)
        {
            if (clonedComponent.Parent == EntityUid.Invalid ||
                !EntityManager.EntityExists(clonedComponent.Parent) ||
                !TryComp<CloningPodComponent>(clonedComponent.Parent, out var cloningPodComponent) ||
                clonedComponent.Owner != cloningPodComponent.BodyContainer?.ContainedEntity)
            {
                EntityManager.RemoveComponent<BeingClonedComponent>(clonedComponent.Owner);
                return;
            }
            UpdateStatus(CloningPodStatus.Cloning, cloningPodComponent);
        }

        public bool IsPowered(CloningPodComponent clonepod)
        {
            if (!TryComp<ApcPowerReceiverComponent>(clonepod.Owner, out var receiver))
                return false;

            return receiver.Powered;
        }

        public bool TryCloning(EntityUid uid, Mind.Mind mind, HumanoidCharacterProfile hcp, CloningPodComponent? clonePod)
        {
            if (!Resolve(uid, ref clonePod))
                return false;

            if (clonePod.BodyContainer.ContainedEntity != null)
                return false;

            if (_cloningSystem.ClonesWaitingForMind.TryGetValue(mind, out var clone))
            {
                if (EntityManager.EntityExists(clone) &&
                    TryComp<MobStateComponent>(clone, out var cloneState) &&
                    !cloneState.IsDead() &&
                    TryComp<MindComponent>(clone, out var cloneMindComp) &&
                    (cloneMindComp.Mind == null || cloneMindComp.Mind == mind))
                    return false; // Mind already has clone

                _cloningSystem.ClonesWaitingForMind.Remove(mind);
            }

            if (mind.OwnedEntity != null &&
                TryComp<MobStateComponent?>(mind.OwnedEntity.Value, out var state) &&
                !state.IsDead())
                return false; // Body controlled by mind is not dead

            // Yes, we still need to track down the client because we need to open the Eui
            if (mind.UserId == null || !_playerManager.TryGetSessionById(mind.UserId.Value, out var client))
                return false; // If we can't track down the client, we can't offer transfer. That'd be quite bad.

            if (!TryComp<TransformComponent>(clonePod.Owner, out var transform))
                return false;

            // Get species from player profile, this needs to get it from entity getting cloned instead
            // This is currently the reason that someone can get scanned after being changed/chose ghost role and will be cloned
            // as the one from their player profile.
            // CHANGE THIS IN THE FUTURE TO GRAB DETAILS FROM SCANNED MOB
            var speciesProto = _prototype.Index<SpeciesPrototype>(hcp.Species).Prototype;
            var mob = Spawn(speciesProto, transform.MapPosition);
            EntitySystem.Get<SharedHumanoidAppearanceSystem>().UpdateFromProfile(mob, hcp);

            // set name if they have it
            if (TryComp<MetaDataComponent>(mob, out var meta))
                meta.EntityName = hcp.Name;

            var cloneMindReturn = EntityManager.AddComponent<BeingClonedComponent>(mob);
            cloneMindReturn.Mind = mind;
            cloneMindReturn.Parent = clonePod.Owner;
            clonePod.BodyContainer.Insert(mob);
            clonePod.CapturedMind = mind;
            _cloningSystem.ClonesWaitingForMind.Add(mind, mob);
            UpdateStatus(CloningPodStatus.NoMind, clonePod);
            _euiManager.OpenEui(new AcceptCloningEui(mind), client);
            return true;
        }

        public void UpdateStatus(CloningPodStatus status, CloningPodComponent cloningPod)
        {
            cloningPod.Status = status;
            UpdateAppearance(cloningPod);
        }

        public override void Update(float frameTime)
        {
            foreach (var cloning in EntityManager.EntityQuery<CloningPodComponent>())
            {
                if (!IsPowered(cloning))
                    continue;

                if (cloning.BodyContainer.ContainedEntity != null)
                {
                    cloning.CloningProgress += frameTime;
                    cloning.CloningProgress = MathHelper.Clamp(cloning.CloningProgress, 0f, cloning.CloningTime);
                }

                if (cloning.CapturedMind?.Session?.AttachedEntity == cloning.BodyContainer.ContainedEntity)
                {
                    Eject(cloning.Owner, cloning);
                }
            }
        }

        public void Eject(EntityUid uid, CloningPodComponent? clonePod)
        {
            if (!Resolve(uid, ref clonePod))
                return;

            if (clonePod.BodyContainer.ContainedEntity is not {Valid: true} entity || clonePod.CloningProgress < clonePod.CloningTime)
                return;

            EntityManager.RemoveComponent<BeingClonedComponent>(entity);
            clonePod.BodyContainer.Remove(entity);
            clonePod.CapturedMind = null;
            clonePod.CloningProgress = 0f;
            UpdateStatus(CloningPodStatus.Idle, clonePod);
            _climbSystem.ForciblySetClimbing(entity);
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            ClonesWaitingForMind.Clear();
        }
    }
}
