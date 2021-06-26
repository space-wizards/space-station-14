using System;
using Content.Client.Items.Components;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Tool;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.Tools.Components
{
    [RegisterComponent]
    [NetworkedComponent()]

    public class WelderComponent : SharedToolComponent, IItemStatus
    {
        public override string Name => "Welder";

        private ToolQuality _behavior;
        [ViewVariables(VVAccess.ReadWrite)] private bool _uiUpdateNeeded;
        [ViewVariables] public float FuelCapacity { get; private set; }
        [ViewVariables] public float Fuel { get; private set; }
        [ViewVariables] public bool Activated { get; private set; }
        [ViewVariables] public override ToolQuality Qualities => _behavior;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not WelderComponentState weld)
                return;

            FuelCapacity = weld.FuelCapacity;
            Fuel = weld.Fuel;
            Activated = weld.Activated;
            _behavior = weld.Quality;
            _uiUpdateNeeded = true;
        }

        public Control MakeControl() => new StatusControl(this);

        private sealed class StatusControl : Control
        {
            private readonly WelderComponent _parent;
            private readonly RichTextLabel _label;

            public StatusControl(WelderComponent parent)
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

                var fuelCap = _parent.FuelCapacity;
                var fuel = _parent.Fuel;

                _label.SetMarkup(Loc.GetString("welder-component-on-examine-detailed-message",
                                               ("colorName", fuel < fuelCap / 4f ? "darkorange" : "orange"),
                                               ("fuelLeft", Math.Round(fuel)),
                                               ("fuelCapacity", fuelCap)));
            }
        }
    }
}
