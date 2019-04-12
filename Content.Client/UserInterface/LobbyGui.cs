using Content.Client.Chat;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.Utility;

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
            Chat = new ChatBox();
            chatContainer.AddChild(Chat);
            Chat.SizeFlagsVertical = SizeFlags.FillExpand;
        }
    }
}
