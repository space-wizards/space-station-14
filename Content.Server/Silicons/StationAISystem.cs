using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Hands.Systems;
using Content.Server.PowerCell;
using Content.Shared.UserInterface;
using Content.Shared.Access.Systems;
using Content.Shared.Alert;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Pointing;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Roles;
using Content.Shared.Silicons.AI;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Throwing;
using Content.Shared.Wires;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Silicons;

/// <inheritdoc/>
public sealed partial class StationAISystem : SharedStationAISystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IBanManager _banManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAccessSystem _access = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    [ValidatePrototypeId<EntityPrototype>]
    public const string ObserverPrototypeName = "AIObserver";



    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionStationAIComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<ActionStationAIComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StationAIComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<StationAIComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<ActionStationAIComponent, ToggleAiViewEvent>(OnToggleAiView);



    }

    private void OnComponentShutdown(EntityUid uid, ActionStationAIComponent component, ComponentShutdown args)
    {
        if (component.ViewAIActionEntity != null)
            _actions.RemoveAction(uid, component.ViewAIActionEntity);
    }

    private void OnMapInit(EntityUid uid, ActionStationAIComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.ViewAIActionEntity, component.ViewAIAction);
    }

    private void OnMindAdded(EntityUid uid, StationAIComponent component, MindAddedMessage args)
    {
        BorgActivate(uid, component);
    }

    private void OnMindRemoved(EntityUid uid, StationAIComponent component, MindRemovedMessage args)
    {
        BorgDeactivate(uid, component);
    }







    /// <summary>
    /// Activates the AI when a player occupies it
    /// </summary>
    public void BorgActivate(EntityUid uid, StationAIComponent component)
    {
        Popup.PopupEntity(Loc.GetString("borg-mind-added", ("name", Identity.Name(uid, EntityManager))), uid);
        //_access.SetAccessEnabled(uid, true);
        ///_appearance.SetData(uid, BorgVisuals.HasPlayer, true);
        Dirty(uid, component);
    }

    private void OnToggleAiView(EntityUid uid, ActionStationAIComponent component, ToggleAiViewEvent args)
    {
        //Popup.PopupEntity(Loc.GetString("borg-mind-removed", ("name", Identity.Name(uid, EntityManager))), uid); //Replace by other
        var position = Transform(uid).Coordinates;
        var observeruid = Spawn(ObserverPrototypeName, position);
        _mind.TryGetMind(uid, out var mindId, out var mind);
        _mind.TransferTo(mindId, observeruid, mind: mind);

    }

    /// <summary>
    /// Deactivates the AI when a player leaves it.
    /// </summary>
    public void BorgDeactivate(EntityUid uid, StationAIComponent component)
    {
        Popup.PopupEntity(Loc.GetString("borg-mind-removed", ("name", Identity.Name(uid, EntityManager))), uid);
        ///_access.SetAccessEnabled(uid, false);
        ///_appearance.SetData(uid, BorgVisuals.HasPlayer, false);
        Dirty(uid, component);
    }

}
