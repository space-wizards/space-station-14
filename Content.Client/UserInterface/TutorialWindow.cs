using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface
{
    public sealed class TutorialWindow : SS14Window
    {
        private readonly int _headerFontSize = 14;
        private VBoxContainer VBox { get; }

        private const string IntroContents = @"Hi and welcome to Space Station 14! This tutorial will assume that you know a bit about how SS13 plays. It's mostly intended to lay out the controls and their differences from SS13.
";

        private const string QuickControlsContents = @"Movement: [color=#a4885c]WASD[/color]
Switch hands: [color=#a4885c]X[/color]
Use held item: [color=#a4885c]Z[/color]
Drop held item: [color=#a4885c]Q[/color]
Open inventory: [color=#a4885c]I[/color]
Open character window: [color=#a4885c]C[/color]
Open crafting window: [color=#a4885c]G[/color]
Focus chat: [color=#a4885c]T[/color]
Use targeted entity: [color=#a4885c]E[/color]
Throw held item: [color=#a4885c]Ctrl + left click[/color]
Examine entity: [color=#a4885c]Shift + left click[/color]
Open entity context menu: [color=#a4885c]Right click[/color]
Toggle combat mode: [color=#a4885c]R[/color]
Toggle console: [color=#a4885c]~ (Tilde)[/color]
Toggle UI: [color=#a4885c]F1[/color]
Toggle debug overlay: [color=#a4885c]F3[/color]
Toggle entity spawner: [color=#a4885c]F5[/color]
Toggle tile spawner: [color=#a4885c]F6[/color]
Toggle sandbox window: [color=#a4885c]B[/color]

If you are not on a QWERTY keyboard, the keys mentioned above are bound to the physical location on your keyboard, not what letter they correspond to. For example on AZERTY movement is ZQSD, drop is A, W is activate in hand.
";

        private const string GameplayContents = @"Some notes on gameplay. To talk in OOC, prefix your chat message with \[ or /ooc. Death is currently show as a black circle around the player. You can respawn via the respawn button in the sandbox menu. Instead of intents, we have ""combat mode"". Check controls above for its keybind. You can't attack anybody with it off, so no more hitting yourself with your own crowbar.
";
        private const string FeedbackContents = @"If you have any feedback, questions, bug reports, etc..., do not be afraid to tell us! You can ask on Discord or heck, just write it in OOC! We'll catch it.";

        protected override Vector2? CustomSize => (520, 580);

        public TutorialWindow()
        {
            Title = "The Tutorial!";

            //Get section header font
            var cache = IoCManager.Resolve<IResourceCache>();
            Font headerFont = new VectorFont(cache.GetResource<FontResource>("/Nano/NotoSans/NotoSans-Regular.ttf"), _headerFontSize);

            var scrollContainer = new ScrollContainer();
            scrollContainer.AddChild(VBox = new VBoxContainer());
            Contents.AddChild(scrollContainer);

            //Intro
            VBox.AddChild(new Label{FontOverride = headerFont, Text = "Intro"});
            AddFormattedText(IntroContents);

            //Controls
            VBox.AddChild(new Label{FontOverride = headerFont, Text = "Controls"});
            AddFormattedText(QuickControlsContents);

            //Gameplay
            VBox.AddChild(new Label { FontOverride = headerFont, Text = "Gameplay" });
            AddFormattedText(GameplayContents);

            //Feedback
            VBox.AddChild(new Label { FontOverride = headerFont, Text = "Feedback" });
            AddFormattedText(FeedbackContents);
        }

        private void AddFormattedText(string text)
        {
            if(VBox == null)
                return;

            var introLabel = new RichTextLabel();
            var introMessage = new FormattedMessage();
            introMessage.AddMarkup(text);
            introLabel.SetMessage(introMessage);
            VBox.AddChild(introLabel);
        }
    }
}
