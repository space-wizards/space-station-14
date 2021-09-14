using Content.Client.Items.Components;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Tool;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.Tools.Components
{
    [RegisterComponent]
    [NetworkedComponent()]
    public class MultiToolComponent : Component, IItemStatus
    {
        private ToolQuality _behavior;
        [DataField("statusShowBehavior")]
        private bool _statusShowBehavior = true;

        [ViewVariables(VVAccess.ReadWrite)] private bool _uiUpdateNeeded;
        [ViewVariables] public bool StatusShowBehavior => _statusShowBehavior;
        [ViewVariables] public ToolQuality? Behavior => _behavior;

        public override string Name => "MultiTool";

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not MultiToolComponentState tool) return;

            _behavior = tool.Quality;
            _uiUpdateNeeded = true;

        }

        public Control MakeControl() => new StatusControl(this);

        private sealed class StatusControl : Control
        {
            private readonly MultiToolComponent _parent;
            private readonly RichTextLabel _label;

            public StatusControl(MultiToolComponent parent)
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

                _label.SetMarkup(_parent.StatusShowBehavior ? _parent.Behavior.ToString() ?? string.Empty : string.Empty);
            }
        }
    }
}
