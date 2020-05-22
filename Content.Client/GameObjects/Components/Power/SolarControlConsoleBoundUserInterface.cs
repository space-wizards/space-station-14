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
        private SolarControlConsoleBoundInterfaceState _lastState = new SolarControlConsoleBoundInterfaceState(0, 0, 0, 0);

        protected override void Open()
        {
            base.Open();

            _window = new SolarControlWindow();
            _window.OnClose += Close;
            _window.PanelRotation.OnTextEntered += (text) => {
                double value;
                if (double.TryParse(text.Text, out value))
                {
                    SolarControlConsoleAdjustMessage msg = new SolarControlConsoleAdjustMessage();
                    msg.Rotation = Angle.FromDegrees(value);
                    msg.AngularVelocity = _lastState.AngularVelocity;
                    SendMessage(msg);
                }
            };
            _window.PanelVelocity.OnTextEntered += (text) => {
                double value;
                if (double.TryParse(text.Text, out value))
                {
                    SolarControlConsoleAdjustMessage msg = new SolarControlConsoleAdjustMessage();
                    msg.Rotation = _lastState.Rotation;
                    msg.AngularVelocity = Angle.FromDegrees(value / 60);
                    SendMessage(msg);
                }
            };
            _window.OpenCenteredMinSize();
        }

        public SolarControlConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private string FormatAngle(Angle d)
        {
            return (Math.Floor(d.Degrees * 10) / 10).ToString();
        }

        // The idea behind this is to prevent every update from the server
        //  breaking the textfield.
        private void UpdateField(LineEdit field, string newValue)
        {
            if (!field.HasKeyboardFocus())
            {
                field.Text = newValue;
            }
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            SolarControlConsoleBoundInterfaceState scc = (SolarControlConsoleBoundInterfaceState) state;
            _lastState = scc;
            _window.OutputPower.Text = ((int) Math.Floor(scc.OutputPower)).ToString();
            _window.SunAngle.Text = FormatAngle(scc.TowardsSun);
            UpdateField(_window.PanelRotation, FormatAngle(scc.Rotation));
            UpdateField(_window.PanelVelocity, FormatAngle(scc.AngularVelocity * 60));
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
            public Label OutputPower;
            public Label SunAngle;

            public string OldSetPanelRotation;
            public LineEdit PanelRotation;
            public string OldSetPanelVelocity;
            public LineEdit PanelVelocity;

            public SolarControlWindow()
            {
                Title = "Solar Control Window";
                var rows = new GridContainer();
                rows.Columns = 2;

                // little secret: the reason I put the values
                // in the first column is because otherwise the UI
                // layouter autoresizes the window to be too small
                rows.AddChild(new Label {Text = "Output Power:"});
                rows.AddChild(new Label {Text = ""});

                rows.AddChild(OutputPower = new Label {Text = "?"});
                rows.AddChild(new Label {Text = "W"});

                rows.AddChild(new Label {Text = "Sun Angle:"});
                rows.AddChild(new Label {Text = ""});

                rows.AddChild(SunAngle = new Label {Text = "?"});
                rows.AddChild(new Label {Text = "°"});

                rows.AddChild(new Label {Text = "Panel Angle:"});
                rows.AddChild(new Label {Text = ""});

                rows.AddChild(PanelRotation = new LineEdit());
                rows.AddChild(new Label {Text = "°"});

                rows.AddChild(new Label {Text = "Panel Angular Velocity:"});
                rows.AddChild(new Label {Text = ""});

                rows.AddChild(PanelVelocity = new LineEdit());
                rows.AddChild(new Label {Text = "°/min."});

                rows.AddChild(new Label {Text = "Press Enter to confirm."});
                rows.AddChild(new Label {Text = ""});

                PanelRotation.SizeFlagsHorizontal = SizeFlags.FillExpand;
                PanelVelocity.SizeFlagsHorizontal = SizeFlags.FillExpand;
                rows.SizeFlagsHorizontal = SizeFlags.Fill;

                Contents.AddChild(rows);
            }
        }
    }
}
