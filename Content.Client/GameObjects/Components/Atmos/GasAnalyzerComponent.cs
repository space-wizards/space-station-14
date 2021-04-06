using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Atmos
{
    [RegisterComponent]
    internal class GasAnalyzerComponent : SharedGasAnalyzerComponent, IItemStatus
    {
        [ViewVariables(VVAccess.ReadWrite)] private bool _uiUpdateNeeded;
        [ViewVariables] private GasAnalyzerDanger Danger { get; set; }

        Control IItemStatus.MakeControl()
        {
            return new StatusControl(this);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not GasAnalyzerComponentState state)
                return;

            Danger = state.Danger;
            _uiUpdateNeeded = true;
        }

        private sealed class StatusControl : Control
        {
            private readonly GasAnalyzerComponent _parent;
            private readonly RichTextLabel _label;

            public StatusControl(GasAnalyzerComponent parent)
            {
                _parent = parent;
                _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
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

                var color = _parent.Danger switch
                {
                    GasAnalyzerDanger.Warning => "orange",
                    GasAnalyzerDanger.Hazard => "red",
                    _ => "green",
                };

                _label.SetMarkup(Loc.GetString("itemstatus-pressure-warn", ("color", color), ("danger", _parent.Danger)));
            }
        }
    }
}
