using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Client.UserInterface;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Content.Shared.GameObjects.Components.Chemistry.SharedReagentDispenserComponent;

namespace Content.Client.GameObjects.Components.Chemistry
{
    public class ReagentDispenserWindow : SS14Window
    {
        public Button DispenseButton1;
        public Button DispenseButton5;
        public Button DispenseButton10;
        public Button DispenseButton25;
        public Button DispenseButton50;
        public Button DispenseButton100;

        public VBoxContainer ContainerInfo;

        public Button ClearButton;
        public Button EjectButton;
        
        public GridContainer ChemicalList;

        [Dependency] private readonly IPrototypeManager _prototypeManager;

        public ReagentDispenserWindow()
        {
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            Contents.AddChild(new VBoxContainer
            {
                Children =
                {
                    new HBoxContainer
                    {
                        Children =
                        {
                            new Label{Text = "Amount    "},
                            (DispenseButton1 = new Button{Text = "1"}),
                            (DispenseButton5 = new Button{Text = "5"}),
                            (DispenseButton10 = new Button{Text = "10"}),
                            (DispenseButton25 = new Button{Text = "25"}),
                            (DispenseButton50 = new Button{Text = "50"}),
                            (DispenseButton100 = new Button{Text = "100"}),
                        }
                    },
                    new Panel{CustomMinimumSize = (0.0f, 10.0f)}, //Padding
                    (ChemicalList = new GridContainer
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
                            new Label{Text = "Container: "},
                            (ClearButton = new Button{Text = "Clear"}),
                            (EjectButton = new Button{Text = "Eject"})
                        }
                    },
                    new PanelContainer
                    {
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        SizeFlagsStretchRatio = 6,
                        PanelOverride = new StyleBoxFlat
                        {
                            BackgroundColor = new Color(27, 27, 30)
                        },
                        Children =
                        {
                            (ContainerInfo = new VBoxContainer
                            {
                                MarginLeft = 5.0f,
                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                Children =
                                {
                                    new Label{Text = "No container loaded."}
                                }
                            }),
                        }
                    },
                }
            });
        }

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
                    ChemicalList.AddChild(new Button { Text = "Reagent name not found" });
                }

            }
        }

        public void UpdateState(BoundUserInterfaceState state)
        {
            var castState = (ReagentDispenserBoundUserInterfaceState)state;
            Title = castState.DispenserName;
            UpdateContainerInfo(castState);
        }

        public void UpdateContainerInfo(ReagentDispenserBoundUserInterfaceState state, string highlightedReagentId = "InvalidReagent")
        {
            ContainerInfo.Children.Clear();
            if (state.HasBeaker)
            {
                ContainerInfo.Children.Add(new HBoxContainer
                {
                    Children =
                    {
                        new Label{Text = $"{state.ContainerName}: "},
                        new Label{Text = $"{state.BeakerCurrentVolume}/{state.BeakerMaxVolume}", StyleClasses = { NanoStyle.StyleClassLabelSecondaryColor }}
                    }
                });
                if (state.ContainerReagents != null)
                {
                    foreach (var reagent in state.ContainerReagents)
                    {
                        if (_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype proto))
                        {
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
                            else
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
                        else
                        {
                            ContainerInfo.Children.Add(new HBoxContainer
                            {
                                Children =
                                {
                                    new Label {Text = "Unknown reagent: "},
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
                ContainerInfo.Children.Add(new Label{Text = "No container loaded."});
            }
            ForceRunLayoutUpdate();
        }
    }
}
