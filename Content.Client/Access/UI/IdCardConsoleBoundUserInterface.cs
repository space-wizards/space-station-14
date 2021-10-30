using System.Collections.Generic;
using Content.Shared.Access;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.SharedIdCardConsoleComponent;

namespace Content.Client.Access.UI
{
    public class IdCardConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public IdCardConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private IdCardConsoleWindow? _window;

        protected override void Open()
        {
            base.Open();

            _window = new IdCardConsoleWindow(this, _prototypeManager) {Title = Owner.Owner.Name};
            _window.OnClose += Close;
            _window.OpenCentered();
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
