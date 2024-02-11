using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.Components.AccessOverriderComponent;

namespace Content.Client.Access.UI
{
    public sealed class AccessOverriderBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        private readonly SharedAccessOverriderSystem _accessOverriderSystem = default!;

        private AccessOverriderWindow? _window;

        public AccessOverriderBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            _accessOverriderSystem = EntMan.System<SharedAccessOverriderSystem>();
        }

        protected override void Open()
        {
            base.Open();

            List<ProtoId<AccessLevelPrototype>> accessLevels;

            if (EntMan.TryGetComponent<AccessOverriderComponent>(Owner, out var accessOverrider))
            {
                accessLevels = accessOverrider.AccessLevels;
                accessLevels.Sort();
            }

            else
            {
                accessLevels = new List<ProtoId<AccessLevelPrototype>>();
                _accessOverriderSystem.Log.Error($"No AccessOverrider component found for {EntMan.ToPrettyString(Owner)}!");
            }

            _window = new AccessOverriderWindow(this, _prototypeManager, accessLevels)
            {
                Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName
            };

            _window.PrivilegedIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(PrivilegedIdCardSlotId));

            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _window?.Dispose();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            var castState = (AccessOverriderBoundUserInterfaceState) state;
            _window?.UpdateState(castState);
        }

        public void SubmitData(List<string> newAccessList)
        {
            SendMessage(new WriteToTargetAccessReaderIdMessage(newAccessList));
        }
    }
}
