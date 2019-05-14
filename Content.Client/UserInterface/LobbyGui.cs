using Content.Client.Chat;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    internal sealed class LobbyGui : PanelContainer
    {
        public Label ServerName { get; }
        public Label StartTime { get; }
        public Button ReadyButton { get; }
        public Button ObserveButton { get; }
        public Button LeaveButton { get; }
        public ChatBox Chat { get; set; }

        public LobbyGui(ILocalizationManager localization, IResourceCache resourceCache)
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = new Color(37, 37, 45)};
            PanelOverride.SetContentMarginOverride(StyleBox.Margin.All, 4);

            var vBox = new VBoxContainer();
            AddChild(vBox);

            {
                // Title bar.
                var titleContainer = new HBoxContainer();
                vBox.AddChild(titleContainer);

                var lobbyTitle = new Label
                {
                    Text = localization.GetString("Lobby"),
                    SizeFlagsHorizontal = SizeFlags.None
                };
                lobbyTitle.AddStyleClass(NanoStyle.StyleClassLabelHeading);
                titleContainer.AddChild(lobbyTitle);

                titleContainer.AddChild(ServerName = new Label
                {
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter | SizeFlags.Expand
                });
                ServerName.AddStyleClass(NanoStyle.StyleClassLabelHeading);

                titleContainer.AddChild(LeaveButton = new Button
                {
                    SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                    Text = localization.GetString("Leave")
                });
                LeaveButton.AddStyleClass(NanoStyle.StyleClassButtonBig);
            }

            var hBox = new HBoxContainer {SizeFlagsVertical = SizeFlags.FillExpand};
            vBox.AddChild(hBox);

            {
                var leftVBox = new VBoxContainer {SizeFlagsHorizontal = SizeFlags.FillExpand};
                hBox.AddChild(leftVBox);

                leftVBox.AddChild(new Placeholder(resourceCache)
                {
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    PlaceholderText = localization.GetString("Character UI\nPlaceholder")
                });

                var readyButtons = new HBoxContainer();

                leftVBox.AddChild(readyButtons);
                readyButtons.AddChild(ObserveButton = new Button
                {
                    Text = localization.GetString("Observe")
                });
                ObserveButton.AddStyleClass(NanoStyle.StyleClassButtonBig);

                readyButtons.AddChild(StartTime = new Label
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    Align = Label.AlignMode.Right
                });

                readyButtons.AddChild(ReadyButton = new Button
                {
                    ToggleMode = true,
                    Text = localization.GetString("Ready Up")
                });
                ReadyButton.AddStyleClass(NanoStyle.StyleClassButtonBig);

                leftVBox.AddChild(Chat = new ChatBox {SizeFlagsVertical = SizeFlags.FillExpand});
                Chat.Input.PlaceHolder = localization.GetString("Talk!");
            }

            // Placeholder.
            hBox.AddChild(new Placeholder(resourceCache)
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                PlaceholderText = localization.GetString("Server Info\nPlaceholder")
            });
        }
    }
}
