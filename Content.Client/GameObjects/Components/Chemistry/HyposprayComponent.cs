using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    public sealed class HyposprayComponent : SharedHyposprayComponent, IItemStatus
    {
        [ViewVariables] private ReagentUnit CurrentVolume { get; set; }
        [ViewVariables] private ReagentUnit TotalVolume { get; set; }
        [ViewVariables(VVAccess.ReadWrite)] private bool _uiUpdateNeeded;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not HyposprayComponentState cState)
                return;

            CurrentVolume = cState.CurVolume;
            TotalVolume = cState.MaxVolume;
            _uiUpdateNeeded = true;
        }

        Control IItemStatus.MakeControl()
        {
            return new StatusControl(this);
        }

        private sealed class StatusControl : Control
        {
            private readonly HyposprayComponent _parent;
            private readonly RichTextLabel _label;

            public StatusControl(HyposprayComponent parent)
            {
                _parent = parent;
                _label = new RichTextLabel {StyleClasses = {StyleNano.StyleClassItemStatus}};
                AddChild(_label);

                parent._uiUpdateNeeded = true;
            }

            /// <inheritdoc />
            protected override void FrameUpdate(FrameEventArgs args)
            {
                base.FrameUpdate(args);
                if (!_parent._uiUpdateNeeded)
                {
                    return;
                }

                _parent._uiUpdateNeeded = false;

                _label.SetMarkup(Loc.GetString(
                    "Volume: [color=white]{0}/{1}[/color]",
                    _parent.CurrentVolume, _parent.TotalVolume));
            }
        }
    }
}
