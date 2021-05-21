using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStackComponent))]
    public class StackComponent : SharedStackComponent, IItemStatus
    {
        [ViewVariables(VVAccess.ReadWrite)]
        private bool _uiUpdateNeeded;

        public Control MakeControl()
        {
            return new StatusControl(this);
        }

        public void DirtyUI()
        {
            _uiUpdateNeeded = true;
        }

        private sealed class StatusControl : Control
        {
            private readonly StackComponent _parent;
            private readonly RichTextLabel _label;

            public StatusControl(StackComponent parent)
            {
                _parent = parent;
                _label = new RichTextLabel {StyleClasses = {StyleNano.StyleClassItemStatus}};
                AddChild(_label);

                parent._uiUpdateNeeded = true;
            }

            protected override void FrameUpdate(FrameEventArgs args)
            {
                base.FrameUpdate(args);

                if (!_parent._uiUpdateNeeded)
                {
                    return;
                }

                _parent._uiUpdateNeeded = false;

                _label.SetMarkup(Loc.GetString("comp-stack-status", ("count", _parent.Count)));
            }
        }
    }
}
