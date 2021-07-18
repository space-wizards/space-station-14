using System.Collections.Generic;
using Content.Client.Stylesheets;
using Content.Shared.Chemistry.Dispenser;
using Content.Shared.Chemistry.Reagent;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Content.Shared.Chemistry.Dispenser.SharedReagentDispenserComponent;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Chemistry.UI
{
    /// <summary>
    /// Client-side UI used to control a <see cref="SharedReagentDispenserComponent"/>
    /// </summary>
    public class ReagentDispenserWindow : SS14Window
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        /// <summary>Contains info about the reagent container such as it's contents, if one is loaded into the dispenser.</summary>
        private readonly BoxContainer ContainerInfo;

        /// <summary>Sets the dispense amount to 1 when pressed.</summary>
        public Button DispenseButton1 { get; }

        /// <summary>Sets the dispense amount to 5 when pressed.</summary>
        public Button DispenseButton5 { get; }

        /// <summary>Sets the dispense amount to 10 when pressed.</summary>
        public Button DispenseButton10 { get; }

        /// <summary>Sets the dispense amount to 15 when pressed.</summary>
        public Button DispenseButton15 { get; }

        /// <summary>Sets the dispense amount to 20 when pressed.</summary>
        public Button DispenseButton20 { get; }

        /// <summary>Sets the dispense amount to 25 when pressed.</summary>
        public Button DispenseButton25 { get; }

        /// <summary>Sets the dispense amount to 30 when pressed.</summary>
        public Button DispenseButton30 { get; }

        /// <summary>Sets the dispense amount to 50 when pressed.</summary>
        public Button DispenseButton50 { get; }

        /// <summary>Sets the dispense amount to 100 when pressed.</summary>
        public Button DispenseButton100 { get; }

        /// <summary>Ejects the reagent container from the dispenser.</summary>
        public Button ClearButton { get; }

        /// <summary>Removes all reagents from the reagent container.</summary>
        public Button EjectButton { get; }

        /// <summary>A grid of buttons for each reagent which can be dispensed.</summary>
        public GridContainer ChemicalList { get; }

        /// <summary>
        /// Create and initialize the dispenser UI client-side. Creates the basic layout,
        /// actual data isn't filled in until the server sends data about the dispenser.
        /// </summary>
        public ReagentDispenserWindow()
        {
            SetSize = MinSize = (590, 400);
            IoCManager.InjectDependencies(this);

            var dispenseAmountGroup = new ButtonGroup();

            Contents.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children =
                {
                    //First, our dispense amount buttons
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        Children =
                        {
                            new Label {Text = Loc.GetString("reagent-dispenser-window-amount-to-dispense-label")},
                            //Padding
                            new Control {MinSize = (20, 0)},
                            (DispenseButton1 = new Button {Text = "1", Group = dispenseAmountGroup, StyleClasses = { StyleBase.ButtonOpenRight }}),
                            (DispenseButton5 = new Button {Text = "5", Group = dispenseAmountGroup, StyleClasses = { StyleBase.ButtonOpenBoth }}),
                            (DispenseButton10 = new Button {Text = "10", Group = dispenseAmountGroup, StyleClasses = { StyleBase.ButtonOpenBoth }}),
                            (DispenseButton15 = new Button {Text = "15", Group = dispenseAmountGroup, StyleClasses = { StyleBase.ButtonOpenBoth }}),
                            (DispenseButton20 = new Button {Text = "20", Group = dispenseAmountGroup, StyleClasses = { StyleBase.ButtonOpenBoth }}),
                            (DispenseButton25 = new Button {Text = "25", Group = dispenseAmountGroup, StyleClasses = { StyleBase.ButtonOpenBoth }}),
                            (DispenseButton30 = new Button {Text = "30", Group = dispenseAmountGroup, StyleClasses = { StyleBase.ButtonOpenBoth }}),
                            (DispenseButton50 = new Button {Text = "50", Group = dispenseAmountGroup, StyleClasses = { StyleBase.ButtonOpenBoth }}),
                            (DispenseButton100 = new Button {Text = "100", Group = dispenseAmountGroup, StyleClasses = { StyleBase.ButtonOpenLeft }}),
                        }
                    },
                    //Padding
                    new Control {MinSize = (0.0f, 10.0f)},
                    //Grid of which reagents can be dispensed.
                    (ChemicalList = new GridContainer
                    {
                        Columns = 5
                    }),
                    //Padding
                    new Control {MinSize = (0.0f, 10.0f)},
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        Children =
                        {
                            new Label {Text = Loc.GetString("reagent-dispenser-window-container-label") + " "},
                            (ClearButton = new Button {Text = Loc.GetString("reagent-dispenser-window-clear-button"), StyleClasses = {StyleBase.ButtonOpenRight}}),
                            (EjectButton = new Button {Text = Loc.GetString("reagent-dispenser-window-eject-button"), StyleClasses = {StyleBase.ButtonOpenLeft}})
                        }
                    },
                    //Wrap the container info in a PanelContainer so we can color it's background differently.
                    new PanelContainer
                    {
                        VerticalExpand = true,
                        SizeFlagsStretchRatio = 6,
                        MinSize = (0, 150),
                        PanelOverride = new StyleBoxFlat
                        {
                            BackgroundColor = new Color(27, 27, 30)
                        },
                        Children =
                        {
                            //Currently empty, when server sends state data this will have container contents and fill volume.
                            (ContainerInfo = new BoxContainer
                            {
                                Orientation = LayoutOrientation.Vertical,
                                HorizontalExpand = true,
                                Children =
                                {
                                    new Label
                                    {
                                        Text = Loc.GetString("reagent-dispenser-window-no-container-loaded-text")
                                    }
                                }
                            }),
                        }
                    },
                }
            });
        }

        /// <summary>
        /// Update the button grid of reagents which can be dispensed.
        /// <para>The actions for these buttons are set in <see cref="ReagentDispenserBoundUserInterface.UpdateReagentsList"/>.</para>
        /// </summary>
        /// <param name="inventory">Reagents which can be dispensed by this dispenser</param>
        public void UpdateReagentsList(List<ReagentDispenserInventoryEntry> inventory)
        {
            if (ChemicalList == null) return;
            if (inventory == null) return;

            ChemicalList.Children.Clear();

            foreach (var entry in inventory)
            {
                if (_prototypeManager.TryIndex(entry.ID, out ReagentPrototype? proto))
                {
                    ChemicalList.AddChild(new Button {Text = proto.Name});
                }
                else
                {
                    ChemicalList.AddChild(new Button {Text = Loc.GetString("reagent-dispenser-window-reagent-name-not-found-text") });
                }
            }
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
            var castState = (ReagentDispenserBoundUserInterfaceState) state;
            Title = castState.DispenserName;
            UpdateContainerInfo(castState);

            // Disable all buttons if not powered
            if (Contents.Children != null)
            {
                SetButtonDisabledRecursive(Contents, !castState.HasPower);
                EjectButton.Disabled = false;
            }

            // Disable the Clear & Eject button if no beaker
            if (!castState.HasBeaker)
            {
                ClearButton.Disabled = true;
                EjectButton.Disabled = true;
            }

            switch (castState.SelectedDispenseAmount.Int())
            {
                case 1:
                    DispenseButton1.Pressed = true;
                    break;
                case 5:
                    DispenseButton5.Pressed = true;
                    break;
                case 10:
                    DispenseButton10.Pressed = true;
                    break;
                case 15:
                    DispenseButton15.Pressed = true;
                    break;
                case 20:
                    DispenseButton20.Pressed = true;
                    break;
                case 25:
                    DispenseButton25.Pressed = true;
                    break;
                case 30:
                    DispenseButton30.Pressed = true;
                    break;
                case 50:
                    DispenseButton50.Pressed = true;
                    break;
                case 100:
                    DispenseButton100.Pressed = true;
                    break;
            }
        }

        /// <summary>
        /// Update the fill state and list of reagents held by the current reagent container, if applicable.
        /// <para>Also highlights a reagent if it's dispense button is being mouse hovered.</para>
        /// </summary>
        /// <param name="state">State data for the dispenser.</param>
        /// <param name="highlightedReagentId">Prototype id of the reagent whose dispense button is currently being mouse hovered.</param>
        public void UpdateContainerInfo(ReagentDispenserBoundUserInterfaceState state, string highlightedReagentId = "")
        {
            ContainerInfo.Children.Clear();

            if (!state.HasBeaker)
            {
                ContainerInfo.Children.Add(new Label {Text = Loc.GetString("reagent-dispenser-window-no-container-loaded-text") });
                return;
            }

            ContainerInfo.Children.Add(new BoxContainer // Name of the container and its fill status (Ex: 44/100u)
            {
                Orientation = LayoutOrientation.Horizontal,
                Children =
                {
                    new Label {Text = $"{state.ContainerName}: "},
                    new Label
                    {
                        Text = $"{state.BeakerCurrentVolume}/{state.BeakerMaxVolume}",
                        StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
                    }
                }
            });

            if (state.ContainerReagents == null)
            {
                return;
            }

            foreach (var reagent in state.ContainerReagents)
            {
                var name = Loc.GetString("reagent-dispenser-window-unknown-reagent-text");
                //Try to the prototype for the given reagent. This gives us it's name.
                if (_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype? proto))
                {
                    name = proto.Name;
                }

                //Check if the reagent is being moused over. If so, color it green.
                if (proto != null && proto.ID == highlightedReagentId)
                {
                    ContainerInfo.Children.Add(new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        Children =
                        {
                            new Label
                            {
                                Text = $"{name}: ",
                                StyleClasses = {StyleNano.StyleClassPowerStateGood}
                            },
                            new Label
                            {
                                Text = Loc.GetString("reagent-dispenser-window-quantity-label-text", ("quantity", reagent.Quantity)),
                                StyleClasses = {StyleNano.StyleClassPowerStateGood}
                            }
                        }
                    });
                }
                else //Otherwise, color it the normal colors.
                {
                    ContainerInfo.Children.Add(new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        Children =
                        {
                            new Label {Text = $"{name}: "},
                            new Label
                            {
                                Text = Loc.GetString("reagent-dispenser-window-quantity-label-text", ("quantity", reagent.Quantity)),
                                StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
                            }
                        }
                    });
                }
            }
        }
    }
}
