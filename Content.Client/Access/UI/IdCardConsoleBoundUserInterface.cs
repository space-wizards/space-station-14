using System.Collections.Generic;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.Components.SharedIdCardConsoleComponent;

namespace Content.Client.Access.UI
{
    public sealed class IdCardConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public IdCardConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private IdCardConsoleWindow? _window;

        protected override void Open()
        {
            base.Open();

            _window = new IdCardConsoleWindow(this, _prototypeManager) {Title = _entityManager.GetComponent<MetaDataComponent>(Owner.Owner).EntityName};
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

        public void ButtonPressed(UiButton button)
        {
            SendMessage(new IdButtonPressedMessage(button));
        }

        public void SubmitData(string newFullName, string newJobTitle, List<string> newAccessList)
        {
            if (newFullName.Length > MaxFullNameLength)
                newFullName = newFullName[..MaxFullNameLength];

            if (newJobTitle.Length > MaxJobTitleLength)
                newJobTitle = newJobTitle[..MaxJobTitleLength];

            SendMessage(new WriteToTargetIdMessage(
                newFullName,
                newJobTitle,
                newAccessList));
        }
    }
}
