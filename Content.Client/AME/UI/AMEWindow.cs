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

            Title = Loc.GetString("ame-window-title");

            MinSize = SetSize = (250, 250);

            Contents.AddChild(new VBoxContainer
            {
                Children =
                    {
                        new HBoxContainer
                        {
                            Children =
                            {
                                new Label {Text = Loc.GetString("ame-window-engine-status-label")},
                                (InjectionStatus = new Label {Text = Loc.GetString("ame-window-engine-injection-status-not-injecting-label")})
                            }
                        },
                        new HBoxContainer
                        {
                            Children =
                            {
                                (ToggleInjection = new Button {Text = Loc.GetString("ame-window-toggle-injection-button"), StyleClasses = {StyleBase.ButtonOpenBoth}, Disabled = true}),
                            }
                        },
                        new HBoxContainer
                        {
                            Children =
                            {
                                new Label {Text = Loc.GetString("ame-window-fuel-status-label")},
                                (FuelAmount = new Label {Text = Loc.GetString("ame-window-fuel-not-inserted-text")})
                            }
                        },
                        new HBoxContainer
                        {
                            Children =
                            {
                                (EjectButton = new Button {Text = Loc.GetString("ame-window-eject-button"), StyleClasses = {StyleBase.ButtonOpenBoth}, Disabled = true}),
                            }
                        },
                        new HBoxContainer
                        {
                            Children =
                            {
                                new Label {Text = Loc.GetString("ame-window-injection-amount-label")},
                                (InjectionAmount = new Label {Text = "0"})
                            }
                        },
                        new HBoxContainer
                        {
                            Children =
                            {
                                (IncreaseFuelButton = new Button {Text = Loc.GetString("ame-window-increase-fuel-button"), StyleClasses = {StyleBase.ButtonOpenRight}}),
                                (DecreaseFuelButton = new Button {Text = Loc.GetString("ame-window-decrease-fuel-button"), StyleClasses = {StyleBase.ButtonOpenLeft}}),
                            }
                        },
                        new HBoxContainer
                        {
                            Children =
                            {
                                (RefreshPartsButton = new Button {Text = Loc.GetString("ame-window-refresh-parts-button"), StyleClasses = {StyleBase.ButtonOpenBoth }, Disabled = true }),
                                 new Label { Text = Loc.GetString("ame-window-core-count-label")},
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
                FuelAmount.Text = Loc.GetString("ame-window-fuel-not-inserted-text");
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
                InjectionStatus.Text = Loc.GetString("ame-window-engine-injection-status-not-injecting-label");
            }
            else
            {
                InjectionStatus.Text = Loc.GetString("ame-window-engine-injection-status-injecting-label");
            }

            RefreshPartsButton.Disabled = castState.Injecting;

            CoreCount.Text = $"{castState.CoreCount}";
            InjectionAmount.Text = $"{castState.InjectionAmount}";
        }
    }
}
