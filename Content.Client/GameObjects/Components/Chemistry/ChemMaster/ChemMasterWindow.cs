using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Content.Shared.GameObjects.Components.Chemistry.SharedChemMasterComponent;

namespace Content.Client.GameObjects.Components.Chemistry
{
    /// <summary>
    /// Client-side UI used to control a <see cref="SharedChemMasterComponent"/>
    /// </summary>
    public class ChemMasterWindow : SS14Window
    {
        /// <summary>Contains info about the reagent container such as it's contents, if one is loaded into the dispenser.</summary>
        private readonly VBoxContainer ContainerInfo;

        private readonly VBoxContainer BufferInfo;

        private readonly VBoxContainer PackagingInfo;

        public Button DispenseButton1 { get; }

        public Button DispenseButton5 { get; }

        public Button DispenseButton10 { get; }

        public Button DispenseButton25 { get; }

        public Button TransferButtonAll { get; }

        /// <summary>Ejects the reagent container from the dispenser.</summary>
        public Button EjectButton { get; }

        public Button BufferTransferButton { get; }
        public Button BufferDiscardButton { get; }

        public bool BufferModeTransfer = true;

        public event Action<BaseButton.ButtonEventArgs, ChemButton> OnChemButtonPressed;

#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649

        protected override Vector2? CustomSize => (400, 200);

        /// <summary>
        /// Create and initialize the dispenser UI client-side. Creates the basic layout,
        /// actual data isn't filled in until the server sends data about the dispenser.
        /// </summary>
        public ChemMasterWindow()
        {
            IoCManager.InjectDependencies(this);

            //var dispenseAmountGroup = new ButtonGroup();

            Contents.AddChild(new VBoxContainer
            {
                Children =
                {
                    //Container
                    new HBoxContainer
                    {
                        Children =
                        {
                            new Label {Text = _localizationManager.GetString("Container: ")},
                            (EjectButton = new Button {Text = _localizationManager.GetString("Eject")})
                        }
                    },
                    //Wrap the container info in a PanelContainer so we can color it's background differently.
                    new PanelContainer
                    {
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        SizeFlagsStretchRatio = 6,
                        CustomMinimumSize = (0, 200),
                        PanelOverride = new StyleBoxFlat
                        {
                            BackgroundColor = new Color(27, 27, 30)
                        },
                        Children =
                        {
                            //Currently empty, when server sends state data this will have container contents and fill volume.
                            (ContainerInfo = new VBoxContainer
                            {
                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                Children =
                                {
                                    new Label
                                    {
                                        Text = _localizationManager.GetString("No container loaded.")
                                    }
                                }
                            }),
                        }
                    },

                    //Padding
                    new Control {CustomMinimumSize = (0.0f, 10.0f)},

                    //Buffer
                    new HBoxContainer
                    {
                        Children =
                        {
                            new Label {Text = _localizationManager.GetString("Buffer Mode: ")},
                            (BufferTransferButton = new Button {Text = _localizationManager.GetString("Transfer"), Pressed = BufferModeTransfer}),
                            (BufferDiscardButton = new Button {Text = _localizationManager.GetString("Discard"), Pressed = !BufferModeTransfer})
                        }
                    },

                    //Wrap the buffer info in a PanelContainer so we can color it's background differently.
                    new PanelContainer
                    {
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        SizeFlagsStretchRatio = 6,
                        CustomMinimumSize = (0, 100),
                        PanelOverride = new StyleBoxFlat
                        {
                            BackgroundColor = new Color(27, 27, 30)
                        },
                        Children =
                        {
                            //Buffer reagent list
                            (BufferInfo = new VBoxContainer
                            {
                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                Children =
                                {
                                    new Label
                                    {
                                        Text = _localizationManager.GetString("Buffer empty.")
                                    }
                                }
                            }),
                        }
                    },

                    //Padding
                    new Control {CustomMinimumSize = (0.0f, 10.0f)},

                    //Packaging
                    new HBoxContainer
                    {
                        Children =
                        {
                            new Label {Text = _localizationManager.GetString("Packaging ")},
                        }
                    },

                    //Wrap the packaging info in a PanelContainer so we can color it's background differently.
                    new PanelContainer
                    {
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        SizeFlagsStretchRatio = 6,
                        CustomMinimumSize = (0, 100),
                        PanelOverride = new StyleBoxFlat
                        {
                            BackgroundColor = new Color(27, 27, 30)
                        },
                        Children =
                        {
                            //Packaging options
                            (PackagingInfo = new VBoxContainer
                            {
                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                Children =
                                {
                                    new Label
                                    {
                                        Text = _localizationManager.GetString("WIP.")
                                    }
                                }
                            }),
                        }
                    },
                }
            });
        }

