using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Doors;
using Content.Shared.Doors.Electronics;
using Content.Server.Doors.Electronics;
using Robust.Server.GameObjects;

namespace Content.Server.Doors.Electronics
{
    public sealed class DoorElectronicsSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DoorElectronicsComponent, SharedDoorElectronicsComponent.UpdateConfigurationMessage>(OnChangeConfiguration);
            SubscribeLocalEvent<DoorElectronicsComponent, SharedDoorElectronicsComponent.RefreshUiMessage>(OnRefreshUi);
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

            var state = new SharedDoorElectronicsComponent.ConfigurationState(accesses);

            _uiSystem.TrySetUiState(uid, DoorElectronicsConfigurationUiKey.Key, state);
        }

        private void OnChangeConfiguration(EntityUid uid,
                                           DoorElectronicsComponent component,
                                           SharedDoorElectronicsComponent.UpdateConfigurationMessage args)
        {
            if (TryComp<AccessReaderComponent>(uid, out var accessReader))
            {
                accessReader.AccessLists.Clear();
                foreach (var access in args.accessList)
                {
                    accessReader.AccessLists.Add(new HashSet<string>(){access});
                }
            }
            var state = new SharedDoorElectronicsComponent.ConfigurationState(args.accessList);
            _uiSystem.TrySetUiState(component.Owner,
                                    DoorElectronicsConfigurationUiKey.Key,
                                    state);
        }

        private void OnRefreshUi(EntityUid uid,
                                 DoorElectronicsComponent component,
                                 SharedDoorElectronicsComponent.RefreshUiMessage args)
        {
            UpdateUserInterface(uid, component);
        }
    }
}
