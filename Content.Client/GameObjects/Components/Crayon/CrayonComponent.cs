using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Crayon
{
    [RegisterComponent]
    public class CrayonComponent : SharedCrayonComponent, IItemStatus
    {
        [ViewVariables(VVAccess.ReadWrite)] private bool _uiUpdateNeeded;
        [ViewVariables(VVAccess.ReadWrite)] private string Color => _color;
        [ViewVariables] private int Charges { get; set; }
        [ViewVariables] private int Capacity { get; set; }

        Control IItemStatus.MakeControl()
        {
            return new StatusControl(this);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (curState is not CrayonComponentState state)
                return;

            _color = state.Color;
            SelectedState = state.State;
            Charges = state.Charges;
            Capacity = state.Capacity;

            _uiUpdateNeeded = true;
        }

        private sealed class StatusControl : Control
        {
            private readonly CrayonComponent _parent;
            private readonly RichTextLabel _label;

            public StatusControl(CrayonComponent parent)
            {
                _parent = parent;
                _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
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
                _label.SetMarkup(Loc.GetString("Drawing: [color={0}]{1}[/color] ({2}/{3})",
                    _parent.Color,
                    _parent.SelectedState,
                    _parent.Charges,
                    _parent.Capacity));
            }
        }
    }
}
