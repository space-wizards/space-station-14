using System;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Interactable
{
    [RegisterComponent]
    public class ToolComponent : SharedToolComponent, IItemStatus
    {
        private Tool _behavior;
        private bool _statusShowBehavior;

        [ViewVariables(VVAccess.ReadWrite)] private bool _uiUpdateNeeded;
        [ViewVariables] public bool StatusShowBehavior => _statusShowBehavior;
        [ViewVariables] public override Tool Behavior => _behavior;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _statusShowBehavior, "statusShowBehavior", false);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (!(curState is ToolComponentState tool)) return;

            _behavior = tool.Behavior;
            _uiUpdateNeeded = true;

        }

        public Control MakeControl() => new StatusControl(this);

        private sealed class StatusControl : Control
        {
            private readonly ToolComponent _parent;
            private readonly RichTextLabel _label;

            public StatusControl(ToolComponent parent)
            {
                _parent = parent;
                _label = new RichTextLabel {StyleClasses = {StyleNano.StyleClassItemStatus}};
                AddChild(_label);

                parent._uiUpdateNeeded = true;
            }

            protected override void Update(FrameEventArgs args)
            {
                base.Update(args);

                if (!_parent._uiUpdateNeeded)
                {
                    return;
                }

                _parent._uiUpdateNeeded = false;

                if(!_parent.StatusShowBehavior)
                    _label.SetMarkup(string.Empty);
                else
                    _label.SetMarkup(_parent.Behavior.ToString());

            }
        }
    }
}
