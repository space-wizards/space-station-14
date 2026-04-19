using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.CrewManifest;
using Content.Shared.Roles;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.Components.IdCardConsoleComponent;

namespace Content.Client.Access.UI
{
    public sealed class IdCardConsoleBoundUserInterface : BoundUserInterface
    {
        private readonly SharedIdCardConsoleSystem _idCardConsoleSystem = default!;
        private IdCardConsoleWindow? _window;

        private List<ProtoId<AccessLevelPrototype>> _accessLevels = [];

        public IdCardConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            _idCardConsoleSystem = EntMan.System<SharedIdCardConsoleSystem>();
        }

        protected override void Open()
        {
            base.Open();

            if (EntMan.TryGetComponent<IdCardConsoleComponent>(Owner, out var idCard))
            {
                _accessLevels = idCard.AccessLevels;
            }
            else
            {
                _accessLevels = [];
                _idCardConsoleSystem.Log.Error($"No IdCardConsole component found for {EntMan.ToPrettyString(Owner)}!");
            }

            _window = this.CreateWindow<IdCardConsoleWindow>();

            _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

            _window.CrewManifestButton.OnPressed += _ => SendMessage(new CrewManifestOpenUiMessage());
            _window.PrivilegedIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(PrivilegedIdCardSlotId));
            _window.TargetIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(TargetIdCardSlotId));
            _window.OnSubmit += SubmitData;
        }

        public override void OnProtoReload(PrototypesReloadedEventArgs args)
        {
            base.OnProtoReload(args);
            if (!args.WasModified<AccessLevelPrototype>())
                return;

            if (EntMan.TryGetComponent<IdCardConsoleComponent>(Owner, out var idCard))
            {
                _accessLevels = idCard.AccessLevels;
                _window?.SetAccessLevels(_accessLevels);
            }

            if (State != null)
                _window?.UpdateState((IdCardConsoleBoundUserInterfaceState)State);
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            var castState = (IdCardConsoleBoundUserInterfaceState)state;
            _window?.UpdateState(castState);
        }

        private void SubmitData(string newFullName, string newJobTitle, List<ProtoId<AccessLevelPrototype>> newAccessList, ProtoId<JobPrototype>? newJobPrototype)
        {
            SendMessage(new WriteToTargetIdMessage(
                newFullName,
                newJobTitle,
                newAccessList,
                newJobPrototype));
        }
    }
}
