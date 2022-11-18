using Content.Shared.GameTicking;
using Content.Shared.Damage;
using Content.Shared.Stacks;
using Content.Shared.Examine;
using Content.Shared.Cloning;
using Content.Shared.Atmos;
using Content.Shared.CCVar;
using Content.Server.Cloning.Components;
using Content.Server.Mind.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Atmos.EntitySystems;
using Content.Server.EUI;
using Content.Server.Humanoid;
using Content.Server.MachineLinking.System;
using Content.Server.MachineLinking.Events;
using Content.Server.MobState;
using Content.Shared.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Materials;
using Content.Server.Stack;
using Content.Server.Jobs;
using Content.Shared.Humanoid.Prototypes;
using Robust.Server.GameObjects;
using Robust.Server.Containers;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;

namespace Content.Server.Cloning
{
    public sealed class CloningSystem : EntitySystem
    {
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;
        [Dependency] private readonly IPlayerManager _playerManager = null!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly EuiManager _euiManager = null!;
        [Dependency] private readonly CloningConsoleSystem _cloningConsoleSystem = default!;
        [Dependency] private readonly HumanoidSystem _humanoidSystem = default!;
        [Dependency] private readonly ContainerSystem _containerSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedStackSystem _stackSystem = default!;
        [Dependency] private readonly StackSystem _serverStackSystem = default!;
        [Dependency] private readonly SpillableSystem _spillableSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly MaterialStorageSystem _material = default!;

        public readonly Dictionary<Mind.Mind, EntityUid> ClonesWaitingForMind = new();
        public const float EasyModeCloningCost = 0.7f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CloningPodComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<CloningPodComponent, RefreshPartsEvent>(OnPartsRefreshed);
            SubscribeLocalEvent<CloningPodComponent, UpgradeExamineEvent>(OnUpgradeExamine);
            SubscribeLocalEvent<CloningPodComponent, MachineDeconstructedEvent>(OnDeconstruct);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<BeingClonedComponent, MindAddedMessage>(HandleMindAdded);
            SubscribeLocalEvent<CloningPodComponent, PortDisconnectedEvent>(OnPortDisconnected);
            SubscribeLocalEvent<CloningPodComponent, AnchorStateChangedEvent>(OnAnchor);
            SubscribeLocalEvent<CloningPodComponent, ExaminedEvent>(OnExamined);
        }

        private void OnComponentInit(EntityUid uid, CloningPodComponent clonePod, ComponentInit args)
        {
            clonePod.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(clonePod.Owner, "clonepod-bodyContainer");
            _signalSystem.EnsureReceiverPorts(uid, CloningPodComponent.PodPort);
        }

        private void OnPartsRefreshed(EntityUid uid, CloningPodComponent component, RefreshPartsEvent args)
        {
            var materialRating = args.PartRatings[component.MachinePartMaterialUse];
            var speedRating = args.PartRatings[component.MachinePartCloningSpeed];

            component.BiomassRequirementMultiplier = MathF.Pow(component.PartRatingMaterialMultiplier, materialRating - 1);
            component.CloningTime = component.BaseCloningTime * MathF.Pow(component.PartRatingSpeedMultiplier, speedRating - 1);
        }

        private void OnUpgradeExamine(EntityUid uid, CloningPodComponent component, UpgradeExamineEvent args)
        {
            args.AddPercentageUpgrade("cloning-pod-component-upgrade-speed", component.BaseCloningTime / component.CloningTime);
            args.AddPercentageUpgrade("cloning-pod-component-upgrade-biomass-requirement", component.BiomassRequirementMultiplier);
        }

        private void OnDeconstruct(EntityUid uid, CloningPodComponent component, MachineDeconstructedEvent args)
        {
            _serverStackSystem.SpawnMultiple(component.MaterialCloningOutput, _material.GetMaterialAmount(uid, component.RequiredMaterial), Transform(uid).Coordinates);
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
                clonedComponent.Owner != cloningPodComponent.BodyContainer.ContainedEntity)
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

        private void OnExamined(EntityUid uid, CloningPodComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange || !_powerReceiverSystem.IsPowered(uid))
                return;

