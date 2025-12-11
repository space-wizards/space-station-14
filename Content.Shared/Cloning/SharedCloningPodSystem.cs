using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Damage.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Materials;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Random.Helpers;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Cloning;

/// <summary>
/// Base system for managing shared logic of cloning pods,
/// including mind tracking, status updates, event handling, and pod-console linkage.
/// </summary>
public abstract partial class SharedCloningPodSystem : EntitySystem
{
    [Dependency] private readonly CloningConsoleSystem _cloningConsole = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = null!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly SharedCloningSystem _cloning = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _material = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// Tracks which minds are waiting to be transferred into a clone.
    public readonly Dictionary<MindComponent, EntityUid> ClonesWaitingForMind = [];
    private readonly ProtoId<CloningSettingsPrototype> _settingsId = "CloningPod";

    private const float EasyModeCloningCost = 0.7f;

    /// <inheritdoc/>
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

    /// <summary>
    /// Tries to start the cloning process for the specified mind and pod.
    /// </summary>
    /// <param name="ent">The cloning pod entity.</param>
    /// <param name="bodyToClone">The body to clone.</param>
    /// <param name="mindEnt">The mind entity.</param>
    /// <param name="failChanceModifier">The chance modifier for the cloning process.</param>
    /// <returns>True if the cloning process was started, false otherwise.</returns>
    public bool TryCloning(Entity<CloningPodComponent?> ent, EntityUid bodyToClone, Entity<MindComponent> mindEnt, float failChanceModifier = 1)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (HasComp<ActiveCloningPodComponent>(ent.Owner))
            return false;

        var mind = mindEnt.Comp;
        if (ClonesWaitingForMind.TryGetValue(mind, out var clone))
        {
            if (Exists(clone) &&
                !_mobState.IsDead(clone) &&
                TryComp<MindContainerComponent>(clone, out var cloneMindComp) &&
                (cloneMindComp.Mind == null || cloneMindComp.Mind == mindEnt))
                return false; // Mind already has clone.

            ClonesWaitingForMind.Remove(mind);
        }

        if (mind.OwnedEntity != null && !_mobState.IsDead(mind.OwnedEntity.Value))
            return false; // Body controlled by mind is not dead.

        // Yes, we still need to track down the client because we need to open the Eui.
        if (mind.UserId == null || !_playerManager.TryGetSessionById(mind.UserId.Value, out var client))
            return false; // If we can't track down the client, we can't offer transfer. That'd be quite bad.

        if (!TryComp<PhysicsComponent>(bodyToClone, out var physics))
            return false;

        var cloningCost = (int)Math.Round(physics.FixturesMass);

        if (_configManager.GetCVar(CCVars.BiomassEasyMode))
            cloningCost = (int)Math.Round(cloningCost * EasyModeCloningCost);

        // biomass checks.
        var biomassAmount = _material.GetMaterialAmount(ent.Owner, ent.Comp.RequiredMaterial);

        if (biomassAmount < cloningCost)
        {
            if (ent.Comp.ConnectedConsole != null)
                _chat.TrySendInGameICMessage(ent.Comp.ConnectedConsole.Value, Loc.GetString("cloning-console-chat-error", ("units", cloningCost)), InGameICChatType.Speak, false);
            return false;
        }
        // end of biomass checks.

        // genetic damage checks.
        if (TryComp<DamageableComponent>(bodyToClone, out var damageable) &&
            damageable.Damage.DamageDict.TryGetValue("Cellular", out var cellularDmg))
        {
            var chance = Math.Clamp((float)(cellularDmg / 100), 0, 1);
            chance *= failChanceModifier;

            if (cellularDmg > 0 && ent.Comp.ConnectedConsole != null)
                _chat.TrySendInGameICMessage(ent.Comp.ConnectedConsole.Value, Loc.GetString("cloning-console-cellular-warning", ("percent", Math.Round(100 - chance * 100))), InGameICChatType.Speak, false);

            // TODO: Replace with RandomPredicted once the engine PR is merged
            var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
            var rand = new System.Random(seed);
            if (rand.Prob(chance))
            {
                ent.Comp.FailedClone = true;
                UpdateStatus((ent.Owner, ent.Comp), CloningPodStatus.Gore);
                AddComp<ActiveCloningPodComponent>(ent.Owner);
                _material.TryChangeMaterialAmount(ent.Owner, ent.Comp.RequiredMaterial, -cloningCost);
                ent.Comp.UsedBiomass = cloningCost;
                Dirty(ent);
                return true;
            }
        }
        // end of genetic damage checks.

