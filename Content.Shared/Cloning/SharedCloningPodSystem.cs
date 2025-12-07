using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Materials;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Shared.Cloning;

/// <summary>
/// Base system for managing shared logic of cloning pods,
/// including mind tracking, status updates, event handling, and pod-console linkage.
/// </summary>
public abstract partial class SharedCloningPodSystem : EntitySystem
{
    [Dependency] private readonly CloningConsoleSystem _cloningConsole = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _material = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;

    /// Tracks which minds are waiting to be transferred into a clone.
    public readonly Dictionary<MindComponent, EntityUid> ClonesWaitingForMind = [];

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
    /// Tries to start the cloning process for the specified mind and pod. Override in server.
    /// </summary>
    public virtual bool TryCloning(EntityUid uid, EntityUid bodyToClone, Entity<MindComponent> mindEnt, CloningPodComponent? clonePod, float failChanceModifier = 1)
    {
        return false;
    }

    /// <summary>
    /// Updates the visual and logic status of the cloning pod.
    /// </summary>
    public void UpdateStatus(EntityUid podUid, CloningPodStatus status, CloningPodComponent cloningPod)
    {
        cloningPod.Status = status;
        _appearance.SetData(podUid, CloningPodVisuals.Status, cloningPod.Status);
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
        Dirty(ent);
    }

    private void OnAnchor(Entity<CloningPodComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (ent.Comp.ConnectedConsole == null || !TryComp<CloningConsoleComponent>(ent.Comp.ConnectedConsole, out var console))
            return;

        if (args.Anchored)
        {
            _cloningConsole.RecheckConnections(ent.Comp.ConnectedConsole.Value, ent.Owner, console.GeneticScanner, console);
            return;
        }

        _cloningConsole.UpdateUserInterface(ent.Comp.ConnectedConsole.Value, console);
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
}