            args.PushMarkup(Loc.GetString("cloning-pod-biomass", ("number", _material.GetMaterialAmount(uid, component.RequiredMaterial))));
        }

        public bool TryCloning(EntityUid uid, EntityUid bodyToClone, Mind.Mind mind, CloningPodComponent? clonePod, float failChanceModifier = 1)
        {
            if (!Resolve(uid, ref clonePod))
                return false;

            if (HasComp<ActiveCloningPodComponent>(uid))
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

            if (!TryComp<HumanoidComponent>(bodyToClone, out var humanoid))
                return false; // whatever body was to be cloned, was not a humanoid

            if (!_prototype.TryIndex<SpeciesPrototype>(humanoid.Species, out var speciesPrototype))
                return false;

            if (!TryComp<PhysicsComponent>(bodyToClone, out var physics))
                return false;

            var cloningCost = (int) Math.Round(physics.FixturesMass * clonePod.BiomassRequirementMultiplier);

            if (_configManager.GetCVar(CCVars.BiomassEasyMode))
                cloningCost = (int) Math.Round(cloningCost * EasyModeCloningCost);

            // biomass checks
            var biomassAmount = _material.GetMaterialAmount(uid, clonePod.RequiredMaterial);

            if (biomassAmount < cloningCost)
            {
                if (clonePod.ConnectedConsole != null)
                    _chatSystem.TrySendInGameICMessage(clonePod.ConnectedConsole.Value, Loc.GetString("cloning-console-chat-error", ("units", cloningCost)), InGameICChatType.Speak, false);
                return false;
            }

            _material.TryChangeMaterialAmount(uid, clonePod.RequiredMaterial, -cloningCost);
            clonePod.UsedBiomass = cloningCost;
            // end of biomass checks

            // genetic damage checks
            if (TryComp<DamageableComponent>(bodyToClone, out var damageable) &&
                damageable.Damage.DamageDict.TryGetValue("Cellular", out var cellularDmg))
            {
                var chance = Math.Clamp((float) (cellularDmg / 100), 0, 1);
                chance *= failChanceModifier;

                if (cellularDmg > 0 && clonePod.ConnectedConsole != null)
                    _chatSystem.TrySendInGameICMessage(clonePod.ConnectedConsole.Value, Loc.GetString("cloning-console-cellular-warning", ("percent", Math.Round(100 - (chance * 100)))), InGameICChatType.Speak, false);

                if (_robustRandom.Prob(chance))
                {
                    UpdateStatus(CloningPodStatus.Gore, clonePod);
                    clonePod.FailedClone = true;
                    AddComp<ActiveCloningPodComponent>(uid);
                    return true;
                }
            }
            // end of genetic damage checks

            var mob = Spawn(speciesPrototype.Prototype, Transform(clonePod.Owner).MapPosition);
            _humanoidSystem.CloneAppearance(bodyToClone, mob);

            MetaData(mob).EntityName = MetaData(bodyToClone).EntityName;

            var cloneMindReturn = EntityManager.AddComponent<BeingClonedComponent>(mob);
            cloneMindReturn.Mind = mind;
            cloneMindReturn.Parent = clonePod.Owner;
            clonePod.BodyContainer.Insert(mob);
            ClonesWaitingForMind.Add(mind, mob);
            UpdateStatus(CloningPodStatus.NoMind, clonePod);
            _euiManager.OpenEui(new AcceptCloningEui(mind, this), client);

            AddComp<ActiveCloningPodComponent>(uid);

            // TODO: Ideally, components like this should be on a mind entity so this isn't neccesary.
            // Remove this when 'mind entities' are added.
            // Add on special job components to the mob.
            if (mind.CurrentJob != null)
            {
                foreach (var special in mind.CurrentJob.Prototype.Special)
                {
                    if (special is AddComponentSpecial)
                        special.AfterEquip(mob);
                }
            }

            return true;
        }

        public void UpdateStatus(CloningPodStatus status, CloningPodComponent cloningPod)
        {
            cloningPod.Status = status;
            _appearance.SetData(cloningPod.Owner, CloningPodVisuals.Status, cloningPod.Status);
        }

        public override void Update(float frameTime)
        {
            foreach (var (_, cloning) in EntityManager.EntityQuery<ActiveCloningPodComponent, CloningPodComponent>())
            {
                if (!_powerReceiverSystem.IsPowered(cloning.Owner))
                    continue;

                if (cloning.BodyContainer.ContainedEntity == null && !cloning.FailedClone)
                    continue;

                cloning.CloningProgress += frameTime;
                if (cloning.CloningProgress < cloning.CloningTime)
                    continue;

                if (cloning.FailedClone)
                    EndFailedCloning(cloning.Owner, cloning);
                else
                    Eject(cloning.Owner, cloning);
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
            clonePod.CloningProgress = 0f;
            clonePod.UsedBiomass = 0;
            UpdateStatus(CloningPodStatus.Idle, clonePod);
            RemCompDeferred<ActiveCloningPodComponent>(uid);
        }

        private void EndFailedCloning(EntityUid uid, CloningPodComponent clonePod)
        {
            clonePod.FailedClone = false;
            clonePod.CloningProgress = 0f;
            UpdateStatus(CloningPodStatus.Idle, clonePod);
            var transform = Transform(uid);
            var indices = _transformSystem.GetGridOrMapTilePosition(uid);

            var tileMix = _atmosphereSystem.GetTileMixture(transform.GridUid, null, indices, true);

            Solution bloodSolution = new();

            int i = 0;
            while (i < 1)
            {
                tileMix?.AdjustMoles(Gas.Miasma, 6f);
                bloodSolution.AddReagent("Blood", 50);
                if (_robustRandom.Prob(0.2f))
                    i++;
            }
            _spillableSystem.SpillAt(uid, bloodSolution, "PuddleBlood");

            var biomassStack = Spawn(clonePod.MaterialCloningOutput, transform.Coordinates);
            _stackSystem.SetCount(biomassStack, _robustRandom.Next(1, (int) (clonePod.UsedBiomass / 2.5)));

            clonePod.UsedBiomass = 0;
            RemCompDeferred<ActiveCloningPodComponent>(uid);
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            ClonesWaitingForMind.Clear();
        }
    }
}
