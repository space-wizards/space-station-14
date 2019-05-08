using Content.Client.Chat;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface
{
    public class LobbyGui : Control
    {
        protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Lobby/Lobby.tscn");

        public Label ServerName => GetChild<Label>("Panel/VBoxContainer/TitleContainer/ServerName");
        public Label StartTime => GetChild<Label>("Panel/VBoxContainer/HBoxContainer/LeftVBox/ReadyButtons/RoundStartText");

        public Button ReadyButton =>
            GetChild<Button>("Panel/VBoxContainer/HBoxContainer/LeftVBox/ReadyButtons/ReadyButton");

        public Button ObserveButton =>
            GetChild<Button>("Panel/VBoxContainer/HBoxContainer/LeftVBox/ReadyButtons/ObserveButton");

        public Button LeaveButton => GetChild<Button>("Panel/VBoxContainer/TitleContainer/LeaveButton");

        public ChatBox Chat { get; private set; }

        protected override void Initialize()
        {
            base.Initialize();

            var chatContainer = GetChild("Panel/VBoxContainer/HBoxContainer/LeftVBox");
            Chat = new ChatBox {ReleaseFocusOnEnter = false};
            chatContainer.AddChild(Chat);
            Chat.SizeFlagsVertical = SizeFlags.FillExpand;
        }
    }
}
