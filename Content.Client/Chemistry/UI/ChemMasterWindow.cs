using System;
using System.Linq;
using Content.Client.Stylesheets;
using Content.Shared.Chemistry.Components;
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
using static Content.Shared.Chemistry.Components.SharedChemMasterComponent;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.Chemistry.UI
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

        public event Action<ButtonEventArgs, ChemButton>? OnChemButtonPressed;

        public HBoxContainer PillInfo { get; set; }
        public HBoxContainer BottleInfo { get; set; }
        public SpinBox PillAmount { get; set; }
        public SpinBox BottleAmount { get; set; }
        public Button CreatePills { get; }
        public Button CreateBottles { get; }

        /// <summary>
        /// Create and initialize the chem master UI client-side. Creates the basic layout,
        /// actual data isn't filled in until the server sends data about the chem master.
        /// </summary>
        public ChemMasterWindow()
        {
            MinSize = SetSize = (400, 525);
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
                            new Label {Text = Loc.GetString("chem-master-window-container-label")},
                            new Control {HorizontalExpand = true},
                            (EjectButton = new Button {Text = Loc.GetString("chem-master-window-eject-button")})
                        }
                    },
                    //Wrap the container info in a PanelContainer so we can color it's background differently.
                    new PanelContainer
                    {
                        VerticalExpand = true,
                        SizeFlagsStretchRatio = 6,
                        MinSize = (0, 200),
                        PanelOverride = new StyleBoxFlat
                        {
                            BackgroundColor = new Color(27, 27, 30)
                        },
                        Children =
                        {
                            //Currently empty, when server sends state data this will have container contents and fill volume.
                            (ContainerInfo = new VBoxContainer
                            {
                                HorizontalExpand = true,
                                Children =
                                {
                                    new Label
                                    {
                                        Text = Loc.GetString("chem-master-window-no-container-loaded-text")
                                    }
                                }
                            }),
                        }
                    },

                    //Padding
                    new Control {MinSize = (0.0f, 10.0f)},

                    //Buffer
                    new HBoxContainer
                    {
                        Children =
                        {
                            new Label {Text = Loc.GetString("chem-master-window-buffer-text")},
                            new Control {HorizontalExpand = true},
                            (BufferTransferButton = new Button {Text = Loc.GetString("chem-master-window-transfer-button"), Pressed = BufferModeTransfer, StyleClasses = { StyleBase.ButtonOpenRight }}),
                            (BufferDiscardButton = new Button {Text = Loc.GetString("chem-master-window-discard-button"), Pressed = !BufferModeTransfer, StyleClasses = { StyleBase.ButtonOpenLeft }})
                        }
                    },

                    //Wrap the buffer info in a PanelContainer so we can color it's background differently.
                    new PanelContainer
                    {
                        VerticalExpand = true,
                        SizeFlagsStretchRatio = 6,
                        MinSize = (0, 100),
                        PanelOverride = new StyleBoxFlat
                        {
                            BackgroundColor = new Color(27, 27, 30)
                        },
                        Children =
                        {
                            //Buffer reagent list
                            (BufferInfo = new VBoxContainer
                            {
                                HorizontalExpand = true,
                                Children =
                                {
                                    new Label
                                    {
                                        Text = Loc.GetString("chem-master-window-buffer-empty-text")
                                    }
                                }
                            }),
                        }
                    },

                    //Padding
                    new Control {MinSize = (0.0f, 10.0f)},

                    //Packaging
                    new HBoxContainer
                    {
                        Children =
                        {
                            new Label {Text = $"{Loc.GetString("chem-master-window-packaging-text")} "},
                        }
                    },

                    //Wrap the packaging info in a PanelContainer so we can color it's background differently.
                    new PanelContainer
                    {
                        VerticalExpand = true,
                        SizeFlagsStretchRatio = 6,
                        MinSize = (0, 100),
                        PanelOverride = new StyleBoxFlat
                        {
                            BackgroundColor = new Color(27, 27, 30)
                        },
                        Children =
                        {
                            //Packaging options
                            (PackagingInfo = new VBoxContainer
                            {
                                HorizontalExpand = true,
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
                        Text = $"{Loc.GetString("chem-master-window-pills-label")} "
                    },

                },

            };
            PackagingInfo.AddChild(PillInfo);

            var pillPadding = new Control {HorizontalExpand = true};
            PillInfo.AddChild(pillPadding);

            PillAmount = new SpinBox
            {
                HorizontalExpand = true,
                Value = 1
            };
            PillAmount.InitDefaultButtons();
            PillAmount.IsValid = (n) => (n > 0 && n <= 10);
            PillInfo.AddChild(PillAmount);

            var pillVolume = new Label
            {
                Text =  $" {Loc.GetString("chem-master-window-max-pills-volume-text")} ",
                StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
            };
            PillInfo.AddChild((pillVolume));

            CreatePills = new Button {Text = Loc.GetString("chem-master-window-create-pill-button") };
            PillInfo.AddChild(CreatePills);

            //Bottles
            BottleInfo = new HBoxContainer
            {
                Children =
                {
                    new Label
                    {
                        Text = Loc.GetString("cham-master-window-bottles-label")
                    },

                },

            };
            PackagingInfo.AddChild(BottleInfo);

            var bottlePadding = new Control {HorizontalExpand = true};
            BottleInfo.AddChild(bottlePadding);

            BottleAmount = new SpinBox
            {
                HorizontalExpand = true,
                Value = 1
            };
            BottleAmount.InitDefaultButtons();
            BottleAmount.IsValid = (n) => (n > 0 && n <= 10);
            BottleInfo.AddChild(BottleAmount);

            var bottleVolume = new Label
            {
                Text = $" {Loc.GetString("chem-master-window-max-bottle-volume-text")} ",
                StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
            };
            BottleInfo.AddChild((bottleVolume));

            CreateBottles = new Button {Text = Loc.GetString("chem-master-window-create-bottle-button") };
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
                ContainerInfo.Children.Add(new Label {Text = Loc.GetString("chem-master-window-no-container-loaded-text") });
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
                var name = Loc.GetString("chem-master-window-unknown-reagent-text");
                //Try to the prototype for the given reagent. This gives us it's name.
                if (_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype? proto))
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
                            new Control {HorizontalExpand = true},

                            MakeChemButton("1", ReagentUnit.New(1), reagent.ReagentId, false, StyleBase.ButtonOpenRight),
                            MakeChemButton("5", ReagentUnit.New(5), reagent.ReagentId, false, StyleBase.ButtonOpenBoth),
                            MakeChemButton("10", ReagentUnit.New(10), reagent.ReagentId, false, StyleBase.ButtonOpenBoth),
                            MakeChemButton("25", ReagentUnit.New(25), reagent.ReagentId, false, StyleBase.ButtonOpenBoth),
                            MakeChemButton(Loc.GetString("chem-master-window-buffer-all-amount"), ReagentUnit.New(-1), reagent.ReagentId, false, StyleBase.ButtonOpenLeft),
                        }
                    });
                }
            }

            BufferInfo.Children.Clear();

            if (!state.BufferReagents.Any())
            {
                BufferInfo.Children.Add(new Label {Text = Loc.GetString("chem-master-window-buffer-empty-text") });
                return;
            }

            var bufferHBox = new HBoxContainer();
            BufferInfo.AddChild(bufferHBox);

            var bufferLabel = new Label { Text = $"{Loc.GetString("chem-master-window-buffer-label")} " };
            bufferHBox.AddChild(bufferLabel);
            var bufferVol = new Label
            {
                Text = $"{state.BufferCurrentVolume}",
                StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
            };
            bufferHBox.AddChild(bufferVol);

            foreach (var reagent in state.BufferReagents)
            {
                var name = Loc.GetString("chem-master-window-unknown-reagent-text");
                //Try to the prototype for the given reagent. This gives us it's name.
                if (_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype? proto))
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
                            new Control {HorizontalExpand = true},

                            MakeChemButton("1", ReagentUnit.New(1), reagent.ReagentId, true, StyleBase.ButtonOpenRight),
                            MakeChemButton("5", ReagentUnit.New(5), reagent.ReagentId, true, StyleBase.ButtonOpenBoth),
                            MakeChemButton("10", ReagentUnit.New(10), reagent.ReagentId, true, StyleBase.ButtonOpenBoth),
                            MakeChemButton("25", ReagentUnit.New(25), reagent.ReagentId, true, StyleBase.ButtonOpenBoth),
                            MakeChemButton(Loc.GetString("chem-master-window-buffer-all-amount"), ReagentUnit.New(-1), reagent.ReagentId, true, StyleBase.ButtonOpenLeft),
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
