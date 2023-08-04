using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.CrewManifest;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.Components.IdCardConsoleComponent;
namespace Content.Client.Access.UI
{
    public sealed class IdCardConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public IdCardConsoleBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }
        private IdCardConsoleWindow? _window;

        protected override void Open()
        {
            base.Open();
            List<string> accessLevels;

            if (_entityManager.TryGetComponent<IdCardConsoleComponent>(Owner.Owner, out var idCard))
            {
                accessLevels = idCard.AccessLevels;
                accessLevels.Sort();
            }
            else
            {
                accessLevels = new List<string>();
                Logger.ErrorS(SharedIdCardConsoleSystem.Sawmill, $"No IdCardConsole component found for {_entityManager.ToPrettyString(Owner.Owner)}!");
            }

            _window = new IdCardConsoleWindow(this, _prototypeManager, accessLevels) {Title = _entityManager.GetComponent<MetaDataComponent>(Owner.Owner).EntityName};

            _window.CrewManifestButton.OnPressed += _ => SendMessage(new CrewManifestOpenUiMessage());
            _window.PrivilegedIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(PrivilegedIdCardSlotId));
            _window.TargetIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(TargetIdCardSlotId));

            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            var castState = (IdCardConsoleBoundUserInterfaceState) state;
            _window?.UpdateState(castState);
        }

        public void SubmitData(string newFullName, string newJobTitle, List<string> newAccessList, string newJobPrototype)
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
