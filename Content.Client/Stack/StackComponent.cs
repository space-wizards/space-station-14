using Content.Client.Items.Components;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Stacks;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Stack
{
    [RegisterComponent, Friend(typeof(StackSystem), typeof(StatusControl))]
    [ComponentReference(typeof(SharedStackComponent))]
    public sealed class StackComponent : SharedStackComponent, IItemStatus
    {
        [ViewVariables]
        public bool UiUpdateNeeded { get; set; }

        public Control MakeControl()
        {
            return new StatusControl(this);
        }

        private sealed class StatusControl : Control
        {
            private readonly StackComponent _parent;
            private readonly RichTextLabel _label;

            public StatusControl(StackComponent parent)
            {
                _parent = parent;
                _label = new RichTextLabel {StyleClasses = {StyleNano.StyleClassItemStatus}};
                _label.SetMarkup(Loc.GetString("comp-stack-status", ("count", _parent.Count)));
                AddChild(_label);
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