        private ChemButton MakeChemButton(string text, ReagentUnit amount, string id, bool isBuffer)
        {
            var button = new ChemButton(text, amount, id, isBuffer);
            button.OnPressed += args
                => OnChemButtonPressed?.Invoke(args, button);
            return button;
        }

        /// <summary>
        /// Update the button grid of reagents which can be dispensed.
        /// <para>The actions for these buttons are set in <see cref="ReagentDispenserBoundUserInterface.UpdateReagentsList"/>.</para>
        /// </summary>
        /// <param name="inventory">Reagents which can be dispensed by this dispenser</param>
        public void UpdateReagentsList(List</*ReagentDispenserInventoryEntry*/string> inventory)
        {
            //if (ChemicalList == null) return;
            if (inventory == null) return;

            //ChemicalList.Children.Clear();

            foreach (var entry in inventory)
            {
                /*if (_prototypeManager.TryIndex(entry.ID, out ReagentPrototype proto))
                {
                    ChemicalList.AddChild(new Button {Text = proto.Name});
                }
                else
                {
                    ChemicalList.AddChild(new Button {Text = _localizationManager.GetString("Reagent name not found")});
                }*/
            }
        }

        /// <summary>
        /// Update the UI state when new state data is received from the server.
        /// </summary>
        /// <param name="state">State data sent by the server.</param>
        public void UpdateState(BoundUserInterfaceState state)
        {
            var castState = (ChemMasterBoundUserInterfaceState) state;
            Title = castState.DispenserName;
            UpdatePanelInfo(castState);

            /*switch (castState.SelectedDispenseAmount.Int())
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
                case 25:
                    DispenseButton25.Pressed = true;
                    break;
                case 50:
                    DispenseButton50.Pressed = true;
                    break;
                case 100:
                    DispenseButton100.Pressed = true;
                    break;
            }*/
        }

