using System.Collections.Generic;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.Cargo.UI
{
    class CargoConsoleOrderMenu : SS14Window
    {
        public LineEdit Requester { get; set; }
        public LineEdit Reason { get; set; }
        public SpinBox Amount { get; set; }
        public Button SubmitButton { get; set; }

        public CargoConsoleOrderMenu()
        {
            IoCManager.InjectDependencies(this);

            Title = Loc.GetString("cargo-console-order-menu-title");

            var vBox = new VBoxContainer();
            var gridContainer = new GridContainer { Columns = 2 };

            var requesterLabel = new Label { Text = Loc.GetString("cargo-console-order-menu-requester-label") };
            Requester = new LineEdit();
            gridContainer.AddChild(requesterLabel);
            gridContainer.AddChild(Requester);

            var reasonLabel = new Label { Text = Loc.GetString("cargo-console-order-menu-reason-label:") };
            Reason = new LineEdit();
            gridContainer.AddChild(reasonLabel);
            gridContainer.AddChild(Reason);

            var amountLabel = new Label { Text = Loc.GetString("cargo-console-order-menu-amount-label:") };
            Amount = new SpinBox
            {
                HorizontalExpand = true,
                Value = 1
            };
            Amount.SetButtons(new List<int>() { -3, -2, -1 }, new List<int>() { 1, 2, 3 });
            Amount.IsValid = (n) => {
                return (n > 0);
            };
            gridContainer.AddChild(amountLabel);
            gridContainer.AddChild(Amount);

            vBox.AddChild(gridContainer);

            SubmitButton = new Button()
            {
                Text = Loc.GetString("cargo-console-order-menu-submit-button"),
                TextAlign = Label.AlignMode.Center,
            };
            vBox.AddChild(SubmitButton);

            Contents.AddChild(vBox);
        }
    }
}
