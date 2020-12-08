using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Disposal;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Content.Client.GameObjects.Components.Atmos;
using Content.Shared.GameObjects.Components.Atmos;

namespace Content.Client.GameObjects.Components.Atmos
{
    /// <summary>
    /// Client-side UI used to control a <see cref="SharedGasCanisterComponent"/>
    /// </summary>
    public class GasCanisterWindow : SS14Window
    {
        private readonly Label _pressure;
        private readonly Label _releasePressure;

        public readonly CheckButton ToggleValve;
        public readonly LineEdit LabelInput;
        public readonly Button EditLabelBtn;
        public string OldLabel { get; set; } = "";

        public bool LabelInputEditable {
            get => LabelInput.Editable;
            set {
                LabelInput.Editable = value;
                EditLabelBtn.Text = value ? Loc.GetString("OK") : Loc.GetString("Edit");
            }
        }

        public List<ReleasePressureButton> ReleasePressureButtons { get; private set; }

        protected override Vector2? CustomSize => (300, 200);

        public GasCanisterWindow()
        {
            HBoxContainer releasePressureButtons;

            Contents.AddChild(new VBoxContainer
            {
                Children =
                {
                    new VBoxContainer
                        {
                            Children =
                            {
                                new HBoxContainer()
                                    {
                                        Children =
                                        {
                                            new Label(){ Text = Loc.GetString("Label") },
                                            (LabelInput = new LineEdit() { Text = Name, Editable = false,
                                                CustomMinimumSize = new Vector2(200, 30)}),
                                            (EditLabelBtn = new Button()),
                                        }
                                    },
                                new HBoxContainer
                                    {
                                        Children =
                                        {
                                            new Label {Text = Loc.GetString("Pressure:")},
                                            (_pressure = new Label())
                                        }
                                    },
                                new VBoxContainer()
                                {
                                    Children =
                                    {
                                        new HBoxContainer()
                                        {
                                            Children =
                                            {
                                                new Label() {Text = Loc.GetString("Release pressure:")},
                                                (_releasePressure = new Label())
                                            }
                                        },
                                        (releasePressureButtons = new HBoxContainer()
                                        {
                                            Children =
                                            {
                                                new ReleasePressureButton() {PressureChange = -50},
                                                new ReleasePressureButton() {PressureChange = -10},
                                                new ReleasePressureButton() {PressureChange = -1},
                                                new ReleasePressureButton() {PressureChange = -0.1f},
                                                new ReleasePressureButton() {PressureChange = 0.1f},
                                                new ReleasePressureButton() {PressureChange = 1},
                                                new ReleasePressureButton() {PressureChange = 10},
                                                new ReleasePressureButton() {PressureChange = 50}
                                            }
                                        })
                                    }
                                },
                                new HBoxContainer()
                                {
                                    Children =
                                    {
                                        new Label { Text = Loc.GetString("Valve") },
                                        (ToggleValve = new CheckButton() { Text = Loc.GetString("Open") })
                                    }
                                }
                            },
                        }
                }
            });

            // Create the release pressure buttons list
            ReleasePressureButtons = new List<ReleasePressureButton>();
            foreach (var control in releasePressureButtons.Children.ToList())
            {
                var btn = (ReleasePressureButton) control;
                ReleasePressureButtons.Add(btn);
            }

            // Reset the editable label
            LabelInputEditable = false;
        }


        /// <summary>
        /// Update the UI based on <see cref="GasCanisterBoundUserInterfaceState"/>
        /// </summary>
        /// <param name="state">The state the UI should reflect</param>
        public void UpdateState(GasCanisterBoundUserInterfaceState state)
        {
            _pressure.Text = Loc.GetString("{0}kPa", state.Volume);
            _releasePressure.Text = Loc.GetString("{0}kPa", state.ReleasePressure);

            // Update the canister label
            OldLabel = LabelInput.Text;
            LabelInput.Text = state.Label;
            Title = state.Label;

            // Reset the editable label
            LabelInputEditable = false;

            ToggleValve.Pressed = state.ValveOpened;
        }
    }


    /// <summary>
    /// Special button class which stores a numerical value and has it as a label
    /// </summary>
    public class ReleasePressureButton : Button
    {
        public float PressureChange
        {
            get { return _pressureChange; }
            set
            {
                _pressureChange = value;
                Text = (value >= 0) ? ("+" + value) : value.ToString();
            }
        }

        private float _pressureChange;

        public ReleasePressureButton() : base() {}
    }
}