        if (!_cloning.TryCloning(bodyToClone, _transform.GetMapCoordinates(bodyToClone), _settingsId, out var mob)) // spawn a new body
        {
            if (ent.Comp.ConnectedConsole != null)
                _chat.TrySendInGameICMessage(ent.Comp.ConnectedConsole.Value, Loc.GetString("cloning-console-uncloneable-trait-error"), InGameICChatType.Speak, false);
            return false;
        }

        var cloneMindReturn = AddComp<BeingClonedComponent>(mob.Value);
        cloneMindReturn.Mind = mind;
        cloneMindReturn.Parent = ent.Owner;

        _container.Insert(mob.Value, ent.Comp.BodyContainer);
        ClonesWaitingForMind.Add(mind, mob.Value);

        _material.TryChangeMaterialAmount(ent.Owner, ent.Comp.RequiredMaterial, -cloningCost);
        ent.Comp.UsedBiomass = cloningCost;

        AddComp<ActiveCloningPodComponent>(ent.Owner);
        OpenEui(mindEnt, mind, client);
        UpdateStatus((ent.Owner, ent.Comp), CloningPodStatus.NoMind);
        Dirty(ent);

        return true;
    }

    /// <summary>
    /// Updates the visual and logic status of the cloning pod.
    /// </summary>
    public void UpdateStatus(Entity<CloningPodComponent> ent, CloningPodStatus status)
    {
        ent.Comp.Status = status;
        _appearance.SetData(ent.Owner, CloningPodVisuals.Status, ent.Comp.Status);
    }

    private void OnComponentInit(Entity<CloningPodComponent> ent, ref ComponentInit args)
    {
        ent.Comp.BodyContainer = _container.EnsureContainer<ContainerSlot>(ent.Owner, "clonepod-bodyContainer");
        _deviceLink.EnsureSinkPorts(ent.Owner, ent.Comp.PodPort);
    }

    /// <summary>
    /// Transfers a mind into its cloned body once the clone is ready.
    /// </summary>
    public void TransferMindToClone(EntityUid mindId, MindComponent mind)
    {
        if (!ClonesWaitingForMind.TryGetValue(mind, out var entity) ||
            !Exists(entity) ||
            !TryComp<MindContainerComponent>(entity, out var mindComp) ||
            mindComp.Mind != null)
            return;

        _mind.TransferTo(mindId, entity, ghostCheckOverride: true, mind: mind);
        _mind.UnVisit(mindId, mind);
        ClonesWaitingForMind.Remove(mind);
    }

    private void HandleMindAdded(Entity<BeingClonedComponent> ent, ref MindAddedMessage message)
    {
        if (ent.Comp.Parent == EntityUid.Invalid ||
            !Exists(ent.Comp.Parent) ||
            !TryComp<CloningPodComponent>(ent.Comp.Parent, out var cloningPodComponent) ||
            ent.Owner != cloningPodComponent.BodyContainer.ContainedEntity)
        {
            RemComp<BeingClonedComponent>(ent.Owner);
            return;
        }

        UpdateStatus((ent.Comp.Parent, cloningPodComponent), CloningPodStatus.Cloning);
    }

    private void OnPortDisconnected(Entity<CloningPodComponent> ent, ref PortDisconnectedEvent args)
    {
        ent.Comp.ConnectedConsole = null;
        Dirty(ent);
    }

    private void OnAnchor(Entity<CloningPodComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (ent.Comp.ConnectedConsole == null || !TryComp<CloningConsoleComponent>(ent.Comp.ConnectedConsole, out var console))
            return;

        if (args.Anchored)
        {
            _cloningConsole.RecheckConnections(ent.Comp.ConnectedConsole.Value, ent.Owner, console.GeneticScanner);
            return;
        }

        _cloningConsole.UpdateUserInterface((ent.Comp.ConnectedConsole.Value, console));
    }

    private void OnExamined(Entity<CloningPodComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !_powerReceiver.IsPowered(ent.Owner))
            return;

        args.PushMarkup(Loc.GetString("cloning-pod-biomass", ("number", _material.GetMaterialAmount(ent.Owner, ent.Comp.RequiredMaterial))));
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

        if (!_powerReceiver.IsPowered(ent.Owner))
            return;

        _popup.PopupPredicted(Loc.GetString("cloning-pod-component-upgrade-emag-requirement"), ent.Owner, args.UserUid);
        args.Handled = true;
    }

    /// <summary>
    /// Clears mind transfer records on round reset.
    /// </summary>
    public void Reset(RoundRestartCleanupEvent ev)
    {
        ClonesWaitingForMind.Clear();
    }

    // TODO: SharedEuiManager so that we can just directly open the eui from shared.
    protected virtual void OpenEui(Entity<MindComponent> mindEnt, MindComponent mind, ICommonSession client) { }
}
