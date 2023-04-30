using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Singularity.Components;
using Robust.Client.Animations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.ParticleAccelerator.UI
{
    public sealed class ParticleAcceleratorControlMenu : BaseWindow
    {
        private readonly ShaderInstance _greyScaleShader;

        private readonly ParticleAcceleratorBoundUserInterface Owner;

        private readonly Label _drawLabel;
        private readonly NoiseGenerator _drawNoiseGenerator;
        private readonly Button _onButton;
        private readonly Button _offButton;
        private readonly Button _scanButton;
        private readonly Label _statusLabel;
        private readonly SpinBox _stateSpinBox;

        private readonly BoxContainer _alarmControl;
        private readonly Animation _alarmControlAnimation;

        private readonly PASegmentControl _endCapTexture;
        private readonly PASegmentControl _fuelChamberTexture;
        private readonly PASegmentControl _controlBoxTexture;
        private readonly PASegmentControl _powerBoxTexture;
        private readonly PASegmentControl _emitterCenterTexture;
        private readonly PASegmentControl _emitterRightTexture;
        private readonly PASegmentControl _emitterLeftTexture;

        private float _time;
        private int _lastDraw;
        private int _lastReceive;

        private bool _blockSpinBox;
        private bool _assembled;
        private bool _shouldContinueAnimating;

        public ParticleAcceleratorControlMenu(ParticleAcceleratorBoundUserInterface owner)
        {
            SetSize = (400, 320);
            _greyScaleShader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("Greyscale").Instance();

            Owner = owner;
            _drawNoiseGenerator = new NoiseGenerator(NoiseGenerator.NoiseType.Fbm);
            _drawNoiseGenerator.SetFrequency(0.5f);

            var resourceCache = IoCManager.Resolve<IResourceCache>();
            var font = resourceCache.GetFont("/Fonts/Boxfont-round/Boxfont Round.ttf", 13);
            var panelTex = resourceCache.GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");

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

            _stateSpinBox = new SpinBox {Value = 0, IsValid = StrengthSpinBoxValid,};
            _stateSpinBox.InitDefaultButtons();
            _stateSpinBox.ValueChanged += PowerStateChanged;
            _stateSpinBox.LineEditDisabled = true;

            _offButton = new Button
            {
                ToggleMode = false,
                Text = Loc.GetString("particle-accelerator-control-menu-off-button"),
                StyleClasses = {StyleBase.ButtonOpenRight},
            };
            _offButton.OnPressed += args => owner.SendEnableMessage(false);

            _onButton = new Button
            {
                ToggleMode = false,
                Text = Loc.GetString("particle-accelerator-control-menu-on-button"),
                StyleClasses = {StyleBase.ButtonOpenLeft},
            };
            _onButton.OnPressed += args => owner.SendEnableMessage(true);

            var closeButton = new TextureButton
            {
                StyleClasses = {"windowCloseButton"},
                HorizontalAlignment = HAlignment.Right,
                Margin = new Thickness(0, 0, 8, 0)
            };
            closeButton.OnPressed += args => Close();

            var serviceManual = new Label
            {
                HorizontalAlignment = HAlignment.Center,
                StyleClasses = {StyleBase.StyleClassLabelSubText},
                Text = Loc.GetString("particle-accelerator-control-menu-service-manual-reference")
            };
            _drawLabel = new Label();
            var imgSize = new Vector2(32, 32);
            AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children =
                {
                    new Control
                    {
                        Margin = new Thickness(2, 2, 0, 0),
                        Children =
                        {
                            new Label
                            {
                                Text = Loc.GetString("particle-accelerator-control-menu-device-version-label"),
                                FontOverride = font,
                                FontColorOverride = StyleNano.NanoGold,
                            },
                            closeButton
                        }
                    },
                    new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat {BackgroundColor = StyleNano.NanoGold},
                        MinSize = (0, 2),
                    },
                    new Control
                    {
                        MinSize = (0, 4)
                    },

                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        VerticalExpand = true,
                        Children =
                        {
                            new BoxContainer
                            {
                                Orientation = LayoutOrientation.Vertical,
                                Margin = new Thickness(4, 0, 0, 0),
                                HorizontalExpand = true,
                                Children =
                                {
                                    new BoxContainer
                                    {
                                        Orientation = LayoutOrientation.Horizontal,
                                        Children =
                                        {
                                            new Label
                                            {
                                                Text = Loc.GetString("particle-accelerator-control-menu-power-label") + " ",
                                                HorizontalExpand = true,
                                                HorizontalAlignment = HAlignment.Left,
                                            },
                                            _offButton,
                                            _onButton
                                        }
                                    },
                                    new BoxContainer
                                    {
                                        Orientation = LayoutOrientation.Horizontal,
                                        Children =
                                        {
                                            new Label
                                            {
                                                Text = Loc.GetString("particle-accelerator-control-menu-strength-label") + " ",
                                                HorizontalExpand = true,
                                                HorizontalAlignment = HAlignment.Left,
                                            },
                                            _stateSpinBox
                                        }
                                    },
                                    new Control
                                    {
                                        MinSize = (0, 10),
                                    },
                                    _drawLabel,
                                    new Control
                                    {
                                        VerticalExpand = true,
                                    },
                                    (_alarmControl = new BoxContainer
                                    {
                                        Orientation = LayoutOrientation.Vertical,
                                        Children =
                                        {
                                            new Label
                                            {
                                                Text = Loc.GetString("particle-accelerator-control-menu-alarm-control"),
                                                FontColorOverride = Color.Red,
                                                Align = Label.AlignMode.Center
                                            },
                                            serviceManual
                                        }
                                    }),
                                }
                            },
                            new BoxContainer
                            {
                                Orientation = LayoutOrientation.Vertical,
                                MinSize = (186, 0),
                                Children =
                                {
                                    (_statusLabel = new Label
                                    {
                                        HorizontalAlignment = HAlignment.Center
                                    }),
                                    new Control
                                    {
                                        MinSize = (0, 20)
                                    },
                                    new PanelContainer
                                    {
                                        HorizontalAlignment = HAlignment.Center,
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
                                                    new Control {MinSize = imgSize},
                                                    (_endCapTexture = Segment("end_cap")),
                                                    new Control {MinSize = imgSize},
                                                    (_controlBoxTexture = Segment("control_box")),
                                                    (_fuelChamberTexture = Segment("fuel_chamber")),
                                                    new Control {MinSize = imgSize},
                                                    new Control {MinSize = imgSize},
                                                    (_powerBoxTexture = Segment("power_box")),
                                                    new Control {MinSize = imgSize},
                                                    (_emitterLeftTexture = Segment("emitter_left")),
                                                    (_emitterCenterTexture = Segment("emitter_center")),
                                                    (_emitterRightTexture = Segment("emitter_right")),
                                                }
                                            }
                                        }
                                    },
                                    (_scanButton = new Button
                                    {
                                        Text = Loc.GetString("particle-accelerator-control-menu-scan-parts-button"),
                                        HorizontalAlignment = HAlignment.Center
                                    })
                                }
                            }
                        }
                    },
                    new StripeBack
                    {
                        Children =
                        {
                            new Label
                            {
                                Margin = new Thickness(4, 4, 0, 4),
                                Text = Loc.GetString("particle-accelerator-control-menu-check-containment-field-warning"),
                                HorizontalAlignment = HAlignment.Center,
                                StyleClasses = {StyleBase.StyleClassLabelSubText},
                            }
                        }
                    },
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        Margin = new Thickness(12, 0, 0, 0),
                        Children =
                        {
                            new Label
                            {
                                Text = Loc.GetString("particle-accelerator-control-menu-foo-bar-baz"),
                                StyleClasses = {StyleBase.StyleClassLabelSubText}
                            }
                        }
                    },
                }
            });

            _scanButton.OnPressed += args => Owner.SendScanPartsMessage();

            _alarmControl.AnimationCompleted += s =>
            {
                if (_shouldContinueAnimating)
                {
                    _alarmControl.PlayAnimation(_alarmControlAnimation, "warningAnim");
                }
                else
                {
                    _alarmControl.Visible = false;
                }
            };

            PASegmentControl Segment(string name)
            {
                return new(this, resourceCache, name);
            }

            UpdateUI(false, false, false, false);
        }

        private bool StrengthSpinBoxValid(int n)
        {
            return (n >= 0 && n <= 3 && !_blockSpinBox);
        }

        private void PowerStateChanged(ValueChangedEventArgs e)
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
                // They can't reach this level anyway and I just want to fix the bugginess for now.
                //case 4:
                //    newState = ParticleAcceleratorPowerState.Level3;
                //    break;
                default:
                    return;
            }

            _stateSpinBox.SetButtonDisabled(true);
            Owner.SendPowerStateMessage(newState);
        }

        protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
        {
            return DragMode.Move;
        }

        public void DataUpdate(ParticleAcceleratorUIState uiState)
        {
            _assembled = uiState.Assembled;
            UpdateUI(uiState.Assembled, uiState.InterfaceBlock, uiState.Enabled,
                uiState.WirePowerBlock);
            _statusLabel.Text = Loc.GetString("particle-accelerator-control-menu-status-label",
                                              ("status", Loc.GetString(uiState.Assembled ? "particle-accelerator-control-menu-status-operational" :
                                                                                           "particle-accelerator-control-menu-status-incomplete")));
            UpdatePowerState(uiState.State, uiState.Enabled, uiState.Assembled,
                uiState.MaxLevel);
            UpdatePreview(uiState);
            _lastDraw = uiState.PowerDraw;
            _lastReceive = uiState.PowerReceive;
        }

        private void UpdatePowerState(ParticleAcceleratorPowerState state, bool enabled, bool assembled,
            ParticleAcceleratorPowerState maxState)
        {
            _stateSpinBox.OverrideValue(state switch
            {
                ParticleAcceleratorPowerState.Standby => 0,
                ParticleAcceleratorPowerState.Level0 => 1,
                ParticleAcceleratorPowerState.Level1 => 2,
                ParticleAcceleratorPowerState.Level2 => 3,
                ParticleAcceleratorPowerState.Level3 => 4,
                _ => 0
            });


            _shouldContinueAnimating = false;
            _alarmControl.StopAnimation("warningAnim");
            _alarmControl.Visible = false;
            if (maxState == ParticleAcceleratorPowerState.Level3 && enabled && assembled)
            {
                _shouldContinueAnimating = true;
                _alarmControl.PlayAnimation(_alarmControlAnimation, "warningAnim");
            }
        }

        private void UpdateUI(bool assembled, bool blocked, bool enabled, bool powerBlock)
        {
            _onButton.Pressed = enabled;
            _offButton.Pressed = !enabled;

            var cantUse = !assembled || blocked || powerBlock;
            _onButton.Disabled = cantUse;
            _offButton.Disabled = cantUse;
            _scanButton.Disabled = blocked;

            var cantChangeLevel = !assembled || blocked;
            _stateSpinBox.SetButtonDisabled(cantChangeLevel);
            _blockSpinBox = cantChangeLevel;
        }

        private void UpdatePreview(ParticleAcceleratorUIState updateMessage)
        {
            _endCapTexture.SetPowerState(updateMessage, updateMessage.EndCapExists);
            _fuelChamberTexture.SetPowerState(updateMessage, updateMessage.FuelChamberExists);
            _controlBoxTexture.SetPowerState(updateMessage, true);
            _powerBoxTexture.SetPowerState(updateMessage, updateMessage.PowerBoxExists);
            _emitterCenterTexture.SetPowerState(updateMessage, updateMessage.EmitterCenterExists);
            _emitterLeftTexture.SetPowerState(updateMessage, updateMessage.EmitterLeftExists);
            _emitterRightTexture.SetPowerState(updateMessage, updateMessage.EmitterRightExists);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (!_assembled)
            {
                _drawLabel.Text = Loc.GetString("particle-accelerator-control-menu-draw-not-available");
                return;
            }

            _time += args.DeltaSeconds;

            var watts = 0;
            if (_lastDraw != 0)
            {
                var val = _drawNoiseGenerator.GetNoise(_time);
                watts = (int) (_lastDraw + val * 5);
            }

            _drawLabel.Text = Loc.GetString("particle-accelerator-control-menu-draw",
                                            ("watts", $"{watts:##,##0}"),
                                            ("lastReceive", $"{_lastReceive:##,##0}"));
        }

        private sealed class PASegmentControl : Control
        {
            private readonly ParticleAcceleratorControlMenu _menu;
            private readonly string _baseState;
            private readonly TextureRect _base;
            private readonly TextureRect _unlit;
            private readonly RSI _rsi;

            public PASegmentControl(ParticleAcceleratorControlMenu menu, IResourceCache cache, string name)
            {
                _menu = menu;
                _baseState = name;
                _rsi = cache.GetResource<RSIResource>($"/Textures/Structures/Power/Generation/PA/{name}.rsi").RSI;

                AddChild(_base = new TextureRect {Texture = _rsi[$"completed"].Frame0});
                AddChild(_unlit = new TextureRect());
                MinSize = _rsi.Size;
            }

            public void SetPowerState(ParticleAcceleratorUIState state, bool exists)
            {
                _base.ShaderOverride = exists ? null : _menu._greyScaleShader;
                _base.ModulateSelfOverride = exists ? null : new Color(127, 127, 127);

                if (!state.Enabled || !exists)
                {
                    _unlit.Visible = false;
                    return;
                }

                _unlit.Visible = true;

                var suffix = state.State switch
                {
                    ParticleAcceleratorPowerState.Standby => "_unlitp",
                    ParticleAcceleratorPowerState.Level0 => "_unlitp0",
                    ParticleAcceleratorPowerState.Level1 => "_unlitp1",
                    ParticleAcceleratorPowerState.Level2 => "_unlitp2",
                    ParticleAcceleratorPowerState.Level3 => "_unlitp3",
                    _ => ""
                };

                if (!_rsi.TryGetState(_baseState + suffix, out var rState))
                {
                    _unlit.Visible = false;
                    return;
                }

                _unlit.Texture = rState.Frame0;
            }
        }
    }
}
