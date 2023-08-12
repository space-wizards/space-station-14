using Content.Server.Chat.Managers;
using Content.Server.Mind.Components;
using Content.Server.Station.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Chat;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Silicons.Laws;

/// <inheritdoc/>
public sealed class SiliconLawSystem : SharedSiliconLawSystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconLawBoundComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SiliconLawBoundComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<SiliconLawBoundComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<SiliconLawBoundComponent, ToggleLawsScreenEvent>(OnToggleLawsScreen);
        SubscribeLocalEvent<SiliconLawBoundComponent, BoundUIOpenedEvent>(OnBoundUIOpened);

        SubscribeLocalEvent<SiliconLawProviderComponent, GetSiliconLawsEvent>(OnDirectedGetLaws);
        SubscribeLocalEvent<EmagSiliconLawComponent, GetSiliconLawsEvent>(OnDirectedEmagGetLaws);
        SubscribeLocalEvent<EmagSiliconLawComponent, ExaminedEvent>(OnExamined);
    }

    private void OnComponentStartup(EntityUid uid, SiliconLawBoundComponent component, ComponentStartup args)
    {
        component.ProvidedAction = new (_prototype.Index<InstantActionPrototype>(component.ViewLawsAction));
        _actions.AddAction(uid, component.ProvidedAction, null);
    }

    private void OnComponentShutdown(EntityUid uid, SiliconLawBoundComponent component, ComponentShutdown args)
    {
        if (component.ProvidedAction != null)
            _actions.RemoveAction(uid, component.ProvidedAction);
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
        var state = new SiliconLawBuiState(GetLaws(uid));
        _userInterface.TrySetUiState(args.Entity, SiliconLawsUiKey.Key, state, (IPlayerSession) args.Session);
    }

    private void OnDirectedGetLaws(EntityUid uid, SiliconLawProviderComponent component, ref GetSiliconLawsEvent args)
    {
        if (args.Handled || HasComp<EmaggedComponent>(uid) || component.Laws.Count == 0)
            return;

        foreach (var law in component.Laws)
        {
            args.Laws.Add(_prototype.Index<SiliconLawPrototype>(law));
        }

        args.Handled = true;
    }

    private void OnDirectedEmagGetLaws(EntityUid uid, EmagSiliconLawComponent component, ref GetSiliconLawsEvent args)
    {
        if (args.Handled || !HasComp<EmaggedComponent>(uid) || component.OwnerName == null)
            return;

        args.Laws.Add(new SiliconLaw
        {
            LawString = Loc.GetString("law-emag-custom", ("name", component.OwnerName)),
            Order = 0
        });
    }

    private void OnExamined(EntityUid uid, EmagSiliconLawComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !HasComp<EmaggedComponent>(uid))
            return;

        args.PushMarkup(Loc.GetString("laws-compromised-examine"));
    }

    protected override void OnGotEmagged(EntityUid uid, EmagSiliconLawComponent component, ref GotEmaggedEvent args)
    {
        base.OnGotEmagged(uid, component, ref args);
        NotifyLawsChanged(uid);
    }

    public List<SiliconLaw> GetLaws(EntityUid uid)
    {
        var xform = Transform(uid);

        var ev = new GetSiliconLawsEvent(uid);

        RaiseLocalEvent(uid, ref ev);
        if (ev.Handled)
            return ev.Laws;

        if (_station.GetOwningStation(uid, xform) is { } station)
        {
            RaiseLocalEvent(station, ref ev);
            if (ev.Handled)
                return ev.Laws;
        }

        if (xform.GridUid is { } grid)
        {
            RaiseLocalEvent(grid, ref ev);
            if (ev.Handled)
                return ev.Laws;
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
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, actor.PlayerSession.ConnectedClient, colorOverride: Color.FromHex("#2ed2fd"));
    }
}
