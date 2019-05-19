using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface
{
    internal sealed class TutorialButton : Button
    {
        private const string TutorialContents = @"Hi and welcome to Space Station 14!

This tutorial will assume that you know a bit about how SS13 plays.
It's mostly intended to lay out the controls and their differences from SS13.

Just like in any game, WASD is movement. If that does not work, the server probably broke.

Clicking on things ""interacts"" in some object-defined sense with it, with your active hand.

X switches hands. Z uses the item in your hand. Q drops items. T focuses chat. C opens your inventory.

New to SS14: You can press ""E"" to activate objects. This functions similarly to clicking with an empty hand most of the time: opens interfaces, etc. The difference is that it works even without an empty hand. No longer do you need to drop your tools to use a computer!

You can talk in OOC by prefixing the message with [ or /ooc.

If you are not on a QWERTY keyboard, the keys mentioned above are bound to the physical location on your keyboard,
not what letter they correspond to. For example on AZERTY movement is ZQSD, drop is A, W is activate in hand.

If you have any feedback, questions, bug reports, etc..., do not be afraid to tell us!
You can ask on Discord or heck, just write it in OOC, we'll catch it.
";


        public TutorialButton()
        {
            OnPressed += OnOnPressed;

            Text = "Tutorial";
        }

        private void OnOnPressed(ButtonEventArgs obj)
        {
            _openTutorialWindow();
        }

        private void _openTutorialWindow()
        {
            var window = new SS14Window {Title = "Tutorial"};

            var scrollContainer = new ScrollContainer();
            window.Contents.AddChild(scrollContainer);

            var label = new RichTextLabel();
            scrollContainer.AddChild(label);

            var message = new FormattedMessage();
            message.AddText(TutorialContents);
            label.SetMessage(message);

            window.AddToScreen();
        }
    }
}
