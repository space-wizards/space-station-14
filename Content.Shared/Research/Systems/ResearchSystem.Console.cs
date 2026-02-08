using Content.Shared.Access.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.UserInterface;

namespace Content.Shared.Research.Systems;

public partial class ResearchSystem
{
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedRadioSystem _radio = default!;

    private void InitializeConsole()
    {
        SubscribeLocalEvent<ResearchConsoleComponent, ConsoleUnlockTechnologyMessage>(OnConsoleUnlock);
        SubscribeLocalEvent<ResearchConsoleComponent, BeforeActivatableUIOpenEvent>(OnConsoleBeforeUiOpened);
        SubscribeLocalEvent<ResearchConsoleComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<ResearchConsoleComponent, ResearchRegistrationChangedEvent>(OnConsoleRegistrationChanged);
        SubscribeLocalEvent<ResearchConsoleComponent, TechnologyDatabaseModifiedEvent>(OnConsoleDatabaseModified);
        SubscribeLocalEvent<ResearchConsoleComponent, TechnologyDatabaseSynchronizedEvent>(OnConsoleDatabaseSynchronized);
        SubscribeLocalEvent<ResearchConsoleComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnConsoleUnlock(Entity<ResearchConsoleComponent> ent, ref ConsoleUnlockTechnologyMessage args)
    {
        var act = args.Actor;

        if (!_power.IsPowered(ent.Owner))
            return;

        if (!_proto.TryIndex<TechnologyPrototype>(args.Id, out var technologyPrototype))
            return;

        if (TryComp<AccessReaderComponent>(ent, out var access) && !_accessReader.IsAllowed(act, ent, access))
        {
            _popup.PopupClient(Loc.GetString("research-console-no-access-popup"), act);
            return;
        }

        if (!UnlockTechnology(ent.Owner, args.Id, act))
            return;

        if (!_emag.CheckFlag(ent, EmagType.Interaction))
        {
            var getIdentityEvent = new TryGetIdentityShortInfoEvent(ent, act);
            RaiseLocalEvent(getIdentityEvent);

            var message = Loc.GetString(
                "research-console-unlock-technology-radio-broadcast",
                ("technology", Loc.GetString(technologyPrototype.Name)),
                ("amount", technologyPrototype.Cost),
                ("approver", getIdentityEvent.Title ?? string.Empty)
            );
            _radio.SendRadioMessage(ent, message, ent.Comp.AnnouncementChannel, ent, escapeMarkup: false);
        }

        SyncClientWithServer(ent.Owner);
        UpdateConsoleInterface(ent.Owner);
    }

    private void OnConsoleBeforeUiOpened(Entity<ResearchConsoleComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        SyncClientWithServer(ent.Owner);
    }

    private void UpdateConsoleInterface(Entity<ResearchConsoleComponent?, ResearchClientComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return;

        var clientComponent = ent.Comp2;
        ResearchConsoleBoundInterfaceState state;

        if (TryGetClientServer((ent, clientComponent), out var server))
        {
            var points = clientComponent.ConnectedToServer ? server.Value.Comp.Points : 0;
            state = new ResearchConsoleBoundInterfaceState(points);
        }
        else
        {
            state = new ResearchConsoleBoundInterfaceState(default);
        }

        _uiSystem.SetUiState(ent.Owner, ResearchConsoleUiKey.Key, state);
    }

    private void OnPointsChanged(Entity<ResearchConsoleComponent> ent, ref ResearchServerPointsChangedEvent args)
    {
        if (!_uiSystem.IsUiOpen(ent.Owner, ResearchConsoleUiKey.Key))
            return;
        UpdateConsoleInterface(ent.Owner);
    }

    private void OnConsoleRegistrationChanged(Entity<ResearchConsoleComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        SyncClientWithServer(ent.Owner);
        UpdateConsoleInterface(ent.Owner);
    }

    private void OnConsoleDatabaseModified(Entity<ResearchConsoleComponent> ent, ref TechnologyDatabaseModifiedEvent args)
    {
        SyncClientWithServer(ent.Owner);
        UpdateConsoleInterface(ent.Owner);
    }

    private void OnConsoleDatabaseSynchronized(Entity<ResearchConsoleComponent> ent, ref TechnologyDatabaseSynchronizedEvent args)
    {
        UpdateConsoleInterface(ent.Owner);
    }

    private void OnEmagged(Entity<ResearchConsoleComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }
}
