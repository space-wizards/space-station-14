using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.CrewManifest;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.Components.IdCardConsoleComponent;

namespace Content.Client.Access.UI
{
    public sealed class IdCardConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        private readonly SharedIdCardConsoleSystem _idCardConsoleSystem = default!;

        private IdCardConsoleWindow? _window;

        public IdCardConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            _idCardConsoleSystem = EntMan.System<SharedIdCardConsoleSystem>();
        }

        protected override void Open()
        {
            base.Open();
            List<ProtoId<AccessLevelPrototype>> accessLevels;

            if (EntMan.TryGetComponent<IdCardConsoleComponent>(Owner, out var idCard))
            {
                accessLevels = idCard.AccessLevels;
            }
            else
            {
                accessLevels = new List<ProtoId<AccessLevelPrototype>>();
                _idCardConsoleSystem.Log.Error($"No IdCardConsole component found for {EntMan.ToPrettyString(Owner)}!");
            }

            _window = new IdCardConsoleWindow(this, _prototypeManager, accessLevels)
            {
                Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName
            };

            _window.CrewManifestButton.OnPressed += _ => SendMessage(new CrewManifestOpenUiMessage());
            _window.PrivilegedIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(PrivilegedIdCardSlotId));
            _window.TargetIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(TargetIdCardSlotId));

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
            var castState = (IdCardConsoleBoundUserInterfaceState) state;
            _window?.UpdateState(castState);
        }

        public void SubmitData(string newFullName, string newJobTitle, List<ProtoId<AccessLevelPrototype>> newAccessList, string newJobPrototype)
        {
            if (newFullName.Length > MaxFullNameLength)
                newFullName = newFullName[..MaxFullNameLength];

            if (newJobTitle.Length > MaxJobTitleLength)
                newJobTitle = newJobTitle[..MaxJobTitleLength];

            SendMessage(new WriteToTargetIdMessage(
                newFullName,
                newJobTitle,
                newAccessList,
                newJobPrototype));
        }
    }
}
