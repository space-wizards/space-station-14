using System;
using System.Linq;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry.ChemMaster;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Content.Shared.GameObjects.Components.Chemistry.ChemMaster.SharedChemMasterComponent;

namespace Content.Client.GameObjects.Components.Chemistry.ChemMaster
{
    /// <summary>
    /// Client-side UI used to control a <see cref="SharedChemMasterComponent"/>
    /// </summary>
    public class ChemMasterWindow : SS14Window
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        /// <summary>Contains info about the reagent container such as it's contents, if one is loaded into the dispenser.</summary>
        private readonly VBoxContainer ContainerInfo;

        private readonly VBoxContainer BufferInfo;

        private readonly VBoxContainer PackagingInfo;

        /// <summary>Ejects the reagent container from the dispenser.</summary>
        public Button EjectButton { get; }

        public Button BufferTransferButton { get; }
        public Button BufferDiscardButton { get; }

        public bool BufferModeTransfer = true;

        public event Action<BaseButton.ButtonEventArgs, ChemButton> OnChemButtonPressed;

        public HBoxContainer PillInfo { get; set; }
        public HBoxContainer BottleInfo { get; set; }
        public SpinBox PillAmount { get; set; }
        public SpinBox BottleAmount { get; set; }
        public Button CreatePills { get; }
        public Button CreateBottles { get; }

        protected override Vector2? CustomSize => (400, 200);

