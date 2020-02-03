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

        private const string ControlsContents = @"Just like in any game, WASD is movement. If that does not work, the server probably broke. Clicking on things ""interacts"" in some object-defined sense with it, with your active hand. X switches hands. Z uses the item in your hand. Q drops items. T focuses chat. C opens your inventory.

New to SS14: You can press ""E"" to activate objects. This functions similarly to clicking with an empty hand most of the time: opens interfaces, etc. The difference is that it works even without an empty hand. No longer do you need to drop your tools to use a computer!

You can talk in OOC by prefixing the message with \[ or /ooc.

Instead of intents, we have ""combat mode"". Hit ""R"" to toggle it on or off. You can't attack anybody with it off, so no more hitting yourself with your own crowbar.

You can toggle the sandbox window with ""B"", the entity spawner with ""F5"", and the tile spawner with ""F6"".

Currently death is a black screen with a circle around your player. You can use the sandbox panel to respawn if you die.

If you are not on a QWERTY keyboard, the keys mentioned above are bound to the physical location on your keyboard, not what letter they correspond to. For example on AZERTY movement is ZQSD, drop is A, W is activate in hand.
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
            AddFormattedText(ControlsContents);

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
