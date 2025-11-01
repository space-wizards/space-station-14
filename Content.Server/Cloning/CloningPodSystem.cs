using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Cloning.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.EUI;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Materials;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.Components;
using Content.Shared.Cloning;
using Content.Shared.Chat;
using Content.Shared.Damage.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.Containers;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Cloning;

public sealed class CloningPodSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = null!;
    [Dependency] private readonly EuiManager _euiManager = null!;
    [Dependency] private readonly CloningConsoleSystem _cloningConsoleSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly MaterialStorageSystem _material = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly CloningSystem _cloning = default!;
    [Dependency] private readonly EmagSystem _emag = default!;

    public readonly Dictionary<MindComponent, EntityUid> ClonesWaitingForMind = new();
    public readonly ProtoId<CloningSettingsPrototype> SettingsId = "CloningPod";
    public const float EasyModeCloningCost = 0.7f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        SubscribeLocalEvent<BeingClonedComponent, MindAddedMessage>(HandleMindAdded);
        SubscribeLocalEvent<CloningPodComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CloningPodComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<CloningPodComponent, AnchorStateChangedEvent>(OnAnchor);
        SubscribeLocalEvent<CloningPodComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CloningPodComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnComponentInit(Entity<CloningPodComponent> ent, ref ComponentInit args)
    {
        ent.Comp.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(ent.Owner, "clonepod-bodyContainer");
        _signalSystem.EnsureSinkPorts(ent.Owner, ent.Comp.PodPort);
    }

    internal void TransferMindToClone(EntityUid mindId, MindComponent mind)
    {
        if (!ClonesWaitingForMind.TryGetValue(mind, out var entity) ||
            !Exists(entity) ||
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
            !Exists(clonedComponent.Parent) ||
            !TryComp<CloningPodComponent>(clonedComponent.Parent, out var cloningPodComponent) ||
            uid != cloningPodComponent.BodyContainer.ContainedEntity)
        {
            RemComp<BeingClonedComponent>(uid);
            return;
        }
        UpdateStatus(clonedComponent.Parent, CloningPodStatus.Cloning, cloningPodComponent);
    }
    private void OnPortDisconnected(Entity<CloningPodComponent> ent, ref PortDisconnectedEvent args)
    {
        ent.Comp.ConnectedConsole = null;
    }

    private void OnAnchor(Entity<CloningPodComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (ent.Comp.ConnectedConsole == null || !TryComp<CloningConsoleComponent>(ent.Comp.ConnectedConsole, out var console))
            return;

        if (args.Anchored)
        {
            _cloningConsoleSystem.RecheckConnections(ent.Comp.ConnectedConsole.Value, ent.Owner, console.GeneticScanner, console);
            return;
        }
        _cloningConsoleSystem.UpdateUserInterface(ent.Comp.ConnectedConsole.Value, console);
    }

    private void OnExamined(Entity<CloningPodComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !_powerReceiverSystem.IsPowered(ent.Owner))
            return;

        args.PushMarkup(Loc.GetString("cloning-pod-biomass", ("number", _material.GetMaterialAmount(ent.Owner, ent.Comp.RequiredMaterial))));
    }

    public bool TryCloning(EntityUid uid, EntityUid bodyToClone, Entity<MindComponent> mindEnt, CloningPodComponent? clonePod, float failChanceModifier = 1)
    {
        if (!Resolve(uid, ref clonePod))
            return false;

        if (HasComp<ActiveCloningPodComponent>(uid))
            return false;

        var mind = mindEnt.Comp;
        if (ClonesWaitingForMind.TryGetValue(mind, out var clone))
        {
            if (Exists(clone) &&
                !_mobStateSystem.IsDead(clone) &&
                TryComp<MindContainerComponent>(clone, out var cloneMindComp) &&
                (cloneMindComp.Mind == null || cloneMindComp.Mind == mindEnt))
                return false; // Mind already has clone

            ClonesWaitingForMind.Remove(mind);
        }

        if (mind.OwnedEntity != null && !_mobStateSystem.IsDead(mind.OwnedEntity.Value))
            return false; // Body controlled by mind is not dead

        // Yes, we still need to track down the client because we need to open the Eui
        if (mind.UserId == null || !_playerManager.TryGetSessionById(mind.UserId.Value, out var client))
            return false; // If we can't track down the client, we can't offer transfer. That'd be quite bad.

        if (!TryComp<PhysicsComponent>(bodyToClone, out var physics))
            return false;

        var cloningCost = (int)Math.Round(physics.FixturesMass);

        if (_configManager.GetCVar(CCVars.BiomassEasyMode))
            cloningCost = (int)Math.Round(cloningCost * EasyModeCloningCost);

        // biomass checks
        var biomassAmount = _material.GetMaterialAmount(uid, clonePod.RequiredMaterial);

        if (biomassAmount < cloningCost)
        {
            if (clonePod.ConnectedConsole != null)
                _chatSystem.TrySendInGameICMessage(clonePod.ConnectedConsole.Value, Loc.GetString("cloning-console-chat-error", ("units", cloningCost)), InGameICChatType.Speak, false);
            return false;
        }

        // end of biomass checks

        // genetic damage checks
        if (TryComp<DamageableComponent>(bodyToClone, out var damageable) &&
            damageable.Damage.DamageDict.TryGetValue("Cellular", out var cellularDmg))
        {
            var chance = Math.Clamp((float)(cellularDmg / 100), 0, 1);
            chance *= failChanceModifier;

            if (cellularDmg > 0 && clonePod.ConnectedConsole != null)
                _chatSystem.TrySendInGameICMessage(clonePod.ConnectedConsole.Value, Loc.GetString("cloning-console-cellular-warning", ("percent", Math.Round(100 - chance * 100))), InGameICChatType.Speak, false);

            if (_robustRandom.Prob(chance))
            {
                clonePod.FailedClone = true;
                UpdateStatus(uid, CloningPodStatus.Gore, clonePod);
                AddComp<ActiveCloningPodComponent>(uid);
                _material.TryChangeMaterialAmount(uid, clonePod.RequiredMaterial, -cloningCost);
                clonePod.UsedBiomass = cloningCost;
                return true;
            }
        }
        // end of genetic damage checks

        if (!_cloning.TryCloning(bodyToClone, _transformSystem.GetMapCoordinates(bodyToClone), SettingsId, out var mob)) // spawn a new body
        {
            if (clonePod.ConnectedConsole != null)
                _chatSystem.TrySendInGameICMessage(clonePod.ConnectedConsole.Value, Loc.GetString("cloning-console-uncloneable-trait-error"), InGameICChatType.Speak, false);
            return false;
        }

        var cloneMindReturn = AddComp<BeingClonedComponent>(mob.Value);
        cloneMindReturn.Mind = mind;
        cloneMindReturn.Parent = uid;
        _containerSystem.Insert(mob.Value, clonePod.BodyContainer);
        ClonesWaitingForMind.Add(mind, mob.Value);
        _euiManager.OpenEui(new AcceptCloningEui(mindEnt, mind, this), client);

        UpdateStatus(uid, CloningPodStatus.NoMind, clonePod);
        AddComp<ActiveCloningPodComponent>(uid);
        _material.TryChangeMaterialAmount(uid, clonePod.RequiredMaterial, -cloningCost);
        clonePod.UsedBiomass = cloningCost;
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
    private void OnEmagged(Entity<CloningPodComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent.Owner, EmagType.Interaction))
            return;

        if (!this.IsPowered(ent.Owner, EntityManager))
            return;

        _popupSystem.PopupEntity(Loc.GetString("cloning-pod-component-upgrade-emag-requirement"), ent.Owner);
        args.Handled = true;
    }

    public void Eject(EntityUid uid, CloningPodComponent? clonePod)
    {
        if (!Resolve(uid, ref clonePod))
            return;

        if (clonePod.BodyContainer.ContainedEntity is not { Valid: true } entity || clonePod.CloningProgress < clonePod.CloningTime)
            return;

        RemComp<BeingClonedComponent>(entity);
        _containerSystem.Remove(entity, clonePod.BodyContainer);
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
        var indices = _transformSystem.GetGridTilePositionOrDefault((uid, transform));
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
            tileMix?.AdjustMoles(Gas.Ammonia, 6f);
            bloodSolution.AddReagent("Blood", 50);
            if (_robustRandom.Prob(0.2f))
                i++;
        }
        _puddleSystem.TrySpillAt(uid, bloodSolution, out _);

        if (!HasComp<EmaggedComponent>(uid))
        {
            _material.SpawnMultipleFromMaterial(_robustRandom.Next(1, (int)(clonePod.UsedBiomass / 2.5)), clonePod.RequiredMaterial, Transform(uid).Coordinates);
        }

        clonePod.UsedBiomass = 0;
        RemCompDeferred<ActiveCloningPodComponent>(uid);
    }

    public void Reset(RoundRestartCleanupEvent ev)
    {
        ClonesWaitingForMind.Clear();
    }
}
