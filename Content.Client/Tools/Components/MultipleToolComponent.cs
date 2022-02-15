using Content.Client.Items.Components;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Tools.Components;
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
    public class MultipleToolComponent : SharedMultipleToolComponent, IItemStatus
    {
        private string? _behavior;
        [DataField("statusShowBehavior")]
        private bool _statusShowBehavior = true;

        [ViewVariables(VVAccess.ReadWrite)] private bool _uiUpdateNeeded;
        [ViewVariables] public bool StatusShowBehavior => _statusShowBehavior;
        [ViewVariables] public string? Behavior => _behavior;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not MultipleToolComponentState tool) return;

            _behavior = tool.QualityName;
            _uiUpdateNeeded = true;

        }

        public Control MakeControl() => new StatusControl(this);

        private sealed class StatusControl : Control
        {
            private readonly MultipleToolComponent _parent;
            private readonly RichTextLabel _label;

            public StatusControl(MultipleToolComponent parent)
            {
                _parent = parent;
                _label = new RichTextLabel {StyleClasses = {StyleNano.StyleClassItemStatus}};
                AddChild(_label);

                UpdateDraw();
            }

            protected override void FrameUpdate(FrameEventArgs args)
            {
                base.FrameUpdate(args);

                if (!_parent._uiUpdateNeeded)
                {
                    return;
                }
                Update();
            }

            public void Update()
            {
                _parent._uiUpdateNeeded = false;

                _label.SetMarkup(_parent.StatusShowBehavior ? _parent.Behavior ?? string.Empty : string.Empty);
            }
        }
    }
}
