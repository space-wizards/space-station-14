using Content.Shared.Ame.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Ame.UI
{
    [UsedImplicitly]
    public sealed class AmeControllerBoundUserInterface : BoundUserInterface
    {
        private AmeWindow? _window;

        public AmeControllerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<AmeWindow>();
            _window.OnAmeButton += ButtonPressed;
        }

        /// <summary>
        /// Update the ui each time new state data is sent from the server.
        /// </summary>
        /// <param name="state">
        /// Data of the <see cref="SharedReagentDispenserComponent"/> that this ui represents.
        /// Sent from the server.
        /// </param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (AmeControllerBoundUserInterfaceState) state;
            _window?.UpdateState(castState); //Update window state
        }

        public void ButtonPressed(UiButton button)
        {
            SendMessage(new UiButtonPressedMessage(button));
        }
    }
}
