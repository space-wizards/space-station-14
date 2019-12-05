using Content.Client.GameObjects.Components.Instruments;
using Robust.Client.Audio.Midi;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;

namespace Content.Client.Instruments
{
    public class InstrumentMenu : SS14Window
    {
#pragma warning disable 649
        [Dependency] private IMidiManager _midiManager;
        [Dependency] private IFileDialogManager _fileDialogManager;
#pragma warning restore 649

        private InstrumentBoundUserInterface _owner;
        private Button midiLoopButton;
        private Button midiStopButton;
        private Button midiInputButton;

        protected override Vector2? CustomSize => (400, 150);

        public InstrumentMenu(InstrumentBoundUserInterface owner)
        {
            IoCManager.InjectDependencies(this);
            Title = "Instrument";

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
                Text = "MIDI Input",
                TextAlign = Button.AlignMode.Center,
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
                Text = "Open File",
                TextAlign = Button.AlignMode.Center,
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
                Text = "Loop",
                TextAlign = Button.AlignMode.Center,
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
                Text = "Stop",
                TextAlign = Button.AlignMode.Center,
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
            var filename = await _fileDialogManager.OpenFile();

            if (filename == null) return;

            if (!_midiManager.IsMidiFile(filename))
            {
                Logger.Warning($"Not a midi file! Chosen file: {filename}");
                return;
            }

            if (!_owner.Instrument.OpenMidi(filename)) return;
            MidiPlaybackSetButtonsDisabled(false);
            if(midiInputButton.Pressed)
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
