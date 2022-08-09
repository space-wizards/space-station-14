using Content.Server.Cloning.Components;
using Content.Server.Mind.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.GameTicking;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.Species;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Content.Server.EUI;
using Robust.Shared.Containers;
using Robust.Server.Containers;
using Content.Shared.Cloning;
using Content.Server.MachineLinking.System;
using Content.Server.MachineLinking.Events;
using Content.Server.MobState;

namespace Content.Server.Cloning.Systems
{
    public sealed class CloningSystem : EntitySystem
    {
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;
        [Dependency] private readonly IPlayerManager _playerManager = null!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly EuiManager _euiManager = null!;
        [Dependency] private readonly CloningConsoleSystem _cloningConsoleSystem = default!;
        [Dependency] private readonly SharedHumanoidAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly ContainerSystem _containerSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
        public readonly Dictionary<Mind.Mind, EntityUid> ClonesWaitingForMind = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CloningPodComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<BeingClonedComponent, MindAddedMessage>(HandleMindAdded);
            SubscribeLocalEvent<CloningPodComponent, PortDisconnectedEvent>(OnPortDisconnected);
            SubscribeLocalEvent<CloningPodComponent, AnchorStateChangedEvent>(OnAnchor);
        }

        private void OnComponentInit(EntityUid uid, CloningPodComponent clonePod, ComponentInit args)
        {
            clonePod.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(clonePod.Owner, "clonepod-bodyContainer");
            _signalSystem.EnsureReceiverPorts(uid, CloningPodComponent.PodPort);
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

        private void OnPortDisconnected(EntityUid uid, CloningPodComponent pod, PortDisconnectedEvent args)
        {
            pod.ConnectedConsole = null;
        }

        private void OnAnchor(EntityUid uid, CloningPodComponent component, ref AnchorStateChangedEvent args)
        {
            if (component.ConnectedConsole == null || !TryComp<CloningConsoleComponent>(component.ConnectedConsole, out var console))
                return;

            if (args.Anchored)
            {
                _cloningConsoleSystem.RecheckConnections(component.ConnectedConsole.Value, uid, console.GeneticScanner, console);
                return;
            }
            _cloningConsoleSystem.UpdateUserInterface(console);
        }

        public bool TryCloning(EntityUid uid, EntityUid bodyToClone, Mind.Mind mind, CloningPodComponent? clonePod)
        {
            if (!Resolve(uid, ref clonePod) || bodyToClone == null)
                return false;

            if (ClonesWaitingForMind.TryGetValue(mind, out var clone))
            {
                if (EntityManager.EntityExists(clone) &&
                    !_mobStateSystem.IsDead(clone) &&
                    TryComp<MindComponent>(clone, out var cloneMindComp) &&
                    (cloneMindComp.Mind == null || cloneMindComp.Mind == mind))
                    return false; // Mind already has clone

                ClonesWaitingForMind.Remove(mind);
            }

            if (mind.OwnedEntity != null && !_mobStateSystem.IsDead(mind.OwnedEntity.Value))
                return false; // Body controlled by mind is not dead

            // Yes, we still need to track down the client because we need to open the Eui
            if (mind.UserId == null || !_playerManager.TryGetSessionById(mind.UserId.Value, out var client))
                return false; // If we can't track down the client, we can't offer transfer. That'd be quite bad.

            if (!TryComp<HumanoidAppearanceComponent>(bodyToClone, out var humanoid))
                return false; // whatever body was to be cloned, was not a humanoid

            var speciesProto = _prototype.Index<SpeciesPrototype>(humanoid.Species).Prototype;
            var mob = Spawn(speciesProto, Transform(clonePod.Owner).MapPosition);
            _appearanceSystem.UpdateAppearance(mob, humanoid.Appearance);
            _appearanceSystem.UpdateSexGender(mob, humanoid.Sex, humanoid.Gender);

            MetaData(mob).EntityName = MetaData(bodyToClone).EntityName;

            var cloneMindReturn = EntityManager.AddComponent<BeingClonedComponent>(mob);
            cloneMindReturn.Mind = mind;
            cloneMindReturn.Parent = clonePod.Owner;
            clonePod.BodyContainer.Insert(mob);
            clonePod.CapturedMind = mind;
            ClonesWaitingForMind.Add(mind, mob);
            UpdateStatus(CloningPodStatus.NoMind, clonePod);
            _euiManager.OpenEui(new AcceptCloningEui(mind, this), client);

            AddComp<ActiveCloningPodComponent>(uid);
            return true;
        }

        public void UpdateStatus(CloningPodStatus status, CloningPodComponent cloningPod)
        {
            cloningPod.Status = status;
            UpdateAppearance(cloningPod);
        }

        public override void Update(float frameTime)
        {
            foreach (var (_, cloning) in EntityManager.EntityQuery<ActiveCloningPodComponent, CloningPodComponent>())
            {
                if (!_powerReceiverSystem.IsPowered(cloning.Owner))
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
            RemCompDeferred<ActiveCloningPodComponent>(uid);
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            ClonesWaitingForMind.Clear();
        }
    }
}
