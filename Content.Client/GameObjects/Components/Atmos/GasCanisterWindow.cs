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
    /// Client-side UI used to control a <see cref="SharedDisposalUnitComponent"/>
    /// </summary>
    public class GasCanisterWindow : SS14Window
    {
        private readonly Label _pressure;
        private readonly Label _releasePressure;

        public readonly LineEdit LabelInput;
        public readonly Button EditLabelBtn;
        public string OldLabel = "";

        public readonly string EditLabelBtnStateEdit = Loc.GetString("Edit");
        public readonly string EditLabelBtnStateSubmit = Loc.GetString("OK");

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
                                            new Label(){ Text = "Label" },
                                            (LabelInput = new LineEdit() { Text = Name, Editable = false,
                                                CustomMinimumSize = new Vector2(200, 30)}),
                                            (EditLabelBtn = new Button() { Text = "" }),
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
                                        new VBoxContainer()
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
                                new VBoxContainer()
                                {
                                    Children =
                                    {
                                        new Label {Text = "Valve"}
                                    }
                                }
                            }
                        },
                }
            });

            // Create the release pressure buttons list
            ReleasePressureButtons = new List<ReleasePressureButton>();
            foreach (var control in releasePressureButtons.Children.ToList())
            {
                var btn = (ReleasePressureButton) control;
                ReleasePressureButtons.Add(btn);
            }


        }

        /// <summary>
        /// Update the UI based on <see cref="GasCanisterBoundUserInterfaceState"/>
        /// </summary>
        /// <param name="state">The state the UI should reflect</param>
        public void UpdateState(GasCanisterBoundUserInterfaceState state)
        {
            _pressure.Text = state.Volume + "kPa";
            _releasePressure.Text = state.ReleasePressure + "kPa";

            // Update the canister label
            OldLabel = LabelInput.Text;
            LabelInput.Text = state.Label;
            Title = state.Label;

            // Reset the editable label
            LabelInput.Editable = false;
            EditLabelBtn.Text = EditLabelBtnStateEdit;

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
                string prefix = value.Equals(Math.Abs(value)) ? "+" : "-";
                Text = prefix + Math.Abs(value);
                _pressureChange = value;
            }
        }

        private float _pressureChange;

        public ReleasePressureButton() : base() {}
    }


}
