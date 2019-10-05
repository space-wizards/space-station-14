using System;
using System.Collections.Generic;
using Content.Client.UserInterface;
using Content.Shared.Chemistry;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Content.Shared.GameObjects.Components.Chemistry.SharedReagentDispenserComponent;

namespace Content.Client.GameObjects.Components.Chemistry
{
    /// <summary>
    /// Client-side UI used to control a <see cref="ReagentDispenserComponent"/>
    /// </summary>
    public class ReagentDispenserWindow : SS14Window
    {
        /// <summary>Sets the dispense amount to 1 when pressed.</summary>
        public Button DispenseButton1;
        /// <summary>Sets the dispense amount to 5 when pressed.</summary>
        public Button DispenseButton5;
        /// <summary>Sets the dispense amount to 10 when pressed.</summary>
        public Button DispenseButton10;
        /// <summary>Sets the dispense amount to 25 when pressed.</summary>
        public Button DispenseButton25;
        /// <summary>Sets the dispense amount to 50 when pressed.</summary>
        public Button DispenseButton50;
        /// <summary>Sets the dispense amount to 100 when pressed.</summary>
        public Button DispenseButton100;

        /// <summary>Contains info about the reagent container such as it's contents, if one is loaded into the dispenser.</summary>
        public VBoxContainer ContainerInfo;

        /// <summary>Ejects the reagent container from the dispenser.</summary>
        public Button ClearButton;
        /// <summary>Removes all reagents from the reagent container.</summary>
        public Button EjectButton;

        /// <summary>A grid of buttons for each reagent which can be dispensed.</summary>
        public GridContainer ChemicalList;

#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649

        /// <summary>
        /// Create and initialize the dispenser UI client-side. Creates the basic layout,
        /// actual data isn't filled in until the server sends data about the dispenser.
        /// </summary>
        public ReagentDispenserWindow()
        {
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            _localizationManager = IoCManager.Resolve<ILocalizationManager>();

            Contents.AddChild(new VBoxContainer
            {
                Children =
                {
                    //First, our dispense amount buttons
                    new HBoxContainer
                    {
                        Children =
                        {
                            new Label{Text = _localizationManager.GetString("Amount    ")},
                            (DispenseButton1 = new Button{Text = "1"}),
                            (DispenseButton5 = new Button{Text = "5"}),
                            (DispenseButton10 = new Button{Text = "10"}),
                            (DispenseButton25 = new Button{Text = "25"}),
                            (DispenseButton50 = new Button{Text = "50"}),
                            (DispenseButton100 = new Button{Text = "100"}),
                        }
                    },
                    new Panel{CustomMinimumSize = (0.0f, 10.0f)}, //Padding
                    (ChemicalList = new GridContainer //Grid of which reagents can be dispensed.
                    {
                        CustomMinimumSize = (470.0f, 200.0f),
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                        Columns = 5
                    }),
                    new Panel{CustomMinimumSize = (0.0f, 10.0f)}, //Padding
                    new HBoxContainer
                    {
                        Children =
                        {
                            new Label{Text = _localizationManager.GetString("Container: ")},
                            (ClearButton = new Button{Text = _localizationManager.GetString("Clear")}),
                            (EjectButton = new Button{Text = _localizationManager.GetString("Eject")})
                        }
                    },
                    new PanelContainer //Wrap the container info in a PanelContainer so we can color it's background differently.
                    {
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        SizeFlagsStretchRatio = 6,
                        PanelOverride = new StyleBoxFlat
                        {
                            BackgroundColor = new Color(27, 27, 30)
                        },
                        Children =
                        {
                            (ContainerInfo = new VBoxContainer //Currently empty, when server sends state data this will have container contents and fill volume.
                            {
                                MarginLeft = 5.0f,
                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                Children =
                                {
                                    new Label{Text = _localizationManager.GetString("No container loaded.")}
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
                if (_prototypeManager.TryIndex(entry.ID, out ReagentPrototype proto))
                {
                    ChemicalList.AddChild(new Button { Text = proto.Name });
                }
                else
                {
                    ChemicalList.AddChild(new Button { Text = _localizationManager.GetString("Reagent name not found") });
                }

            }
        }

        /// <summary>
        /// Update the UI state when new state data is received from the server.
        /// </summary>
        /// <param name="state">State data sent by the server.</param>
        public void UpdateState(BoundUserInterfaceState state)
        {
            var castState = (ReagentDispenserBoundUserInterfaceState)state;
            Title = castState.DispenserName;
            UpdateContainerInfo(castState);
        }

        /// <summary>
        /// Update the fill state and list of reagents held by the current reagent container, if applicable.
        /// <para>Also highlights a reagent if it's dispense button is being mouse hovered.</para>
        /// </summary>
        /// <param name="state">State data for the dispenser.</param>
        /// <param name="highlightedReagentId">Prototype id of the reagent whose dispense button is currently being mouse hovered.</param>
        public void UpdateContainerInfo(ReagentDispenserBoundUserInterfaceState state, string highlightedReagentId = "InvalidReagent")
        {
            ContainerInfo.Children.Clear();
            if (state.HasBeaker) //If the dispenser doesn't have a beaker/container don't bother with this.
            {
                ContainerInfo.Children.Add(new HBoxContainer //Name of the container and it's fill status (Ex: 44/100u)
                {
                    Children =
                    {
                        new Label{Text = $"{state.ContainerName}: "},
                        new Label{Text = $"{state.BeakerCurrentVolume}/{state.BeakerMaxVolume}", StyleClasses = { NanoStyle.StyleClassLabelSecondaryColor }}
                    }
                });
                //List the reagents in the container if it has any at all.
                if (state.ContainerReagents != null)
                {
                    //Loop through the reagents in the container.
                    foreach (var reagent in state.ContainerReagents)
                    {
                        //Try to the prototype for the given reagent. This gives us it's name.
                        if (_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype proto))
                        {
                            //Check if the reagent is being moused over. If so, color it green.
                            if (proto.ID == highlightedReagentId)
                            {
                                ContainerInfo.Children.Add(new HBoxContainer
                                {
                                    Children =
                                    {
                                        new Label {Text = $"{proto.Name}: ", StyleClasses = {NanoStyle.StyleClassPowerStateGood}},
                                        new Label
                                        {
                                            Text = $"{reagent.Quantity}u",
                                            StyleClasses = {NanoStyle.StyleClassPowerStateGood}
                                        }
                                    }
                                });
                            }
                            else //Otherwise, color it the normal colors.
                            {
                                ContainerInfo.Children.Add(new HBoxContainer
                                {
                                    Children =
                                    {
                                        new Label {Text = $"{proto.Name}: "},
                                        new Label
                                        {
                                            Text = $"{reagent.Quantity}u",
                                            StyleClasses = {NanoStyle.StyleClassLabelSecondaryColor}
                                        }
                                    }
                                });
                            }
                        }
                        else //If you fail to get the reagents name, just call it "Unknown reagent".
                        {
                            ContainerInfo.Children.Add(new HBoxContainer
                            {
                                Children =
                                {
                                    new Label {Text = _localizationManager.GetString("Unknown reagent: ")},
                                    new Label
                                    {
                                        Text = $"{reagent.Quantity}u",
                                        StyleClasses = {NanoStyle.StyleClassLabelSecondaryColor}
                                    }
                                }
                            });
                        }
                    }
                }
            }
            else
            {
                ContainerInfo.Children.Add(new Label{Text = _localizationManager.GetString("No container loaded.")});
            }
            ForceRunLayoutUpdate(); //Force a layout update to avoid text hanging off the window until the user manually resizes it.
        }
    }
}
