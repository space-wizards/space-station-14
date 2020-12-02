using Content.Client.UserInterface.Stylesheets;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Localization;

namespace Content.Client.UserInterface
{
    public sealed partial class OptionsMenu
    {
        private sealed class AudioControl : Control
        {
            private readonly IConfigurationManager _cfg;
            private readonly IClydeAudio _clydeAudio;

            private readonly Button ApplyButton;
            private readonly Label MasterVolumeLabel;
            private readonly Slider MasterVolumeSlider;
            private readonly Button ResetButton;

            public AudioControl(IConfigurationManager cfg, IClydeAudio clydeAudio)
            {
                _cfg = cfg;
                _clydeAudio = clydeAudio;

                var vBox = new VBoxContainer();

                var contents = new VBoxContainer();

                MasterVolumeSlider = new Slider
                {
                    MinValue = 0.0f,
                    MaxValue = 100.0f,
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    CustomMinimumSize = (80, 8),
                    Rounded = true
                };

                MasterVolumeLabel = new Label
                {
                    CustomMinimumSize = (48, 0),
                    Align = Label.AlignMode.Right
                };

                contents.AddChild(new HBoxContainer
                {
                    Children =
                    {
                        new Control {CustomMinimumSize = (4, 0)},
                        new Label {Text = Loc.GetString("Master Volume:")},
                        new Control {CustomMinimumSize = (8, 0)},
                        MasterVolumeSlider,
                        new Control {CustomMinimumSize = (8, 0)},
                        MasterVolumeLabel,
                        new Control { CustomMinimumSize = (4, 0) },
                    }
                });

                ApplyButton = new Button
                {
                    Text = Loc.GetString("Apply"), TextAlign = Label.AlignMode.Center,
                    SizeFlagsHorizontal = SizeFlags.ShrinkEnd
                };

                vBox.AddChild(new Label
                {
                    Text = Loc.GetString("Volume Sliders"),
                    FontColorOverride = StyleNano.NanoGold,
                    StyleClasses = { StyleNano.StyleClassLabelKeyText }
                });

                vBox.AddChild(new MarginContainer
                {
                    MarginLeftOverride = 2,
                    MarginTopOverride = 2,
                    MarginRightOverride = 2,
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    Children =
                    {
                        contents
                    }
                });

                ResetButton = new Button
                {
                    Text = Loc.GetString("Reset all"),
                    StyleClasses = { StyleBase.ButtonCaution },
                    SizeFlagsHorizontal = SizeFlags.ShrinkEnd
                };

                vBox.AddChild(new StripeBack
                {
                    HasBottomEdge = false,
                    HasMargins = false,
                    Children =
                    {
                        new HBoxContainer
                        {
                            Align = BoxContainer.AlignMode.End,
                            SizeFlagsHorizontal = SizeFlags.FillExpand,
                            SizeFlagsVertical = SizeFlags.FillExpand,
                            Children =
                            {
                                ResetButton,
                                new Control { CustomMinimumSize = (2, 0) },
                                ApplyButton
                            }
                        }
                    }
                });

                MasterVolumeSlider.Value = _cfg.GetCVar(CVars.AudioMasterVolume) * 100.0f;
                MasterVolumeLabel.Text = string.Format(Loc.GetString("{0:0}%"), MasterVolumeSlider.Value);

                ApplyButton.OnPressed += OnApplyButtonPressed;
                ResetButton.OnPressed += OnResetButtonPressed;
                MasterVolumeSlider.OnValueChanged += OnMasterVolumeSliderChanged;

                AddChild(vBox);
                UpdateChanges();
            }

            protected override void Dispose(bool disposing)
            {
                ApplyButton.OnPressed -= OnApplyButtonPressed;
                ResetButton.OnPressed -= OnResetButtonPressed;
                MasterVolumeSlider.OnValueChanged -= OnMasterVolumeSliderChanged;
                base.Dispose(disposing);
            }

            private void OnMasterVolumeSliderChanged(Range range)
            {
                MasterVolumeLabel.Text = string.Format(Loc.GetString("{0:0}%"), MasterVolumeSlider.Value);
                _clydeAudio.SetMasterVolume(MasterVolumeSlider.Value / 100.0f);
                UpdateChanges();
            }

            private void OnApplyButtonPressed(BaseButton.ButtonEventArgs args)
            {
                _cfg.SetCVar(CVars.AudioMasterVolume, MasterVolumeSlider.Value / 100.0f);
                _cfg.SaveToFile();
                UpdateChanges();
            }

            private void OnResetButtonPressed(BaseButton.ButtonEventArgs args)
            {
                MasterVolumeSlider.Value = _cfg.GetCVar(CVars.AudioMasterVolume) * 100.0f;
                MasterVolumeLabel.Text = string.Format(Loc.GetString("{0:0}%"), MasterVolumeSlider.Value);
                UpdateChanges();
            }

            private void UpdateChanges()
            {
                var isMasterVolumeSame = System.Math.Abs(MasterVolumeSlider.Value - _cfg.GetCVar(CVars.AudioMasterVolume) * 100.0f) < 0.01f;
                var isEverythingSame = isMasterVolumeSame;
                ApplyButton.Disabled = isEverythingSame;
                ResetButton.Disabled = isEverythingSame;
            }
        }
    }
}
