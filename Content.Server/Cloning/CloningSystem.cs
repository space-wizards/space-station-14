using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Cloning.Components;
using Content.Server.Construction;
using Content.Server.DeviceLinking.Systems;
using Content.Server.EUI;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Humanoid;
using Content.Server.Jobs;
using Content.Server.Materials;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Traits.Assorted;
using Content.Shared.Atmos;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.Components;
using Content.Shared.Cloning;
using Content.Shared.Damage;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles.Jobs;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Cloning
{
    public sealed class CloningSystem : EntitySystem
    {
        [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
        [Dependency] private readonly IPlayerManager _playerManager = null!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly EuiManager _euiManager = null!;
        [Dependency] private readonly CloningConsoleSystem _cloningConsoleSystem = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _humanoidSystem = default!;
        [Dependency] private readonly ContainerSystem _containerSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly PuddleSystem _puddleSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly MaterialStorageSystem _material = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;
        [Dependency] private readonly MetaDataSystem _metaSystem = default!;
        [Dependency] private readonly SharedJobSystem _jobs = default!;

        public readonly Dictionary<MindComponent, EntityUid> ClonesWaitingForMind = new();
        public const float EasyModeCloningCost = 0.7f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CloningPodComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<CloningPodComponent, RefreshPartsEvent>(OnPartsRefreshed);
            SubscribeLocalEvent<CloningPodComponent, UpgradeExamineEvent>(OnUpgradeExamine);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<BeingClonedComponent, MindAddedMessage>(HandleMindAdded);
            SubscribeLocalEvent<CloningPodComponent, PortDisconnectedEvent>(OnPortDisconnected);
            SubscribeLocalEvent<CloningPodComponent, AnchorStateChangedEvent>(OnAnchor);
            SubscribeLocalEvent<CloningPodComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<CloningPodComponent, GotEmaggedEvent>(OnEmagged);
        }

        private void OnComponentInit(EntityUid uid, CloningPodComponent clonePod, ComponentInit args)
        {
            clonePod.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, "clonepod-bodyContainer");
            _signalSystem.EnsureSinkPorts(uid, CloningPodComponent.PodPort);
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

        internal void TransferMindToClone(EntityUid mindId, MindComponent mind)
        {
            if (!ClonesWaitingForMind.TryGetValue(mind, out var entity) ||
                !EntityManager.EntityExists(entity) ||
                !TryComp<MindContainerComponent>(entity, out var mindComp) ||
                mindComp.Mind != null)
                return;

            _mindSystem.TransferTo(mindId, entity, ghostCheckOverride: true, mind: mind);
            _mindSystem.UnVisit(mindId, mind);
            ClonesWaitingForMind.Remove(mind);
        }

        private void HandleMindAdded(EntityUid uid, BeingClonedComponent clonedComponent, MindAddedMessage message)
        {
            if (clonedComponent.Parent == EntityUid.Invalid ||
                !EntityManager.EntityExists(clonedComponent.Parent) ||
                !TryComp<CloningPodComponent>(clonedComponent.Parent, out var cloningPodComponent) ||
                uid != cloningPodComponent.BodyContainer.ContainedEntity)
            {
                EntityManager.RemoveComponent<BeingClonedComponent>(uid);
                return;
            }
            UpdateStatus(clonedComponent.Parent, CloningPodStatus.Cloning, cloningPodComponent);
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
            _cloningConsoleSystem.UpdateUserInterface(component.ConnectedConsole.Value, console);
        }

        private void OnExamined(EntityUid uid, CloningPodComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange || !_powerReceiverSystem.IsPowered(uid))
                return;

            args.PushMarkup(Loc.GetString("cloning-pod-biomass", ("number", _material.GetMaterialAmount(uid, component.RequiredMaterial))));
        }

        public bool TryCloning(EntityUid uid, EntityUid bodyToClone, MindComponent mind, CloningPodComponent? clonePod, float failChanceModifier = 1)
        {
            if (!Resolve(uid, ref clonePod))
                return false;

            if (HasComp<ActiveCloningPodComponent>(uid))
                return false;

            if (ClonesWaitingForMind.TryGetValue(mind, out var clone))
            {
                if (EntityManager.EntityExists(clone) &&
                    !_mobStateSystem.IsDead(clone) &&
                    TryComp<MindContainerComponent>(clone, out var cloneMindComp) &&
                    (cloneMindComp.Mind == null || cloneMindComp.Mind == mind.Owner))
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

            if (!_prototype.TryIndex<SpeciesPrototype>(humanoid.Species, out var speciesPrototype))
                return false;

            if (!TryComp<PhysicsComponent>(bodyToClone, out var physics))
                return false;

            var cloningCost = (int) Math.Round(physics.FixturesMass * clonePod.BiomassRequirementMultiplier);

            if (_configManager.GetCVar(CCVars.BiomassEasyMode))
                cloningCost = (int) Math.Round(cloningCost * EasyModeCloningCost);

            // Check if they have the uncloneable trait
            if (TryComp<UncloneableComponent>(bodyToClone, out _))
            {
                if (clonePod.ConnectedConsole != null)
                    _chatSystem.TrySendInGameICMessage(clonePod.ConnectedConsole.Value,
                        Loc.GetString("cloning-console-uncloneable-trait-error"),
                        InGameICChatType.Speak, false);
                return false;
            }

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
                    _chatSystem.TrySendInGameICMessage(clonePod.ConnectedConsole.Value, Loc.GetString("cloning-console-cellular-warning", ("percent", Math.Round(100 - chance * 100))), InGameICChatType.Speak, false);

                if (_robustRandom.Prob(chance))
                {
                    UpdateStatus(uid, CloningPodStatus.Gore, clonePod);
                    clonePod.FailedClone = true;
                    AddComp<ActiveCloningPodComponent>(uid);
                    return true;
                }
            }
            // end of genetic damage checks

            var mob = Spawn(speciesPrototype.Prototype, Transform(uid).MapPosition);
            _humanoidSystem.CloneAppearance(bodyToClone, mob);

            var ev = new CloningEvent(bodyToClone, mob);
            RaiseLocalEvent(bodyToClone, ref ev);

            if (!ev.NameHandled)
                _metaSystem.SetEntityName(mob, MetaData(bodyToClone).EntityName);

            var cloneMindReturn = EntityManager.AddComponent<BeingClonedComponent>(mob);
            cloneMindReturn.Mind = mind;
            cloneMindReturn.Parent = uid;
            clonePod.BodyContainer.Insert(mob);
            ClonesWaitingForMind.Add(mind, mob);
            UpdateStatus(uid, CloningPodStatus.NoMind, clonePod);
            var mindId = mind.Owner;
            _euiManager.OpenEui(new AcceptCloningEui(mindId, mind, this), client);

            AddComp<ActiveCloningPodComponent>(uid);

            // TODO: Ideally, components like this should be components on the mind entity so this isn't necessary.
            // Add on special job components to the mob.
            if (_jobs.MindTryGetJob(mindId, out _, out var prototype))
            {
                foreach (var special in prototype.Special)
                {
                    if (special is AddComponentSpecial)
                        special.AfterEquip(mob);
                }
            }

            return true;
        }

        public void UpdateStatus(EntityUid podUid, CloningPodStatus status, CloningPodComponent cloningPod)
        {
            cloningPod.Status = status;
            _appearance.SetData(podUid, CloningPodVisuals.Status, cloningPod.Status);
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<ActiveCloningPodComponent, CloningPodComponent>();
            while (query.MoveNext(out var uid, out var _, out var cloning))
            {
                if (!_powerReceiverSystem.IsPowered(uid))
                    continue;

                if (cloning.BodyContainer.ContainedEntity == null && !cloning.FailedClone)
                    continue;

                cloning.CloningProgress += frameTime;
                if (cloning.CloningProgress < cloning.CloningTime)
                    continue;

                if (cloning.FailedClone)
                    EndFailedCloning(uid, cloning);
                else
                    Eject(uid, cloning);
            }
        }

        /// <summary>
        /// On emag, spawns a failed clone when cloning process fails which attacks nearby crew.
        /// </summary>
        private void OnEmagged(EntityUid uid, CloningPodComponent clonePod, ref GotEmaggedEvent args)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            _audio.PlayPvs(clonePod.SparkSound, uid);
            _popupSystem.PopupEntity(Loc.GetString("cloning-pod-component-upgrade-emag-requirement"), uid);
            args.Handled = true;
        }

        public void Eject(EntityUid uid, CloningPodComponent? clonePod)
        {
            if (!Resolve(uid, ref clonePod))
                return;

            if (clonePod.BodyContainer.ContainedEntity is not { Valid: true } entity || clonePod.CloningProgress < clonePod.CloningTime)
                return;

            EntityManager.RemoveComponent<BeingClonedComponent>(entity);
            clonePod.BodyContainer.Remove(entity);
            clonePod.CloningProgress = 0f;
            clonePod.UsedBiomass = 0;
            UpdateStatus(uid, CloningPodStatus.Idle, clonePod);
            RemCompDeferred<ActiveCloningPodComponent>(uid);
        }

        private void EndFailedCloning(EntityUid uid, CloningPodComponent clonePod)
        {
            clonePod.FailedClone = false;
            clonePod.CloningProgress = 0f;
            UpdateStatus(uid, CloningPodStatus.Idle, clonePod);
            var transform = Transform(uid);
            var indices = _transformSystem.GetGridOrMapTilePosition(uid);

            var tileMix = _atmosphereSystem.GetTileMixture(transform.GridUid, null, indices, true);

            if (HasComp<EmaggedComponent>(uid))
            {
                _audio.PlayPvs(clonePod.ScreamSound, uid);
                Spawn(clonePod.MobSpawnId, transform.Coordinates);
            }

            Solution bloodSolution = new();

            var i = 0;
            while (i < 1)
            {
                tileMix?.AdjustMoles(Gas.Miasma, 6f);
                bloodSolution.AddReagent("Blood", 50);
                if (_robustRandom.Prob(0.2f))
                    i++;
            }
            _puddleSystem.TrySpillAt(uid, bloodSolution, out _);

            if (!HasComp<EmaggedComponent>(uid))
            {
                _material.SpawnMultipleFromMaterial(_robustRandom.Next(1, (int) (clonePod.UsedBiomass / 2.5)), clonePod.RequiredMaterial, Transform(uid).Coordinates);
            }

            clonePod.UsedBiomass = 0;
            RemCompDeferred<ActiveCloningPodComponent>(uid);
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            ClonesWaitingForMind.Clear();
        }
    }

    /// <summary>
    /// Raised after a new mob got spawned when cloning a humanoid
    /// </summary>
    [ByRefEvent]
    public struct CloningEvent
    {
        public bool NameHandled = false;

        public readonly EntityUid Source;
        public readonly EntityUid Target;

        public CloningEvent(EntityUid source, EntityUid target)
        {
            Source = source;
            Target = target;
        }
    }
}
