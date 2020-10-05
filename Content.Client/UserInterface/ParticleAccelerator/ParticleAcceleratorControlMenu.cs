using System;
using Content.Client.Animations;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components;
using Robust.Client.Animations;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Noise;
using Robust.Shared.Timing;

namespace Content.Client.ParticleAccelerator
{
    public sealed class ParticleAcceleratorControlMenu : BaseWindow
    {
        private readonly ParticleAcceleratorBoundUserInterface Owner;

        private readonly Label _drawLabel;
        private readonly NoiseGenerator _drawNoiseGenerator;
        private readonly Button _onButton;
        private readonly Button _offButton;
        private readonly Label _statusLabel;
        private readonly SpinBox _stateSpinBox;

        private readonly VBoxContainer _alarmControl;
        private readonly Animation _alarmControlAnimation;

        private TextureRect _endCapTexture;
        private TextureRect _fuelChamberTexture;
        private TextureRect _controlBoxTexture;
        private TextureRect _powerBoxTexture;
        private TextureRect _emitterCenterTexture;
        private TextureRect _emitterRightTexture;
        private TextureRect _emitterLeftTexture;

        private readonly RSI _endCapRSI;
        private readonly RSI _fuelChamberRSI;
        private readonly RSI _controlBoxRSI;
        private readonly RSI _powerBoxRSI;
        private readonly RSI _emitterCenterRSI;
        private readonly RSI _emitterRightRSI;
        private readonly RSI _emitterLeftRSI;

        public ParticleAcceleratorControlMenu(ParticleAcceleratorBoundUserInterface owner)
        {
            Owner = owner;
            _drawNoiseGenerator = new NoiseGenerator(NoiseGenerator.NoiseType.Fbm);
            _drawNoiseGenerator.SetFrequency(0.5f);

            var resourceCache = IoCManager.Resolve<IResourceCache>();
            var font = resourceCache.GetFont("/Fonts/Boxfont-round/Boxfont Round.ttf", 13);
            var panelTex = resourceCache.GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");
            _endCapRSI = resourceCache.GetResource<RSIResource>("/Textures/Constructible/Power/PA/end_cap.rsi").RSI;
            _fuelChamberRSI = resourceCache.GetResource<RSIResource>("/Textures/Constructible/Power/PA/fuel_chamber.rsi").RSI;
            _controlBoxRSI = resourceCache.GetResource<RSIResource>("/Textures/Constructible/Power/PA/control_box.rsi").RSI;
            _powerBoxRSI = resourceCache.GetResource<RSIResource>("/Textures/Constructible/Power/PA/power_box.rsi").RSI;
            _emitterCenterRSI = resourceCache.GetResource<RSIResource>("/Textures/Constructible/Power/PA/emitter_center.rsi").RSI;
            _emitterRightRSI = resourceCache.GetResource<RSIResource>("/Textures/Constructible/Power/PA/emitter_right.rsi").RSI;
            _emitterLeftRSI = resourceCache.GetResource<RSIResource>("/Textures/Constructible/Power/PA/emitter_left.rsi").RSI;

            MouseFilter = MouseFilterMode.Stop;

            _alarmControlAnimation = new Animation
            {
                Length = TimeSpan.FromSeconds(1),
                AnimationTracks =
                {
                    new AnimationTrackControlProperty
                    {
                        Property = nameof(Control.Visible),
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(true, 0),
                            new AnimationTrackProperty.KeyFrame(false, 0.75f),
                        }
                    }
                }
            };

            var back = new StyleBoxTexture
            {
                Texture = panelTex,
                Modulate = Color.FromHex("#25252A"),
            };
            back.SetPatchMargin(StyleBox.Margin.All, 10);

            var back2 = new StyleBoxTexture(back)
            {
                Modulate = Color.FromHex("#202023")
            };

            AddChild(new PanelContainer
            {
                PanelOverride = back,
                MouseFilter = MouseFilterMode.Pass
            });

            _stateSpinBox = new SpinBox
            {
                Value = 0,
            };
            _stateSpinBox.IsValid = (n) => (n >= 0 && n <= 4 && _previousAssembled==true && _previousEnabled==true);
            _stateSpinBox.InitDefaultButtons();
            _stateSpinBox.ValueChanged += PowerStateChanged;

            _offButton = new Button
            {
                ToggleMode = true,
                Text = "Off",
                StyleClasses = {StyleBase.ButtonOpenRight},
            };
            _offButton.OnPressed += args => owner.SendEnableMessage(false);

            _onButton = new Button
            {
                ToggleMode = true,
                Pressed = true,
                Text = "On",
                StyleClasses = {StyleBase.ButtonOpenLeft},
            };
            _onButton.OnPressed += args => owner.SendEnableMessage(true);

