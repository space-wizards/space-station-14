using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Doors;
using Content.Shared.Doors.Electronics;
using Content.Shared.Interaction;
using Content.Server.Doors.Electronics;
using Robust.Server.GameObjects;

namespace Content.Server.Doors.Electronics;

public sealed class DoorElectronicsSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DoorElectronicsComponent, DoorElectronicsUpdateConfigurationMessage>(OnChangeConfiguration);
        SubscribeLocalEvent<DoorElectronicsComponent, DoorElectronicsRefreshUiMessage>(OnRefreshUi);
        SubscribeLocalEvent<DoorElectronicsComponent, InteractUsingEvent>(OnInteractUsing);
    }

    public void UpdateUserInterface(EntityUid uid, DoorElectronicsComponent component)
    {
        List<string> accesses = new List<string>();

        if (TryComp<AccessReaderComponent>(component.Owner, out var accessReader))
        {
            foreach (var accessList in accessReader.AccessLists)
            {
                var access = accessList.FirstOrDefault();
                if (access == null)
                    continue;
                accesses.Add(access);
            }
        }

        var state = new DoorElectronicsConfigurationState(accesses);

        _uiSystem.TrySetUiState(uid, DoorElectronicsConfigurationUiKey.Key, state);
    }

    private void OnChangeConfiguration(EntityUid uid,
                                       DoorElectronicsComponent component,
                                       DoorElectronicsUpdateConfigurationMessage args)
    {
        if (TryComp<AccessReaderComponent>(uid, out var accessReader))
        {
            accessReader.AccessLists.Clear();
            foreach (var access in args.accessList)
            {
                accessReader.AccessLists.Add(new HashSet<string>(){access});
            }
        }
        var state = new DoorElectronicsConfigurationState(args.accessList);
        _uiSystem.TrySetUiState(component.Owner,
                                DoorElectronicsConfigurationUiKey.Key,
                                state);
    }

    private void OnRefreshUi(EntityUid uid,
                             DoorElectronicsComponent component,
                             DoorElectronicsRefreshUiMessage args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnInteractUsing(EntityUid uid, DoorElectronicsComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<NetworkConfiguratorComponent>(args.Used, out var networkConfigurator))
            return;

        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        args.Handled = true;

        _uiSystem.TryOpen(uid, DoorElectronicsConfigurationUiKey.Key, actor.PlayerSession);
    }
}
