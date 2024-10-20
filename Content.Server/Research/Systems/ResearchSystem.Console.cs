using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared.UserInterface;
using Content.Shared.Access.Components;
using Content.Shared.Emag.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private void InitializeConsole()
    {
        SubscribeLocalEvent<ResearchConsoleComponent, ConsoleUnlockTechnologyMessage>(OnConsoleUnlock);
        SubscribeLocalEvent<ResearchConsoleComponent, ConsoleRediscoverTechnologyMessage>(OnRediscoverTechnology);
        SubscribeLocalEvent<ResearchConsoleComponent, BeforeActivatableUIOpenEvent>(OnConsoleBeforeUiOpened);
        SubscribeLocalEvent<ResearchConsoleComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<ResearchConsoleComponent, ResearchRegistrationChangedEvent>(OnConsoleRegistrationChanged);
        SubscribeLocalEvent<ResearchConsoleComponent, TechnologyDatabaseModifiedEvent>(OnConsoleDatabaseModified);
    }

    private void OnRediscoverTechnology(
        EntityUid uid,
        ResearchConsoleComponent console,
        ConsoleRediscoverTechnologyMessage args
    )
    {
        var act = args.Actor;

        if (!this.IsPowered(uid, EntityManager))
            return;

        if (TryComp<AccessReaderComponent>(uid, out var access) && !_accessReader.IsAllowed(act, uid, access))
        {
            _popup.PopupEntity(Loc.GetString("research-console-no-access-popup"), act);
            return;
        }

        if (!TryGetClientServer(uid, out var serverEnt, out var serverComponent))
        {
            return;
        }

        if (serverComponent.RediscoverCost > serverComponent.Points)
        {
            return;
        }

        ModifyServerPoints(serverEnt.Value, -serverComponent.RediscoverCost);
        UpdateTechnologyCards(serverEnt.Value);
        SyncClientWithServer(uid);
        UpdateConsoleInterface(uid);
    }

    private void OnConsoleUnlock(EntityUid uid, ResearchConsoleComponent component, ConsoleUnlockTechnologyMessage args)
    {
        var act = args.Actor;

        if (!this.IsPowered(uid, EntityManager))
            return;

        if (!PrototypeManager.TryIndex<TechnologyPrototype>(args.Id, out var technologyPrototype))
            return;

        if (TryComp<AccessReaderComponent>(uid, out var access) && !_accessReader.IsAllowed(act, uid, access))
        {
            _popup.PopupEntity(Loc.GetString("research-console-no-access-popup"), act);
            return;
        }

        if (!UnlockTechnology(uid, args.Id, act))
            return;

        if (!HasComp<EmaggedComponent>(uid))
        {
            var getIdentityEvent = new TryGetIdentityShortInfoEvent(uid, act);
            RaiseLocalEvent(getIdentityEvent);

            var message = Loc.GetString(
                "research-console-unlock-technology-radio-broadcast",
                ("technology", Loc.GetString(technologyPrototype.Name)),
                ("amount", technologyPrototype.Cost),
                ("approver", getIdentityEvent.Title ?? string.Empty)
            );
            _radio.SendRadioMessage(uid, message, component.AnnouncementChannel, uid, escapeMarkup: false);
        }
       
        SyncClientWithServer(uid);
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleBeforeUiOpened(EntityUid uid, ResearchConsoleComponent component, BeforeActivatableUIOpenEvent args)
    {
        SyncClientWithServer(uid);
    }

    private void UpdateConsoleInterface(EntityUid uid, ResearchConsoleComponent? component = null, ResearchClientComponent? clientComponent = null)
    {
        if (!Resolve(uid, ref component, ref clientComponent, false))
            return;

        ResearchConsoleBoundInterfaceState state;

        if (TryGetClientServer(uid, out _, out var serverComponent, clientComponent))
        {
            var points = clientComponent.ConnectedToServer ? serverComponent.Points : 0;
            state = new ResearchConsoleBoundInterfaceState(points, serverComponent.RediscoverCost);
        }
        else
        {
            state = new ResearchConsoleBoundInterfaceState(default, default);
        }

        _uiSystem.SetUiState(uid, ResearchConsoleUiKey.Key, state);
    }

    private void OnPointsChanged(EntityUid uid, ResearchConsoleComponent component, ref ResearchServerPointsChangedEvent args)
    {
        if (!_uiSystem.IsUiOpen(uid, ResearchConsoleUiKey.Key))
            return;
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleRegistrationChanged(EntityUid uid, ResearchConsoleComponent component, ref ResearchRegistrationChangedEvent args)
    {
        SyncClientWithServer(uid);
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleDatabaseModified(EntityUid uid, ResearchConsoleComponent component, ref TechnologyDatabaseModifiedEvent args)
    {
        UpdateConsoleInterface(uid, component);
    }

}
