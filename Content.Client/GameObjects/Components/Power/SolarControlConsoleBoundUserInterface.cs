using System;
using Content.Shared.GameObjects.Components.Power;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.GameObjects.Components.Power
{
    [UsedImplicitly]
    public class SolarControlConsoleBoundUserInterface : ComputerBoundUserInterface<SolarControlWindow, SolarControlConsoleBoundInterfaceState>
    {
        public SolarControlConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) {}
    }

    public sealed class SolarControlWindow : SS14Window, IComputerWindow<SolarControlConsoleBoundInterfaceState>
    {
        public readonly Label OutputPower;
        public readonly Label SunAngle;

        private readonly SolarControlNotARadar _notARadar;

        public readonly LineEdit PanelRotation;
        public readonly LineEdit PanelVelocity;

        private SolarControlConsoleBoundInterfaceState _lastState = new(0, 0, 0, 0);

        public SolarControlWindow()
        {
            Title = Loc.GetString("solar-control-window-title");

            var rows = new GridContainer();
            rows.Columns = 2;

            // little secret: the reason I put the values
            // in the first column is because otherwise the UI
            // layouter autoresizes the window to be too small
            rows.AddChild(new Label {Text = Loc.GetString("solar-control-window-output-power")});
            rows.AddChild(new Label {Text = ""});

            rows.AddChild(OutputPower = new Label {Text = "?"});
            rows.AddChild(new Label {Text = Loc.GetString("solar-control-window-watts")});

            rows.AddChild(new Label {Text = Loc.GetString("solar-control-window-sun-angle")});
            rows.AddChild(new Label {Text = ""});

            rows.AddChild(SunAngle = new Label {Text = "?"});
            rows.AddChild(new Label {Text = Loc.GetString("solar-control-window-degrees")});

            rows.AddChild(new Label {Text = Loc.GetString("solar-control-window-panel-angle")});
            rows.AddChild(new Label {Text = ""});

            rows.AddChild(PanelRotation = new LineEdit());
            rows.AddChild(new Label {Text = Loc.GetString("solar-control-window-degrees")});

            rows.AddChild(new Label {Text = Loc.GetString("solar-control-window-panel-angular-velocity")});
            rows.AddChild(new Label {Text = ""});

            rows.AddChild(PanelVelocity = new LineEdit());
            rows.AddChild(new Label {Text = Loc.GetString("solar-control-window-degrees-per-minute")});

            rows.AddChild(new Label {Text = Loc.GetString("solar-control-window-press-enter-to-confirm")});
            rows.AddChild(new Label {Text = ""});

            PanelRotation.HorizontalExpand = true;
            PanelVelocity.HorizontalExpand = true;

            _notARadar = new SolarControlNotARadar();

            var outerColumns = new HBoxContainer();
            outerColumns.AddChild(rows);
            outerColumns.AddChild(_notARadar);
            Contents.AddChild(outerColumns);
            Resizable = false;
        }

        public void SetupComputerWindow(ComputerBoundUserInterfaceBase cb)
        {
            PanelRotation.OnTextEntered += (text) => {
                double value;
                if (double.TryParse(text.Text, out value))
                {
                    SolarControlConsoleAdjustMessage msg = new SolarControlConsoleAdjustMessage();
                    msg.Rotation = Angle.FromDegrees(value);
                    msg.AngularVelocity = _lastState.AngularVelocity;
                    cb.SendMessage(msg);
                    // Predict this...
                    _lastState.Rotation = msg.Rotation;
                    _notARadar.UpdateState(_lastState);
                }
            };
            PanelVelocity.OnTextEntered += (text) => {
                double value;
                if (double.TryParse(text.Text, out value))
                {
                    SolarControlConsoleAdjustMessage msg = new SolarControlConsoleAdjustMessage();
                    msg.Rotation = _notARadar.PredictedPanelRotation;
                    msg.AngularVelocity = Angle.FromDegrees(value / 60);
                    cb.SendMessage(msg);
                    // Predict this...
                    _lastState.Rotation = _notARadar.PredictedPanelRotation;
                    _lastState.AngularVelocity = msg.AngularVelocity;
                    _notARadar.UpdateState(_lastState);
                }
            };
        }

        private string FormatAngle(Angle d)
        {
            return d.Degrees.ToString("F1");
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

        public void UpdateState(SolarControlConsoleBoundInterfaceState scc)
        {
            _lastState = scc;
            _notARadar.UpdateState(scc);
            OutputPower.Text = ((int) MathF.Floor(scc.OutputPower)).ToString();
            SunAngle.Text = FormatAngle(scc.TowardsSun);
            UpdateField(PanelRotation, FormatAngle(scc.Rotation));
            UpdateField(PanelVelocity, FormatAngle(scc.AngularVelocity * 60));
        }

        private sealed class SolarControlNotARadar : Control
        {
            // This is used for client-side prediction of the panel rotation.
            // This makes the display feel a lot smoother.
            private IGameTiming _gameTiming = IoCManager.Resolve<IGameTiming>();

            private SolarControlConsoleBoundInterfaceState _lastState = new(0, 0, 0, 0);

            private TimeSpan _lastStateTime = TimeSpan.Zero;

            public const int StandardSizeFull = 290;
            public const int StandardRadiusCircle = 140;
            public int SizeFull => (int) (StandardSizeFull * UIScale);
            public int RadiusCircle => (int) (StandardRadiusCircle * UIScale);

            public SolarControlNotARadar()
            {
                MinSize = (SizeFull, SizeFull);
            }

            public void UpdateState(SolarControlConsoleBoundInterfaceState ls)
            {
                _lastState = ls;
                _lastStateTime = _gameTiming.CurTime;
            }

            public Angle PredictedPanelRotation => _lastState.Rotation + (_lastState.AngularVelocity * ((_gameTiming.CurTime - _lastStateTime).TotalSeconds));

            protected override void Draw(DrawingHandleScreen handle)
            {
                var point = SizeFull / 2;
                var fakeAA = new Color(0.08f, 0.08f, 0.08f);
                var gridLines = new Color(0.08f, 0.08f, 0.08f);
                var panelExtentCutback = 4;
                var gridLinesRadial = 8;
                var gridLinesEquatorial = 8;

                // Draw base
                handle.DrawCircle((point, point), RadiusCircle + 1, fakeAA);
                handle.DrawCircle((point, point), RadiusCircle, Color.Black);

                // Draw grid lines
                for (var i = 0; i < gridLinesEquatorial; i++)
                {
                    handle.DrawCircle((point, point), (RadiusCircle / gridLinesEquatorial) * i, gridLines, false);
                }

                for (var i = 0; i < gridLinesRadial; i++)
                {
                    Angle angle = (Math.PI / gridLinesRadial) * i;
                    var aExtent = angle.ToVec() * RadiusCircle;
                    handle.DrawLine((point, point) - aExtent, (point, point) + aExtent, gridLines);
                }

                // The rotations need to be adjusted because Y is inverted in Robust (like BYOND)
                Vector2 rotMul = (1, -1);
                // Hotfix corrections I don't understand
                Angle rotOfs = new Angle(Math.PI * -0.5);

                Angle predictedPanelRotation = PredictedPanelRotation;

                var extent = (predictedPanelRotation + rotOfs).ToVec() * rotMul * RadiusCircle;
                Vector2 extentOrtho = (extent.Y, -extent.X);
                handle.DrawLine((point, point) - extentOrtho, (point, point) + extentOrtho, Color.White);
                handle.DrawLine((point, point) + (extent / panelExtentCutback), (point, point) + extent - (extent / panelExtentCutback), Color.DarkGray);

                var sunExtent = (_lastState.TowardsSun + rotOfs).ToVec() * rotMul * RadiusCircle;
                handle.DrawLine((point, point) + sunExtent, (point, point), Color.Yellow);
            }
        }
    }
}
