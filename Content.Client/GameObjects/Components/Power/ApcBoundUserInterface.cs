using System;
using Content.Shared.GameObjects.Components.Power;
using OpenTK.Graphics.OpenGL4;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.Components.Power
{
    public class ApcBoundUserInterface : BoundUserInterface
    {
        private SS14Window _window;
        private BaseButton _breakerButton;
        private Label _externalPowerStateLabel;
        private ProgressBar _chargeBar;

        protected override void Open()
        {
            base.Open();

            _window = new SS14Window(IoCManager.Resolve<IDisplayManager>())
            {
                MarginRight = 426.0f, MarginBottom = 270.0f
            };
            _window.OnClose += Close;

            var rows = new VBoxContainer("Rows");

            var statusHeader = new Label("StatusHeader") {Text = "Power Status: "};
            rows.AddChild(statusHeader);

            var breaker = new HBoxContainer("Breaker");
            var breakerLabel = new Label("Label") {Text = "Main Breaker: "};
            _breakerButton = new CheckButton();
            _breakerButton.OnPressed += _ => SendMessage(new ApcToggleMainBreakerMessage());
            breaker.AddChild(breakerLabel);
            breaker.AddChild(_breakerButton);
            rows.AddChild(breaker);

            var externalStatus = new HBoxContainer("ExternalStatus");
            var externalStatusLabel = new Label("Label") {Text = "External Power: "};
            _externalPowerStateLabel = new Label("Status") {Text = "Good"};
            externalStatus.AddChild(externalStatusLabel);
            externalStatus.AddChild(_externalPowerStateLabel);
            rows.AddChild(externalStatus);

            var charge = new HBoxContainer("Charge");
            var chargeLabel = new Label("Label") {Text = "Charge:"};
            _chargeBar = new ProgressBar("Charge")
            {
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
                MinValue = 0.0f,
                MaxValue = 1.0f,
                Page = 0.0f,
                Value = 0.5f
            };
            charge.AddChild(chargeLabel);
            charge.AddChild(_chargeBar);
            rows.AddChild(charge);

            _window.Contents.AddChild(rows);
            _window.AddToScreen();
        }

        public ApcBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (ApcBoundInterfaceState) state;

            _breakerButton.Pressed = castState.MainBreaker;
            switch (castState.ApcExternalPower)
            {
                case ApcExternalPowerState.None:
                    _externalPowerStateLabel.Text = "None";
                    break;
                case ApcExternalPowerState.Low:
                    _externalPowerStateLabel.Text = "Low";
                    break;
                case ApcExternalPowerState.Good:
                    _externalPowerStateLabel.Text = "Good";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _chargeBar.Value = castState.Charge;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window.Dispose();
            }
        }

        private class ApcWindow : SS14Window
        {
            //protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Power/Apc.tscn");

            public ApcWindow(IDisplayManager displayMan) : base(displayMan) { }
        }
    }
}