        /// <summary>
        /// Update the fill state and list of reagents held by the current reagent container, if applicable.
        /// <para>Also highlights a reagent if it's dispense button is being mouse hovered.</para>
        /// </summary>
        /// <param name="state">State data for the dispenser.</param>
        /// <param name="highlightedReagentId">Prototype id of the reagent whose dispense button is currently being mouse hovered.</param>
        public void UpdatePanelInfo(ChemMasterBoundUserInterfaceState state,
            string highlightedReagentId = null)
        {
            ContainerInfo.Children.Clear();

            if (!state.HasBeaker)
            {
                ContainerInfo.Children.Add(new Label {Text = _localizationManager.GetString("No container loaded.")});
                return;
            }

            ContainerInfo.Children.Add(new HBoxContainer // Name of the container and its fill status (Ex: 44/100u)
            {
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

            foreach (var reagent in state.ContainerReagents)
            {
                var name = _localizationManager.GetString("Unknown reagent");
                //Try to the prototype for the given reagent. This gives us it's name.
                if (_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype proto))
                {
                    name = proto.Name;
                }

                if (proto != null)
                {
                    ContainerInfo.Children.Add(new HBoxContainer
                    {
                        //SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                        Children =
                        {
                            new Label {Text = $"{name}: "},
                            new Label
                            {
                                Text = $"{reagent.Quantity}u",
                                StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
                            },
                            //Padding
                            //new Control {CustomMinimumSize = (20, 0)},
                            new Control {SizeFlagsHorizontal = SizeFlags.FillExpand},

                            /*(new ChemButton
                            {
                                Text = "1", Amount = ReagentUnit.New(1), Id = reagent.ReagentId, isBuffer = false,
                                OnPressed += args => OnChemButtonPressed?.Invoke(ChemButton.All),
                            }),
                            (new ChemButton {Text = "5", Amount = ReagentUnit.New(5), Id = reagent.ReagentId, isBuffer = false}),
                            (new ChemButton {Text = "10", Amount = ReagentUnit.New(10), Id = reagent.ReagentId, isBuffer = false}),
                            (new ChemButton {Text = "25", Amount = ReagentUnit.New(25), Id = reagent.ReagentId, isBuffer = false}),
                            (new ChemButton {Text = "All", Amount = ReagentUnit.New(-1), Id = reagent.ReagentId, isBuffer = false}),*/

                            MakeChemButton("1", ReagentUnit.New(1), reagent.ReagentId, false),
                            MakeChemButton("5", ReagentUnit.New(5), reagent.ReagentId, false),
                            MakeChemButton("10", ReagentUnit.New(10), reagent.ReagentId, false),
                            MakeChemButton("25", ReagentUnit.New(25), reagent.ReagentId, false),
                            MakeChemButton("All", ReagentUnit.New(-1), reagent.ReagentId, false),
                        }
                    });
                }
            }

            BufferInfo.Children.Clear();

            if (!state.BufferReagents.Any())
            {
                BufferInfo.Children.Add(new Label {Text = _localizationManager.GetString("Buffer empty.")});
                return;
            }

            BufferInfo.Children.Add(new HBoxContainer());

            foreach (var reagent in state.BufferReagents)
            {
                var name = _localizationManager.GetString("Unknown reagent");
                //Try to the prototype for the given reagent. This gives us it's name.
                if (_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype proto))
                {
                    name = proto.Name;
                }

                if (proto != null)
                {
                    BufferInfo.Children.Add(new HBoxContainer
                    {
                        //SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                        Children =
                        {
                            new Label {Text = $"{name}: "},
                            new Label
                            {
                                Text = $"{reagent.Quantity}u",
                                StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
                            },
                            //Padding
                            //new Control {CustomMinimumSize = (20, 0)},
                            new Control {SizeFlagsHorizontal = SizeFlags.FillExpand},

                            /*(new ChemButton
                            {
                                Text = "1", Amount = ReagentUnit.New(1), Id = reagent.ReagentId, isBuffer = false,
                                OnPressed += args => OnChemButtonPressed?.Invoke(ChemButton.All),
                            }),
                            (new ChemButton {Text = "5", Amount = ReagentUnit.New(5), Id = reagent.ReagentId, isBuffer = false}),
                            (new ChemButton {Text = "10", Amount = ReagentUnit.New(10), Id = reagent.ReagentId, isBuffer = false}),
                            (new ChemButton {Text = "25", Amount = ReagentUnit.New(25), Id = reagent.ReagentId, isBuffer = false}),
                            (new ChemButton {Text = "All", Amount = ReagentUnit.New(-1), Id = reagent.ReagentId, isBuffer = false}),*/

                            MakeChemButton("1", ReagentUnit.New(1), reagent.ReagentId, true),
                            MakeChemButton("5", ReagentUnit.New(5), reagent.ReagentId, true),
                            MakeChemButton("10", ReagentUnit.New(10), reagent.ReagentId, true),
                            MakeChemButton("25", ReagentUnit.New(25), reagent.ReagentId, true),
                            MakeChemButton("All", ReagentUnit.New(-1), reagent.ReagentId, true),
                        }
                    });
                }
            }
        }
    }

    public class ChemButton : Button
    {
        public ReagentUnit Amount { get; set; }
        public bool isBuffer = true;
        public string Id { get; set; }
        public ChemButton(string _text, ReagentUnit _amount, string _id, bool _isBuffer)
        {
            AddStyleClass(StyleClassButton);
            Text = _text;
            Amount = _amount;
            Id = _id;
            isBuffer = _isBuffer;
        }

    }
}
