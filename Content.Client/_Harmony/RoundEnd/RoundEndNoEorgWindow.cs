using System.Numerics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._Harmony.RoundEnd
{
    public sealed class RoundEndNoEorgWindow : DefaultWindow
    {
        private static readonly Vector2 WindowSize = new Vector2(520, 420);
        private const string TitleText = "harmony-round-end-no-eorg-window-title";
        private const string LabelText = "harmony-round-end-no-eorg-window-label";
        private const string MessageText = "harmony-round-end-no-eorg-window-message";
        private const string RuleText = "harmony-round-end-no-eorg-window-rule";
        private const string RuleDetailText = "harmony-round-end-no-eorg-window-rule-text";
        private const string CloseButtonText = "harmony-round-end-no-eorg-window-close-button";
        private const string CloseButtonWaitText = "harmony-round-end-no-eorg-window-close-button-wait";
        private const string CheckboxText = "harmony-round-end-no-eorg-window-checkbox-text";
        public Button TimedCloseButton;
        public CheckBox CheckBox;

        public RoundEndNoEorgWindow()
        {
            MinSize = SetSize = WindowSize;
            Title = Loc.GetString(TitleText);
            CloseButton.Visible = false;
            CloseButton.Disabled = true;
            TimedCloseButton = CreateButton();
            CheckBox = CreateCheckBox();

            var container = CreateContainer();

            var noEorgLabel = CreateLabel();
            var noEorgMessage = CreateMessage();
            var noEorgRule = CreateRule();

            container.AddChild(noEorgLabel);
            container.AddChild(noEorgMessage);
            container.AddChild(noEorgRule);
            container.AddChild(CheckBox);
            container.AddChild(TimedCloseButton);
            Contents.AddChild(container);
        }

        private static BoxContainer CreateContainer()
        {
            return new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
            };
        }

        private static Label CreateLabel()
        {
            return new Label
            {
                Text = Loc.GetString(LabelText),
                StyleClasses = { "LabelBig" },
                FontColorOverride = Color.Red,
                Align = Label.AlignMode.Center
            };
        }

        private static RichTextLabel CreateMessage()
        {
            var message = new FormattedMessage();
            message.AddMarkupOrThrow(Loc.GetString(MessageText) + "\n");

            var richTextLabel = new RichTextLabel();
            richTextLabel.SetMessage(message);
            return richTextLabel;
        }

        private static RichTextLabel CreateRule()
        {
            var rule = new FormattedMessage();
            rule.PushColor(Color.Gray);
            rule.AddMarkupOrThrow(Loc.GetString(RuleText));
            rule.AddText("\n...\n" + Loc.GetString(RuleDetailText) + "\n...\n");

            var richTextLabel = new RichTextLabel();
            richTextLabel.SetMessage(rule);
            return richTextLabel;
        }

        private static CheckBox CreateCheckBox()
        {
            return new CheckBox
            {
                Text = Loc.GetString(CheckboxText)
            };
        }

        private Button CreateButton() => new Button { Disabled = true };

        public void UpdateCloseButton(float timer)
        {
            if (timer > 0.0f)
            {
                TimedCloseButton.Text = Loc.GetString(CloseButtonWaitText, ("time", MathF.Floor(timer)));
                TimedCloseButton.Disabled = true;
            }
            else
            {
                TimedCloseButton.Text = Loc.GetString(CloseButtonText);
                TimedCloseButton.Disabled = false;
            }
        }
    }
}
