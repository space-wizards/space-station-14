using Content.Client.GameObjects.Components.Instruments;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Robust.Client.Audio.Midi;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Timers;

namespace Content.Client.Instruments
{
    public class InstrumentMenu : SS14Window
    {
        [Dependency] private readonly IMidiManager _midiManager = default!;
        [Dependency] private readonly IFileDialogManager _fileDialogManager = default!;

        private InstrumentBoundUserInterface _owner;
        private Button midiLoopButton;
        private Button midiStopButton;
        private Button midiInputButton;

        protected override Vector2? CustomSize => (400, 150);

        public InstrumentMenu(InstrumentBoundUserInterface owner)
        {
            IoCManager.InjectDependencies(this);
            Title = Loc.GetString("Instrument");

            _owner = owner;

            _owner.Instrument.OnMidiPlaybackEnded += InstrumentOnMidiPlaybackEnded;

            var margin = new MarginContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            var vBox = new VBoxContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SeparationOverride = 5,
            };

            var hBoxTopButtons = new HBoxContainer()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1,
                Align = BoxContainer.AlignMode.Center
            };

            midiInputButton = new Button()
            {
                Text = Loc.GetString("MIDI Input"),
                TextAlign = Label.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1,
                ToggleMode = true,
                Pressed = _owner.Instrument.IsInputOpen,
            };

            midiInputButton.OnToggled += MidiInputButtonOnOnToggled;

            var topSpacer = new Control()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 2,
            };

            var midiFileButton = new Button()
            {
                Text = Loc.GetString("Play MIDI File"),
                TextAlign = Label.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1,
            };

            midiFileButton.OnPressed += MidiFileButtonOnOnPressed;

            var hBoxBottomButtons = new HBoxContainer()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1,
                Align = BoxContainer.AlignMode.Center
            };

            midiLoopButton = new Button()
            {
                Text = Loc.GetString("Loop"),
                TextAlign = Label.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1,
                ToggleMode = true,
                Disabled = !_owner.Instrument.IsMidiOpen,
                Pressed = _owner.Instrument.LoopMidi,
            };

            midiLoopButton.OnToggled += MidiLoopButtonOnOnToggled;

            var bottomSpacer = new Control()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 2,
            };

            midiStopButton = new Button()
            {
                Text = Loc.GetString("Stop"),
                TextAlign = Label.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1,
                Disabled = !_owner.Instrument.IsMidiOpen,
            };

            midiStopButton.OnPressed += MidiStopButtonOnPressed;

            hBoxBottomButtons.AddChild(midiLoopButton);
            hBoxBottomButtons.AddChild(bottomSpacer);
            hBoxBottomButtons.AddChild(midiStopButton);

            hBoxTopButtons.AddChild(midiInputButton);
            hBoxTopButtons.AddChild(topSpacer);
            hBoxTopButtons.AddChild(midiFileButton);

            vBox.AddChild(hBoxTopButtons);
            vBox.AddChild(hBoxBottomButtons);

            margin.AddChild(vBox);

            if (!_midiManager.IsAvailable)
            {
                margin.AddChild(new PanelContainer
                {
                    MouseFilter = MouseFilterMode.Stop,
                    PanelOverride = new StyleBoxFlat {BackgroundColor = Color.Black.WithAlpha(0.90f)},
                    Children =
                    {
                        new Label
                        {
                            Align = Label.AlignMode.Center,
                            SizeFlagsVertical = SizeFlags.ShrinkCenter,
                            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                            StyleClasses = {StyleNano.StyleClassLabelBig},
                            Text = Loc.GetString("MIDI support is currently\nnot available on your platform.")
                        }
                    }
                });
            }

            Contents.AddChild(margin);
        }

        private void InstrumentOnMidiPlaybackEnded()
        {
            MidiPlaybackSetButtonsDisabled(true);
        }

        public void MidiPlaybackSetButtonsDisabled(bool disabled)
        {
            midiLoopButton.Disabled = disabled;
            midiStopButton.Disabled = disabled;
        }

        private async void MidiFileButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            var filters = new FileDialogFilters(new FileDialogFilters.Group("mid", "midi"));
            var filename = await _fileDialogManager.OpenFile(filters);

            var instrumentEnt = _owner.Instrument.Owner;
            var instrument = _owner.Instrument;

            ContainerHelpers.TryGetContainerMan(_owner.Instrument.Owner, out var conMan);

            var localPlayer = IoCManager.Resolve<IPlayerManager>().LocalPlayer;

            // The following checks are only in place to prevent players from playing MIDI songs locally.
            // There are equivalents for these checks on the server.

            if (string.IsNullOrEmpty(filename)) return;

            // If we don't have a player or controlled entity, we return.
            if(localPlayer?.ControlledEntity == null) return;

            // If the instrument is handheld and we're not holding it, we return.
            if((instrument.Handheld && (conMan == null
                                        || conMan.Owner != localPlayer.ControlledEntity))) return;

            // We check that we're in range unobstructed just in case.
            if (!localPlayer.InRangeUnobstructed(instrumentEnt)) return;

            if (!_midiManager.IsMidiFile(filename))
            {
                Logger.Warning($"Not a midi file! Chosen file: {filename}");
                return;
            }

            MidiStopButtonOnPressed(null);
            await Timer.Delay(100);
            if (!_owner.Instrument.OpenMidi(filename)) return;
            MidiPlaybackSetButtonsDisabled(false);
            if (midiInputButton.Pressed)
                midiInputButton.Pressed = false;
        }

        private void MidiInputButtonOnOnToggled(BaseButton.ButtonToggledEventArgs obj)
        {
            if (obj.Pressed)
            {
                MidiStopButtonOnPressed(null);
                _owner.Instrument.OpenInput();
            }
            else
                _owner.Instrument.CloseInput();
        }

        private void MidiStopButtonOnPressed(BaseButton.ButtonEventArgs obj)
        {
            MidiPlaybackSetButtonsDisabled(true);
            _owner.Instrument.CloseMidi();
        }

        private void MidiLoopButtonOnOnToggled(BaseButton.ButtonToggledEventArgs obj)
        {
            _owner.Instrument.LoopMidi = obj.Pressed;
        }
    }
}
