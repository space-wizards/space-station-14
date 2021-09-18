using Content.Client.Message;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.Gravity.UI {
    public class GravityGeneratorWindow : SS14Window
    {
        public RichTextLabel Status = new();

        public Button Switch;

        public GravityGeneratorBoundUserInterface Owner;

        public GravityGeneratorWindow(GravityGeneratorBoundUserInterface ui)
        {
            IoCManager.InjectDependencies(this);

            Owner = ui;

            Title = Loc.GetString("gravity-generator-window-title");

            var vBox = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                MinSize = new Vector2(250, 100)
            };

            vBox.AddChild(Status);
            vBox.AddChild(Switch = new Button
            {
                TextAlign = Label.AlignMode.Center,
                MinSize = new Vector2(150, 60)
            });

            Contents.AddChild(vBox);

            UpdateButton();
        }

        public void UpdateButton()
        {
            Status.SetMarkup(
                $"{Loc.GetString("gravity-generator-window-status-label")}{Loc.GetString(Owner.IsOn ? "gravity-generator-window-is-on" : "gravity-generator-window-is-off")}");
            Switch.Text = Loc.GetString(Owner.IsOn
                                            ? "gravity-generator-window-turn-off-button"
                                            : "gravity-generator-window-turn-on-button");
        }
    }
}
