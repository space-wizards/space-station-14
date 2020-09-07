using Robust.Client.Console;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.UserInterface
{
    internal sealed class EscapeMenu : SS14Window
    {
        private readonly IClientConsole _console;
        private readonly ILocalizationManager _localizationManager;

        private BaseButton DisconnectButton;
        private BaseButton QuitButton;
        private BaseButton OptionsButton;
        private OptionsMenu optionsMenu;

        public EscapeMenu(IClientConsole console, ILocalizationManager localizationManager)
        {
            _localizationManager = localizationManager;
            _console = console;

            IoCManager.InjectDependencies(this);

            PerformLayout();
        }

        private void PerformLayout()
        {
            optionsMenu = new OptionsMenu();

            Resizable = false;

            Title = "Esc Menu";

            var vBox = new VBoxContainer {SeparationOverride = 4};
            Contents.AddChild(vBox);

            OptionsButton = new Button {Text = _localizationManager.GetString("Options")};
            OptionsButton.OnPressed += OnOptionsButtonClicked;
            vBox.AddChild(OptionsButton);

            DisconnectButton = new Button {Text = _localizationManager.GetString("Disconnect")};
            DisconnectButton.OnPressed += OnDisconnectButtonClicked;
            vBox.AddChild(DisconnectButton);

            QuitButton = new Button {Text = _localizationManager.GetString("Quit Game")};
            QuitButton.OnPressed += OnQuitButtonClicked;
            vBox.AddChild(QuitButton);
        }

        private void OnQuitButtonClicked(BaseButton.ButtonEventArgs args)
        {
            _console.ProcessCommand("quit");
            Dispose();
        }

        private void OnDisconnectButtonClicked(BaseButton.ButtonEventArgs args)
        {
            _console.ProcessCommand("disconnect");
            Dispose();
        }

        private void OnOptionsButtonClicked(BaseButton.ButtonEventArgs args)
        {
            optionsMenu.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                optionsMenu.Dispose();
            }
        }
    }
}
