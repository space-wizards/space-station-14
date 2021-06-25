using Content.Shared.Gravity;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.Gravity
{
    [UsedImplicitly]
    public class GravityGeneratorBoundUserInterface : BoundUserInterface
    {
        private GravityGeneratorWindow? _window;

        public bool IsOn;

        public GravityGeneratorBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base (owner, uiKey)
        {
            SendMessage(new SharedGravityGeneratorComponent.GeneratorStatusRequestMessage());
        }

        protected override void Open()
        {
            base.Open();

            IsOn = false;

            _window = new GravityGeneratorWindow(this);

            _window.Switch.OnPressed += (_) =>
            {
                SendMessage(new SharedGravityGeneratorComponent.SwitchGeneratorMessage(!IsOn));
                SendMessage(new SharedGravityGeneratorComponent.GeneratorStatusRequestMessage());
            };

            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (SharedGravityGeneratorComponent.GeneratorState) state;
            IsOn = castState.On;
            _window?.UpdateButton();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;

            _window?.Dispose();
        }
    }

    public class GravityGeneratorWindow : SS14Window
    {
        public Label Status;

        public Button Switch;

        public GravityGeneratorBoundUserInterface Owner;

        public GravityGeneratorWindow(GravityGeneratorBoundUserInterface ui)
        {
            IoCManager.InjectDependencies(this);

            Owner = ui;

            Title = Loc.GetString("gravity-generator-window-title");

            var vBox = new VBoxContainer
            {
                MinSize = new Vector2(250, 100)
            };
            Status = new Label
            {
                Text = $"{Loc.GetString("gravity-generator-window-status-label")} {Loc.GetString(Owner.IsOn ? "gravity-generator-window-is-on" : "gravity-generator-window-is-off")}",
                FontColorOverride = Owner.IsOn ? Color.ForestGreen : Color.Red
            };
            Switch = new Button
            {
                Text = Loc.GetString(Owner.IsOn ? "gravity-generator-window-turn-off-button" : "gravity-generator-window-turn-on-button"),
                TextAlign = Label.AlignMode.Center,
                MinSize = new Vector2(150, 60)
            };

            vBox.AddChild(Status);
            vBox.AddChild(Switch);

            Contents.AddChild(vBox);
        }

        public void UpdateButton()
        {
            Status.Text = $"{Loc.GetString("gravity-generator-window-status-label")} {Loc.GetString(Owner.IsOn ? "gravity-generator-window-is-on" : "gravity-generator-window-is-off")}";
            Status.FontColorOverride = Owner.IsOn ? Color.ForestGreen : Color.Red;
            Switch.Text = Loc.GetString(Owner.IsOn ? "gravity-generator-window-turn-off-button" : "gravity-generator-window-turn-on-button");
        }
    }
}
