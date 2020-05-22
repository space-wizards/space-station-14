using System;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.GameObjects.Components.Power;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Power
{
    public class SolarControlConsoleBoundUserInterface : BoundUserInterface
    {
        private SolarControlWindow _window;

        protected override void Open()
        {
            base.Open();

            _window = new SolarControlWindow();
            _window.OnClose += Close;
            _window.OpenCenteredMinSize();
        }

        public SolarControlConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window.Dispose();
            }
        }

        private class SolarControlWindow : SS14Window
        {
            public SolarControlWindow()
            {
                Title = "Solar Control Window";
                var rows = new VBoxContainer();

                var statusHeader = new Label {Text = "TODO"};
                rows.AddChild(statusHeader);

                Contents.AddChild(rows);
            }
        }
    }
}
