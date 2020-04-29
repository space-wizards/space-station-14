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
        [ViewVariables(VVAccess.ReadWrite)] private bool _uiUpdateNeeded;
        private bool _statusShowBehavior;
        [ViewVariables] public float FuelCapacity { get; private set; }
        [ViewVariables] public float Fuel { get; private set; }
        [ViewVariables] public bool Activated { get; private set; }
        [ViewVariables] public bool StatusShowBehavior => _statusShowBehavior;
        [ViewVariables]
        public override Tool Behavior
        {
            get => _behavior;
            set {}
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _statusShowBehavior, "statusShowBehavior", false);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (!(curState is ToolComponentState cast))
                return;

            FuelCapacity = cast.FuelCapacity;
            Fuel = cast.Fuel;
            Activated = cast.Activated;
            _behavior = cast.Behavior;

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

                if (_parent.Behavior == Tool.Welder)
                {
                    var fuelCap = _parent.FuelCapacity;
                    var fuel = _parent.Fuel;

                    _label.SetMarkup(Loc.GetString("Fuel: [color={0}]{1}/{2}[/color]",
                        fuel < fuelCap / 4f ? "darkorange" : "orange", Math.Round(fuel), fuelCap));
                }
                else
                {
                    if(!_parent.StatusShowBehavior)
                        _label.SetMarkup(string.Empty);
                    else
                        _label.SetMarkup(_parent.Behavior.ToString());
                }

            }
        }
    }
}
