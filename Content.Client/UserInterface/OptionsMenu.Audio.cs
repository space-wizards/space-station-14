using Content.Client.UserInterface.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

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

                var contents = new VBoxContainer
                {
                    Margin = new Thickness(2, 2, 2, 0),
                    VerticalExpand = true,
                };

                MasterVolumeSlider = new Slider
                {
                    MinValue = 0.0f,
                    MaxValue = 100.0f,
                    HorizontalExpand = true,
                    MinSize = (80, 8),
                    Rounded = true
                };

                MasterVolumeLabel = new Label
                {
                    MinSize = (48, 0),
                    Align = Label.AlignMode.Right
                };

                contents.AddChild(new HBoxContainer
                {
                    Children =
                    {
                        new Control {MinSize = (4, 0)},
                        new Label {Text = Loc.GetString("Master Volume:")},
                        new Control {MinSize = (8, 0)},
                        MasterVolumeSlider,
                        new Control {MinSize = (8, 0)},
                        MasterVolumeLabel,
                        new Control { MinSize = (4, 0) },
                    }
                });

                ApplyButton = new Button
                {
                    Text = Loc.GetString("Apply"), TextAlign = Label.AlignMode.Center,
                    HorizontalAlignment = HAlignment.Right
                };

                vBox.AddChild(new Label
                {
                    Text = Loc.GetString("Volume Sliders"),
                    FontColorOverride = StyleNano.NanoGold,
                    StyleClasses = { StyleNano.StyleClassLabelKeyText }
                });

                vBox.AddChild(contents);

                ResetButton = new Button
                {
                    Text = Loc.GetString("Reset all"),
                    StyleClasses = { StyleBase.ButtonCaution },
                    HorizontalAlignment = HAlignment.Right
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
                            HorizontalExpand = true,
                            VerticalExpand = true,
                            Children =
                            {
                                ResetButton,
                                new Control { MinSize = (2, 0) },
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
