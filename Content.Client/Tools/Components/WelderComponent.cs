using System;
using Content.Client.Items.Components;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Tools.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.Tools.Components
{
    [RegisterComponent, Friend(typeof(ToolSystem), typeof(StatusControl))]
    public class WelderComponent : SharedWelderComponent, IItemStatus
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool UiUpdateNeeded { get; set; }

        [ViewVariables]
        public float FuelCapacity { get; set; }

        [ViewVariables]
        public float Fuel { get; set; }

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

                UpdateDraw();
            }

            /// <inheritdoc />
            protected override void FrameUpdate(FrameEventArgs args)
            {
                base.FrameUpdate(args);

                if (!_parent.UiUpdateNeeded)
                {
                    return;
                }
                Update();
            }

            public void Update()
            {
                _parent.UiUpdateNeeded = false;

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