        /// <summary>
        /// Create and initialize the chem master UI client-side. Creates the basic layout,
        /// actual data isn't filled in until the server sends data about the chem master.
        /// </summary>
        public ChemMasterWindow()
        {
            IoCManager.InjectDependencies(this);

            Contents.AddChild(new VBoxContainer
            {
                Children =
                {
                    //Container
                    new HBoxContainer
                    {
                        Children =
                        {
                            new Label {Text = Loc.GetString("Container")},
                            new Control {SizeFlagsHorizontal = SizeFlags.FillExpand},
                            (EjectButton = new Button {Text = Loc.GetString("Eject")})
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
                                        Text = Loc.GetString("No container loaded.")
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
                            new Label {Text = Loc.GetString("Buffer")},
                            new Control {SizeFlagsHorizontal = SizeFlags.FillExpand},
                            (BufferTransferButton = new Button {Text = Loc.GetString("Transfer"), Pressed = BufferModeTransfer, StyleClasses = { StyleBase.ButtonOpenRight }}),
                            (BufferDiscardButton = new Button {Text = Loc.GetString("Discard"), Pressed = !BufferModeTransfer, StyleClasses = { StyleBase.ButtonOpenLeft }})
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
                                        Text = Loc.GetString("Buffer empty.")
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
                            new Label {Text = Loc.GetString("Packaging ")},
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
                            }),

                        }
                    },
                }
            });

            //Pills
            PillInfo = new HBoxContainer
            {
                Children =
                {
                    new Label
                    {
                        Text = Loc.GetString("Pills:")
                    },

                },

            };
            PackagingInfo.AddChild(PillInfo);

            var pillPadding = new Control {SizeFlagsHorizontal = SizeFlags.FillExpand};
            PillInfo.AddChild(pillPadding);

            PillAmount = new SpinBox
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                Value = 1
            };
            PillAmount.InitDefaultButtons();
            PillAmount.IsValid = (n) => (n > 0 && n <= 10);
            PillInfo.AddChild(PillAmount);

            var pillVolume = new Label
            {
                Text = " max 50u/each ",
                StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
            };
            PillInfo.AddChild((pillVolume));

            CreatePills = new Button {Text = Loc.GetString("Create")};
            PillInfo.AddChild(CreatePills);

            //Bottles
            BottleInfo = new HBoxContainer
            {
                Children =
                {
                    new Label
                    {
                        Text = Loc.GetString("Bottles:")
                    },

                },

            };
            PackagingInfo.AddChild(BottleInfo);

            var bottlePadding = new Control {SizeFlagsHorizontal = SizeFlags.FillExpand};
            BottleInfo.AddChild(bottlePadding);

            BottleAmount = new SpinBox
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                Value = 1
            };
            BottleAmount.InitDefaultButtons();
            BottleAmount.IsValid = (n) => (n > 0 && n <= 10);
            BottleInfo.AddChild(BottleAmount);

            var bottleVolume = new Label
            {
                Text = " max 30u/each ",
                StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
            };
            BottleInfo.AddChild((bottleVolume));

            CreateBottles = new Button {Text = Loc.GetString("Create")};
            BottleInfo.AddChild(CreateBottles);
        }

        private ChemButton MakeChemButton(string text, ReagentUnit amount, string id, bool isBuffer, string styleClass)
        {
            var button = new ChemButton(text, amount, id, isBuffer, styleClass);
            button.OnPressed += args
                => OnChemButtonPressed?.Invoke(args, button);
            return button;
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
            if (Contents.Children != null)
            {
                SetButtonDisabledRecursive(Contents, !castState.HasPower);
                EjectButton.Disabled = !castState.HasBeaker;
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
        /// Update the container, buffer, and packaging panels.
        /// </summary>
        /// <param name="state">State data for the dispenser.</param>
        private void UpdatePanelInfo(ChemMasterBoundUserInterfaceState state)
        {
            BufferModeTransfer = state.BufferModeTransfer;
            BufferTransferButton.Pressed = BufferModeTransfer;
            BufferDiscardButton.Pressed = !BufferModeTransfer;

            ContainerInfo.Children.Clear();

            if (!state.HasBeaker)
            {
                ContainerInfo.Children.Add(new Label {Text = Loc.GetString("No container loaded.")});
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
                var name = Loc.GetString("Unknown reagent");
                //Try to the prototype for the given reagent. This gives us it's name.
                if (_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype proto))
                {
                    name = proto.Name;
                }

                if (proto != null)
                {
                    ContainerInfo.Children.Add(new HBoxContainer
                    {
                        Children =
                        {
                            new Label {Text = $"{name}: "},
                            new Label
                            {
                                Text = $"{reagent.Quantity}u",
                                StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
                            },

                            //Padding
                            new Control {SizeFlagsHorizontal = SizeFlags.FillExpand},

                            MakeChemButton("1", ReagentUnit.New(1), reagent.ReagentId, false, StyleBase.ButtonOpenRight),
                            MakeChemButton("5", ReagentUnit.New(5), reagent.ReagentId, false, StyleBase.ButtonOpenBoth),
                            MakeChemButton("10", ReagentUnit.New(10), reagent.ReagentId, false, StyleBase.ButtonOpenBoth),
                            MakeChemButton("25", ReagentUnit.New(25), reagent.ReagentId, false, StyleBase.ButtonOpenBoth),
                            MakeChemButton("All", ReagentUnit.New(-1), reagent.ReagentId, false, StyleBase.ButtonOpenLeft),
                        }
                    });
                }
            }

            BufferInfo.Children.Clear();

            if (!state.BufferReagents.Any())
            {
                BufferInfo.Children.Add(new Label {Text = Loc.GetString("Buffer empty.")});
                return;
            }

            var bufferHBox = new HBoxContainer();
            BufferInfo.AddChild(bufferHBox);

            var bufferLabel = new Label {Text = "buffer: "};
            bufferHBox.AddChild(bufferLabel);
            var bufferVol = new Label
            {
                Text = $"{state.BufferCurrentVolume}/{state.BufferMaxVolume}",
                StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
            };
            bufferHBox.AddChild(bufferVol);

            foreach (var reagent in state.BufferReagents)
            {
                var name = Loc.GetString("Unknown reagent");
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
                            new Control {SizeFlagsHorizontal = SizeFlags.FillExpand},

                            MakeChemButton("1", ReagentUnit.New(1), reagent.ReagentId, true, StyleBase.ButtonOpenRight),
                            MakeChemButton("5", ReagentUnit.New(5), reagent.ReagentId, true, StyleBase.ButtonOpenBoth),
                            MakeChemButton("10", ReagentUnit.New(10), reagent.ReagentId, true, StyleBase.ButtonOpenBoth),
                            MakeChemButton("25", ReagentUnit.New(25), reagent.ReagentId, true, StyleBase.ButtonOpenBoth),
                            MakeChemButton("All", ReagentUnit.New(-1), reagent.ReagentId, true, StyleBase.ButtonOpenLeft),
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
        public ChemButton(string _text, ReagentUnit _amount, string _id, bool _isBuffer, string _styleClass)
        {
            AddStyleClass(_styleClass);
            Text = _text;
            Amount = _amount;
            Id = _id;
            isBuffer = _isBuffer;
        }

    }
}
