using System.Linq;
using Content.Server.Doors.Electronics;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Doors.Electronics;
using Content.Shared.Doors;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Doors.Electronics;

public sealed class DoorElectronicsSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DoorElectronicsComponent, DoorElectronicsUpdateConfigurationMessage>(OnChangeConfiguration);
        SubscribeLocalEvent<DoorElectronicsComponent, AccessReaderConfigurationChangedEvent>(OnAccessReaderChanged);
        SubscribeLocalEvent<DoorElectronicsComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
    }

    public void UpdateUserInterface(EntityUid uid, DoorElectronicsComponent component)
    {
        var accesses = new List<ProtoId<AccessLevelPrototype>>();

        if (TryComp<AccessReaderComponent>(uid, out var accessReader))
        {
            foreach (var accessList in accessReader.AccessLists)
            {
                var access = accessList.FirstOrDefault();
                accesses.Add(access);
            }
        }

        var state = new DoorElectronicsConfigurationState(accesses);
        _uiSystem.SetUiState(uid, DoorElectronicsConfigurationUiKey.Key, state);
    }

    private void OnChangeConfiguration(
        EntityUid uid,
        DoorElectronicsComponent component,
        DoorElectronicsUpdateConfigurationMessage args)
    {
        var accessReader = EnsureComp<AccessReaderComponent>(uid);
        _accessReader.SetAccesses(uid, accessReader, args.AccessList);
    }

    private void OnAccessReaderChanged(
        EntityUid uid,
        DoorElectronicsComponent component,
        AccessReaderConfigurationChangedEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnBoundUIOpened(
        EntityUid uid,
        DoorElectronicsComponent component,
        BoundUIOpenedEvent args)
    {
        UpdateUserInterface(uid, component);
    }
}
