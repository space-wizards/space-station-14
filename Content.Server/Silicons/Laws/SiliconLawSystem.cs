using Content.Server.Station.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Silicons.Laws.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Silicons.Laws;

/// <summary>
/// This handles getting and displaying the laws for silicons.
/// </summary>
public sealed class SiliconLawSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SiliconLawBoundComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SiliconLawBoundComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<SiliconLawBoundComponent, ToggleLawsScreenEvent>(OnToggleLawsScreen);
        SubscribeLocalEvent<SiliconLawBoundComponent, BoundUIOpenedEvent>(OnBoundUIOpened);

        SubscribeLocalEvent<SiliconLawProviderComponent, GetSiliconLawsEvent>(OnDirectedGetLaws);
        SubscribeLocalEvent<EmagSiliconLawProviderComponent, GetSiliconLawsEvent>(OnDirectedEmagGetLaws);
        SubscribeLocalEvent<EmagSiliconLawProviderComponent, GotEmaggedEvent>(OnGotEmagged);
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
            args.Laws.Add(law);
        }

        args.Handled = true;
    }

    private void OnDirectedEmagGetLaws(EntityUid uid, EmagSiliconLawProviderComponent component, ref GetSiliconLawsEvent args)
    {
        if (args.Handled || !HasComp<EmaggedComponent>(uid) || component.Laws.Count == 0)
            return;

        foreach (var law in component.Laws)
        {
            args.Laws.Add(law);
        }

        args.Handled = true;
    }

    private void OnGotEmagged(EntityUid uid, EmagSiliconLawProviderComponent component, ref GotEmaggedEvent args)
    {
        args.Handled = true;
    }

    public List<string> GetLaws(EntityUid uid)
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
}
