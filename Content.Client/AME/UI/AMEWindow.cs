using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using static Content.Shared.AME.SharedAMEControllerComponent;

namespace Content.Client.AME.UI
{
    public class AMEWindow : SS14Window
    {
        public Label InjectionStatus { get; set; }
        public Button EjectButton { get; set; }
        public Button ToggleInjection { get; set; }
        public Button IncreaseFuelButton { get; set; }
        public Button DecreaseFuelButton { get; set; }
        public Button RefreshPartsButton { get; set; }
        public ProgressBar? FuelMeter { get; set; }
        public Label FuelAmount { get; set; }
        public Label InjectionAmount { get; set; }
        public Label CoreCount { get; set; }


        public AMEWindow()
        {
            IoCManager.InjectDependencies(this);

            Title = "Antimatter Control Unit";

            MinSize = SetSize = (250, 250);

            Contents.AddChild(new VBoxContainer
            {
                Children =
                    {
                        new HBoxContainer
                        {
                            Children =
                            {
                                new Label {Text = Loc.GetString("Engine Status") + ": "},
                                (InjectionStatus = new Label {Text = "Not Injecting"})
                            }
                        },
                        new HBoxContainer
                        {
                            Children =
                            {
                                (ToggleInjection = new Button {Text = "Toggle Injection", StyleClasses = {StyleBase.ButtonOpenBoth}, Disabled = true}),
                            }
                        },
                        new HBoxContainer
                        {
                            Children =
                            {
                                new Label {Text = Loc.GetString("Fuel Status") + ": "},
                                (FuelAmount = new Label {Text = "No fuel inserted"})
                            }
                        },
                        new HBoxContainer
                        {
                            Children =
                            {
                                (EjectButton = new Button {Text = "Eject", StyleClasses = {StyleBase.ButtonOpenBoth}, Disabled = true}),
                            }
                        },
                        new HBoxContainer
                        {
                            Children =
                            {
                                new Label {Text = Loc.GetString("Injection amount") + ": "},
                                (InjectionAmount = new Label {Text = "0"})
                            }
                        },
                        new HBoxContainer
                        {
                            Children =
                            {
                                (IncreaseFuelButton = new Button {Text = "Increase", StyleClasses = {StyleBase.ButtonOpenRight}}),
                                (DecreaseFuelButton = new Button {Text = "Decrease", StyleClasses = {StyleBase.ButtonOpenLeft}}),
                            }
                        },
                        new HBoxContainer
                        {
                            Children =
                            {
                                (RefreshPartsButton = new Button {Text = "Refresh Parts", StyleClasses = {StyleBase.ButtonOpenBoth }, Disabled = true }),
                                 new Label { Text = Loc.GetString("Core count") + ": "},
                                (CoreCount = new Label { Text = "0"}),
                            }
                        }
                    }
            });
        }

        /// <summary>
        /// This searches recursively through all the children of "parent"
        /// and sets the Disabled value of any buttons found to "val"
        /// </summary>
        /// <param name="parent">The control which childrens get searched</param>
        /// <param name="val">The value to which disabled gets set</param>
        private void SetButtonDisabledRecursive(Control parent, bool val)
        {
            foreach (var child in parent.Children)
            {
                if (child is Button but)
                {
                    but.Disabled = val;
                    continue;
                }

                if (child.Children != null)
                {
                    SetButtonDisabledRecursive(child, val);
                }
            }
        }

        /// <summary>
        /// Update the UI state when new state data is received from the server.
        /// </summary>
        /// <param name="state">State data sent by the server.</param>
        public void UpdateState(BoundUserInterfaceState state)
        {
            var castState = (AMEControllerBoundUserInterfaceState) state;

            // Disable all buttons if not powered
            if (Contents.Children != null)
            {
                SetButtonDisabledRecursive(Contents, !castState.HasPower);
                EjectButton.Disabled = false;
            }

            if(!castState.HasFuelJar)
            {
                EjectButton.Disabled = true;
                ToggleInjection.Disabled = true;
                FuelAmount.Text = Loc.GetString("No fuel inserted");
            }
            else
            {
                EjectButton.Disabled = false;
                ToggleInjection.Disabled = false;
                FuelAmount.Text = $"{castState.FuelAmount}";
            }

            if(!castState.IsMaster)
            {
                ToggleInjection.Disabled = true;
            }

            if (!castState.Injecting)
            {
                InjectionStatus.Text = Loc.GetString("Not Injecting");
            }
            else
            {
                InjectionStatus.Text = Loc.GetString("Injecting...");
            }

            RefreshPartsButton.Disabled = castState.Injecting;

            CoreCount.Text = $"{castState.CoreCount}";
            InjectionAmount.Text = $"{castState.InjectionAmount}";
        }
    }
}
