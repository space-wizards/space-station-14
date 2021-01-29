using Content.Client.GameObjects.EntitySystems;
using Robust.Client.Console;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.UserInterface
{
    internal sealed class EscapeMenu : SS14Window
    {
        [Dependency] private readonly IEntitySystemManager _systemManager = default!;

        private readonly IClientConsole _console;

        private BaseButton DisconnectButton;
        private BaseButton QuitButton;
        private BaseButton OptionsButton;
        private BaseButton AHelpButton;
        private OptionsMenu optionsMenu;

        public EscapeMenu(IClientConsole console)
        {
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

            OptionsButton = new Button {Text = Loc.GetString("Options")};
            OptionsButton.OnPressed += OnOptionsButtonClicked;
            vBox.AddChild(OptionsButton);

            AHelpButton = new Button {Text = Loc.GetString("AHelp")};
            AHelpButton.OnPressed += OnAHelpButtonClicked;
            vBox.AddChild(AHelpButton);

            DisconnectButton = new Button {Text = Loc.GetString("Disconnect")};
            DisconnectButton.OnPressed += OnDisconnectButtonClicked;
            vBox.AddChild(DisconnectButton);

            QuitButton = new Button {Text = Loc.GetString("Quit Game")};
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

        private void OnAHelpButtonClicked(BaseButton.ButtonEventArgs args)
        {
            _systemManager.GetEntitySystem<BwoinkSystem>().EnsureWindowForLocalPlayer();
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
