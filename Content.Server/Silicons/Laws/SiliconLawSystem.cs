using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Radio.Components;
using Content.Server.Roles;
using Content.Server.Station.Systems;
using Content.Shared.Actions;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Stunnable;
using Content.Shared.Wires;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.Silicons.Laws;

/// <inheritdoc/>
public sealed class SiliconLawSystem : SharedSiliconLawSystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconLawBoundComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<SiliconLawBoundComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SiliconLawBoundComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<SiliconLawBoundComponent, ToggleLawsScreenEvent>(OnToggleLawsScreen);
        SubscribeLocalEvent<SiliconLawBoundComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        SubscribeLocalEvent<SiliconLawBoundComponent, PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        SubscribeLocalEvent<SiliconLawProviderComponent, GetSiliconLawsEvent>(OnDirectedGetLaws);
        SubscribeLocalEvent<SiliconLawProviderComponent, IonStormLawsEvent>(OnIonStormLaws);
        SubscribeLocalEvent<EmagSiliconLawComponent, GetSiliconLawsEvent>(OnDirectedEmagGetLaws);
        SubscribeLocalEvent<EmagSiliconLawComponent, IonStormLawsEvent>(OnEmagIonStormLaws);
        SubscribeLocalEvent<EmagSiliconLawComponent, MindAddedMessage>(OnEmagMindAdded);
        SubscribeLocalEvent<EmagSiliconLawComponent, MindRemovedMessage>(OnEmagMindRemoved);
        SubscribeLocalEvent<EmagSiliconLawComponent, ExaminedEvent>(OnExamined);
    }

    private void OnComponentShutdown(EntityUid uid, SiliconLawBoundComponent component, ComponentShutdown args)
    {
        if (component.ViewLawsActionEntity != null)
            _actions.RemoveAction(uid, component.ViewLawsActionEntity);
    }

    private void OnMapInit(EntityUid uid, SiliconLawBoundComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.ViewLawsActionEntity, component.ViewLawsAction);
        GetLaws(uid, component);
    }

    private void OnMindAdded(EntityUid uid, SiliconLawBoundComponent component, MindAddedMessage args)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var msg = Loc.GetString("laws-notify");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false,
            actor.PlayerSession.ConnectedClient, colorOverride: Color.FromHex("#2ed2fd"));
    }

    private void OnToggleLawsScreen(EntityUid uid, SiliconLawBoundComponent component, ToggleLawsScreenEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(uid, out var actor))
            return;
        args.Handled = true;

        _userInterface.TryToggleUi(uid, SiliconLawsUiKey.Key, actor.PlayerSession);
    }

    private void OnBoundUIOpened(EntityUid uid, SiliconLawBoundComponent component, BoundUIOpenedEvent args)
    {
        _entityManager.TryGetComponent<IntrinsicRadioTransmitterComponent>(uid, out var intrinsicRadio);
        HashSet<string>? radioChannels = intrinsicRadio?.Channels;

        var state = new SiliconLawBuiState(GetLaws(uid).Laws, radioChannels);
        _userInterface.TrySetUiState(args.Entity, SiliconLawsUiKey.Key, state, args.Session);
    }

    private void OnPlayerSpawnComplete(EntityUid uid, SiliconLawBoundComponent component, PlayerSpawnCompleteEvent args)
    {
        component.LastLawProvider = args.Station;
    }

    private void OnDirectedGetLaws(EntityUid uid, SiliconLawProviderComponent component, ref GetSiliconLawsEvent args)
    {
        if (args.Handled || HasComp<EmaggedComponent>(uid))
            return;

        if (component.Lawset == null)
            component.Lawset = GetLawset(component.Laws);

        args.Laws = component.Lawset;

        args.Handled = true;
    }

    private void OnIonStormLaws(EntityUid uid, SiliconLawProviderComponent component, ref IonStormLawsEvent args)
    {
        if (HasComp<EmaggedComponent>(uid))
            return;

        component.Lawset = args.Lawset;

        // gotta tell player to check their laws
        NotifyLawsChanged(uid);

        // new laws may allow antagonist behaviour so make it clear for admins
        if (TryComp<EmagSiliconLawComponent>(uid, out var emag))
            EnsureEmaggedRole(uid, emag);
    }

    private void OnDirectedEmagGetLaws(EntityUid uid, EmagSiliconLawComponent component, ref GetSiliconLawsEvent args)
    {
        if (args.Handled || !HasComp<EmaggedComponent>(uid) || component.OwnerName == null)
            return;

        if (component.Lawset == null)
        {
            // Add new emagged laws
            component.Lawset = GetLawset(component.EmagLaws);

            // Add the first emag law before the others
            component.Lawset.Laws.Insert(0, new SiliconLaw
            {
                LawString = Loc.GetString("law-emag-custom", ("name", component.OwnerName)),
                Order = 0
            });
        }

        args.Laws = component.Lawset;

        args.Handled = true;
    }

    private void OnEmagIonStormLaws(EntityUid uid, EmagSiliconLawComponent component, ref IonStormLawsEvent args)
    {
        if (!HasComp<EmaggedComponent>(uid))
            return;

        component.Lawset = args.Lawset;

        // gotta tell player to check their laws
        NotifyLawsChanged(uid);
    }

    private void OnExamined(EntityUid uid, EmagSiliconLawComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !HasComp<EmaggedComponent>(uid))
            return;

        if (component.RequireOpenPanel && TryComp<WiresPanelComponent>(uid, out var panel) && !panel.Open)
            return;

        args.PushMarkup(Loc.GetString("laws-compromised-examine"));
    }

    protected override void OnGotEmagged(EntityUid uid, EmagSiliconLawComponent component, ref GotEmaggedEvent args)
    {
        if (component.RequireOpenPanel && TryComp<WiresPanelComponent>(uid, out var panel) && !panel.Open)
            return;

        base.OnGotEmagged(uid, component, ref args);
        NotifyLawsChanged(uid);
        EnsureEmaggedRole(uid, component);

        _stunSystem.TryParalyze(uid, component.StunTime, true);
    }

    private void OnEmagMindAdded(EntityUid uid, EmagSiliconLawComponent component, MindAddedMessage args)
    {
        if (HasComp<EmaggedComponent>(uid))
            EnsureEmaggedRole(uid, component);
    }

    private void OnEmagMindRemoved(EntityUid uid, EmagSiliconLawComponent component, MindRemovedMessage args)
    {
        if (component.AntagonistRole == null)
            return;

        _roles.MindTryRemoveRole<SubvertedSiliconRoleComponent>(args.Mind);
    }

    private void EnsureEmaggedRole(EntityUid uid, EmagSiliconLawComponent component)
    {
        if (component.AntagonistRole == null || !_mind.TryGetMind(uid, out var mindId, out _))
            return;

        if (_roles.MindHasRole<SubvertedSiliconRoleComponent>(mindId))
            return;

        _roles.MindAddRole(mindId, new SubvertedSiliconRoleComponent { PrototypeId = component.AntagonistRole });
    }

    public SiliconLawset GetLaws(EntityUid uid, SiliconLawBoundComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new SiliconLawset();

        var ev = new GetSiliconLawsEvent(uid);

        RaiseLocalEvent(uid, ref ev);
        if (ev.Handled)
        {
            component.LastLawProvider = uid;
            return ev.Laws;
        }

        var xform = Transform(uid);

        if (_station.GetOwningStation(uid, xform) is { } station)
        {
            RaiseLocalEvent(station, ref ev);
            if (ev.Handled)
            {
                component.LastLawProvider = station;
                return ev.Laws;
            }
        }

        if (xform.GridUid is { } grid)
        {
            RaiseLocalEvent(grid, ref ev);
            if (ev.Handled)
            {
                component.LastLawProvider = grid;
                return ev.Laws;
            }
        }

        if (component.LastLawProvider == null ||
            Deleted(component.LastLawProvider) ||
            Terminating(component.LastLawProvider.Value))
        {
            component.LastLawProvider = null;
        }
        else
        {
            RaiseLocalEvent(component.LastLawProvider.Value, ref ev);
            if (ev.Handled)
            {
                return ev.Laws;
            }
        }

        RaiseLocalEvent(ref ev);
        return ev.Laws;
    }

    public void NotifyLawsChanged(EntityUid uid)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var msg = Loc.GetString("laws-update-notify");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, actor.PlayerSession.ConnectedClient, colorOverride: Color.Red);
    }

    /// <summary>
    /// Extract all the laws from a lawset's prototype ids.
    /// </summary>
    public SiliconLawset GetLawset(string lawset)
    {
        var proto = _prototype.Index<SiliconLawsetPrototype>(lawset);
        var laws = new SiliconLawset()
        {
            Laws = new List<SiliconLaw>(proto.Laws.Count)
        };
        foreach (var law in proto.Laws)
        {
            laws.Laws.Add(_prototype.Index<SiliconLawPrototype>(law));
        }

        return laws;
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class LawsCommand : ToolshedCommand
{
    private SiliconLawSystem? _law;

    [CommandImplementation("list")]
    public IEnumerable<EntityUid> List()
    {
        var query = EntityManager.EntityQueryEnumerator<SiliconLawBoundComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            yield return uid;
        }
    }

    [CommandImplementation("get")]
    public IEnumerable<string> Get([PipedArgument] EntityUid lawbound)
    {
        _law ??= GetSys<SiliconLawSystem>();

        foreach (var law in _law.GetLaws(lawbound).Laws)
        {
            yield return $"law {law.LawIdentifierOverride ?? law.Order.ToString()}: {Loc.GetString(law.LawString)}";
        }
    }
}