            var serviceManual = new Label
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                StyleClasses = {StyleBase.StyleClassLabelSubText},
                Text = "Refer to p.132 of service manual"
            };
            _drawLabel = new Label();
            AddChild(new VBoxContainer
            {
                Children =
                {
                    new MarginContainer
                    {
                        MarginLeftOverride = 2,
                        MarginTopOverride = 2,
                        Children =
                        {
                            new Label
                            {
                                Text = "Mark 2 Particle Accelerator",
                                FontOverride = font,
                                FontColorOverride = StyleNano.NanoGold,
                            }
                        }
                    },
                    new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat {BackgroundColor = StyleNano.NanoGold},
                        CustomMinimumSize = (0, 2),
                    },
                    new Control
                    {
                        CustomMinimumSize = (0, 4)
                    },

                    new HBoxContainer
                    {
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        Children =
                        {
                            new MarginContainer
                            {
                                MarginLeftOverride = 4,
                                Children =
                                {
                                    new VBoxContainer
                                    {
                                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                                        Children =
                                        {
                                            new HBoxContainer
                                            {
                                                Children =
                                                {
                                                    new Label
                                                    {
                                                        Text = "Power: ",
                                                        SizeFlagsHorizontal = SizeFlags.Expand
                                                    },
                                                    _offButton,
                                                    _onButton
                                                }
                                            },
                                            new HBoxContainer
                                            {
                                                Children =
                                                {
                                                    new Label
                                                    {
                                                        Text = "Strength: ",
                                                        SizeFlagsHorizontal = SizeFlags.Expand
                                                    },
                                                    _stateSpinBox
                                                }
                                            },
                                            new Control
                                            {
                                                CustomMinimumSize = (0, 10),
                                            },
                                            _drawLabel,
                                            new Control
                                            {
                                                SizeFlagsVertical = SizeFlags.Expand
                                            },
                                            (_alarmControl = new VBoxContainer
                                            {
                                                Children =
                                                {
                                                    new Label
                                                    {
                                                        Text = "PARTICLE STRENGTH\nOVERLOAD",
                                                        FontColorOverride = Color.Red,
                                                        Align = Label.AlignMode.Center
                                                    },
                                                    serviceManual
                                                }
                                            }),
                                        }
                                    }
                                }
                            },
                            new VBoxContainer
                            {
                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                Children =
                                {
                                    (_statusLabel = new Label
                                    {
                                        SizeFlagsHorizontal = SizeFlags.ShrinkCenter
                                    }),
                                    new Control
                                    {
                                        CustomMinimumSize = (0, 20)
                                    },
                                    new PanelContainer
                                    {
                                        SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                                        PanelOverride = back2,
                                        Children =
                                        {
                                            new GridContainer
                                            {
                                                Columns = 3,
                                                VSeparationOverride = 0,
                                                HSeparationOverride = 0,
                                                Children =
                                                {
                                                    new Control(),
                                                    (_endCapTexture = new TextureRect()),
                                                    new Control(),
                                                    (_controlBoxTexture = new TextureRect()),
                                                    (_fuelChamberTexture = new TextureRect()),
                                                    new Control(),
                                                    new Control(),
                                                    (_powerBoxTexture = new TextureRect()),
                                                    new Control(),
                                                    (_emitterLeftTexture = new TextureRect()),
                                                    (_emitterCenterTexture = new TextureRect()),
                                                    (_emitterRightTexture = new TextureRect()),
                                                }
                                            }
                                        }
                                    },
                                }
                            }
                        }
                    },
                    new StripeBack
                    {
                        Children =
                        {
                            new MarginContainer
                            {
                                MarginLeftOverride = 4,
                                MarginTopOverride = 4,
                                MarginBottomOverride = 4,
                                Children =
                                {
                                    new Label
                                    {
                                        Text = "Ensure containment field is active before operation",
                                        SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                                        StyleClasses = {StyleBase.StyleClassLabelSubText},
                                    }
                                }
                            }
                        }
                    },
                    new MarginContainer
                    {
                        MarginLeftOverride = 12,
                        Children =
                        {
                            new HBoxContainer
                            {
                                Children =
                                {
                                    new Label
                                    {
                                        Text = "FOO-BAR-BAZ",
                                        StyleClasses = {StyleBase.StyleClassLabelSubText}
                                    }
                                }
                            }
                        }
                    },
                }
            });

            _alarmControl.AnimationCompleted += s =>
            {
                if(shouldAnimate)
                {
                    _alarmControl.PlayAnimation(_alarmControlAnimation, "warningAnim");
                }
                else
                {
                    _alarmControl.Visible = false;
                }
            };

        }

        private void PowerStateChanged(object sender, ValueChangedEventArgs e)
        {
            ParticleAcceleratorPowerState newState;
            switch (e.Value)
            {
                case 0:
                    newState = ParticleAcceleratorPowerState.Standby;
                    break;
                case 1:
                    newState = ParticleAcceleratorPowerState.Level0;
                    break;
                case 2:
                    newState = ParticleAcceleratorPowerState.Level1;
                    break;
                case 3:
                    newState = ParticleAcceleratorPowerState.Level2;
                    break;
                case 4:
                    newState = ParticleAcceleratorPowerState.Level3;
                    break;
                default:
                    return;
            }
            Owner.SendPowerStateMessage(newState);
        }

        protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
        {
            return DragMode.Move;
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return (400, 300);
        }

        public void DataUpdate(ParticleAcceleratorDataUpdateMessage dataUpdateMessage)
        {
            if (UpdatePowerState(dataUpdateMessage.State) | UpdateStatus(dataUpdateMessage.Assembled) | UpdateEnabled(dataUpdateMessage.Enabled))
            {
                UpdatePreview(dataUpdateMessage);
            }

            UpdatePowerDraw(dataUpdateMessage.PowerDraw);
        }

        private ParticleAcceleratorPowerState? _previousPowerstate;
        private bool shouldAnimate;
        private bool UpdatePowerState(ParticleAcceleratorPowerState state)
        {
            if (_previousPowerstate == state) return false;

            _previousPowerstate = state;

            _stateSpinBox.OverrideValue(state switch
            {
                ParticleAcceleratorPowerState.Standby => 0,
                ParticleAcceleratorPowerState.Level0 => 1,
                ParticleAcceleratorPowerState.Level1 => 2,
                ParticleAcceleratorPowerState.Level2 => 3,
                ParticleAcceleratorPowerState.Level3 => 4,
                _ => 0
            });

            shouldAnimate = false;
            _alarmControl.StopAnimation("warningAnim");
            _alarmControl.Visible = false;
            if (state == ParticleAcceleratorPowerState.Level3)
            {
                shouldAnimate = true;

                _alarmControl.PlayAnimation(_alarmControlAnimation, "warningAnim");
            }

            return true;
        }

        private bool? _previousEnabled;
        private bool UpdateEnabled(bool enabled)
        {
            if (_previousEnabled == enabled) return false;

            _previousEnabled = enabled;
            _onButton.Pressed = enabled;
            _offButton.Pressed = !enabled;
            return true;
        }

        private bool? _previousAssembled;
        private bool UpdateStatus(bool assembled)
        {
            if (_previousAssembled == assembled) return false;

            _previousAssembled = assembled;
            _statusLabel.Text = $"Status: {(assembled ? "Operational" : "Incomplete")}";
            return true;
        }

        private void UpdatePowerDraw(int powerDraw)
        {
            _lastWatts = powerDraw;
        }

        private void UpdatePreview(ParticleAcceleratorDataUpdateMessage updateMessage)
        {
            SetTexture(ref _endCapTexture, "end_cap", updateMessage.EndCapExists, updateMessage.State, updateMessage.Enabled, _endCapRSI);
            SetTexture(ref _fuelChamberTexture, "fuel_chamber", updateMessage.FuelChamberExists, updateMessage.State, updateMessage.Enabled, _fuelChamberRSI);
            SetTexture(ref _controlBoxTexture, "control_box", true, updateMessage.State, updateMessage.Enabled, _controlBoxRSI);
            SetTexture(ref _powerBoxTexture, "power_box", updateMessage.PowerBoxExists, updateMessage.State, updateMessage.Enabled, _powerBoxRSI);
            SetTexture(ref _emitterCenterTexture, "emitter_center", updateMessage.EmitterCenterExists, updateMessage.State, updateMessage.Enabled, _emitterCenterRSI);
            SetTexture(ref _emitterLeftTexture, "emitter_left", updateMessage.EmitterLeftExists, updateMessage.State, updateMessage.Enabled, _emitterLeftRSI);
            SetTexture(ref _emitterRightTexture, "emitter_right", updateMessage.EmitterRightExists, updateMessage.State, updateMessage.Enabled, _emitterRightRSI);
        }

        private void SetTexture(ref TextureRect rect, string baseState, bool exists, ParticleAcceleratorPowerState state, bool enabled, RSI rsi)
        {
            var suffix = "c";
            if(enabled)
            {
                suffix = state switch
                {
                    ParticleAcceleratorPowerState.Standby => "p",
                    ParticleAcceleratorPowerState.Level0 => "p0",
                    ParticleAcceleratorPowerState.Level1 => "p1",
                    ParticleAcceleratorPowerState.Level2 => "p2",
                    ParticleAcceleratorPowerState.Level3 => "p3",
                    _ => "p"
                };
            }

            rect.Texture = exists ? rsi[baseState + suffix].Frame0 : null;
        }

        private float _time;
        private float _lastWatts;
        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            _time += args.DeltaSeconds;

            var val = _drawNoiseGenerator.GetNoise(_time);
            var watts = _lastWatts + val * 5;

            _drawLabel.Text = $"Draw: {(int) watts:00,000} W";
        }
    }
}
