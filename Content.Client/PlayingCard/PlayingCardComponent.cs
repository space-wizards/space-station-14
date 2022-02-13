using Content.Client.Items.Components;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.PlayingCard;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.PlayingCard
{
    [RegisterComponent, Friend(typeof(PlayingCardSystem), typeof(StatusControl))]
    [ComponentReference(typeof(SharedPlayingCardComponent))]
    public class PlayingCardComponent : SharedPlayingCardComponent, IItemStatus
    {
        [ViewVariables]
        public bool UiUpdateNeeded { get; set; }

        public Control MakeControl()
        {
            return new StatusControl(this);
        }

        private sealed class StatusControl : Control
        {
            private readonly PlayingCardComponent _parent;
            private readonly RichTextLabel _label;

            public StatusControl(PlayingCardComponent parent)
            {
                _parent = parent;
                _label = new RichTextLabel {StyleClasses = {StyleNano.StyleClassItemStatus}};
                AddChild(_label);

                parent.UiUpdateNeeded = true;
            }

            protected override void FrameUpdate(FrameEventArgs args)
            {
                base.FrameUpdate(args);

                if (!_parent.UiUpdateNeeded)
                {
                    return;
                }

                _parent.UiUpdateNeeded = false;

                _label.SetMarkup(Loc.GetString("comp-stack-status", ("count", _parent.Count)));
            }
        }
    }
}
