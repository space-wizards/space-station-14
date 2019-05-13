using Content.Client.Chat;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface;
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

        public LobbyGui(ILocalizationManager localization)
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = new Color(37, 37, 45)};

            var vBox = new VBoxContainer();
            AddChild(vBox);

            {
                // Title bar.
                var titleContainer = new HBoxContainer();
                vBox.AddChild(titleContainer);

                titleContainer.AddChild(new Label
                {
                    Text = localization.GetString("Lobby"),
                    SizeFlagsHorizontal = SizeFlags.None
                });

                titleContainer.AddChild(ServerName = new Label
                {
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter | SizeFlags.Expand
                });

                titleContainer.AddChild(LeaveButton = new Button
                {
                    SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                    Text = localization.GetString("Leave")
                });
            }

            var hBox = new HBoxContainer {SizeFlagsVertical = SizeFlags.FillExpand};
            vBox.AddChild(hBox);

            {
                var leftVBox = new VBoxContainer {SizeFlagsHorizontal = SizeFlags.FillExpand};
                hBox.AddChild(leftVBox);

                // Placeholder.
                leftVBox.AddChild(new Control {SizeFlagsVertical = SizeFlags.FillExpand});

                var readyButtons = new HBoxContainer();

                leftVBox.AddChild(readyButtons);
                readyButtons.AddChild(ObserveButton = new Button
                {
                    Text = localization.GetString("Observe")
                });

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

                leftVBox.AddChild(Chat = new ChatBox {SizeFlagsVertical = SizeFlags.FillExpand});
            }

            // Placeholder.
            hBox.AddChild(new Control {SizeFlagsHorizontal = SizeFlags.FillExpand});
        }
    }
}
