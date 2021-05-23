using System;
using Robust.Client.Audio.Midi;
using Robust.Client.Player;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace ManagedDoom.Audio
{
    public class RobustMusic : IMusic, IDisposable
    {
        // TODO: no handling of music looping.
        [Dependency] private readonly IMidiManager _midiManager = default!;

        private readonly Config _config;
        private readonly Wad _wad;

        private IMidiRenderer _renderer;
        private Bgm _current = Bgm.NONE;

        public RobustMusic(Config config, Wad wad)
        {
            IoCManager.InjectDependencies(this);

            _config = config;
            _wad = wad;
        }

        public void StartMusic(Bgm bgm, bool loop)
        {
            if (bgm == _current)
            {
                return;
            }

            _renderer?.Dispose();
            _renderer = _midiManager.GetNewRenderer();

            var lump = "D_" + DoomInfo.BgmNames[(int) bgm].ToString().ToUpper();
            var data = _wad.ReadLump(lump);
            var decoder = ReadData(data, loop);
            decoder.QueueAllEvents(_renderer);

            _renderer.Mono = true;
            _renderer.MidiBank = 0;
            _renderer.MidiProgram = 1;
            _renderer.TrackingEntity = IoCManager.Resolve<IPlayerManager>().LocalPlayer.ControlledEntity;
            _renderer.DisablePercussionChannel = false;
            _renderer.DisableProgramChangeEvent = false;

            _current = bgm;
        }

        private MusDecoder ReadData(byte[] data, bool loop)
        {
            var isMus = true;
            for (var i = 0; i < MusDecoder.MusHeader.Length; i++)
            {
                if (data[i] != MusDecoder.MusHeader[i])
                {
                    isMus = false;
                }
            }

            if (isMus)
            {
                return new MusDecoder(data, loop);
            }

            /*
            var isMidi = true;
            for (var i = 0; i < MidiDecoder.MidiHeader.Length; i++)
            {
                if (data[i] != MidiDecoder.MidiHeader[i])
                {
                    isMidi = false;
                }
            }

            if (isMidi)
            {
                return new MidiDecoder(data, loop);
            }
            */

            throw new Exception("Unknown format!");
        }


        public int MaxVolume { get; }
        public int Volume { get; set; }

        public void Dispose()
        {
            _renderer?.Dispose();
        }

        private class MusDecoder
        {
            public static readonly int SampleRate = 44100;
            public static readonly int BufferLength = SampleRate / 140;

            public static readonly byte[] MusHeader = new byte[]
            {
                (byte) 'M',
                (byte) 'U',
                (byte) 'S',
                0x1A
            };

            private byte[] data;
            private bool loop;

            private int scoreLength;
            private int scoreStart;
            private int channelCount;
            private int channelCount2;
            private int instrumentCount;
            private int[] instruments;

            private MusEvent[] events;
            private int eventCount;

            private int[] lastVolume;
            private int p;

            public MusDecoder(byte[] data, bool loop)
            {
                CheckHeader(data);

                this.data = data;
                this.loop = loop;

                scoreLength = BitConverter.ToUInt16(data, 4);
                scoreStart = BitConverter.ToUInt16(data, 6);
                channelCount = BitConverter.ToUInt16(data, 8);
                channelCount2 = BitConverter.ToUInt16(data, 10);
                instrumentCount = BitConverter.ToUInt16(data, 12);
                instruments = new int[instrumentCount];
                for (var i = 0; i < instruments.Length; i++)
                {
                    instruments[i] = BitConverter.ToUInt16(data, 16 + 2 * i);
                }

                events = new MusEvent[128];
                for (var i = 0; i < events.Length; i++)
                {
                    events[i] = new MusEvent();
                }

                eventCount = 0;

                lastVolume = new int[16];

                Reset();
            }

            private static void CheckHeader(byte[] data)
            {
                for (var p = 0; p < MusHeader.Length; p++)
                {
                    if (data[p] != MusHeader[p])
                    {
                        throw new Exception("Invalid format!");
                    }
                }
            }

            // Called from the OnGetData with no timing data passed in?
            public void QueueAllEvents(IMidiRenderer synthesizer)
            {
                synthesizer.ScheduleMidiEvent(new MidiEvent
                {
                    Type = 176,
                    Control = 121,
                    Value = 0,
                }, 0, true);

                var blockDuration = (double) BufferLength / SampleRate;
                var scale = synthesizer.SequencerTimeScale;
                var timeAbs = 0;
                while (true)
                {
                    var delay = ReadSingleEventGroup();

                    var totalSecs = timeAbs * blockDuration;
                    var ticks = (uint) (totalSecs * scale);

                    SendEvents(ticks, synthesizer);

                    if (delay == -1)
                    {
                        for (byte i = 0; i < 16; i++)
                        {
                            synthesizer.ScheduleMidiEvent(new MidiEvent
                            {
                                Type = 0xF0,
                                Control = 11,
                                Channel = i
                            }, ticks, true);
                        }

                        return;
                    }

                    timeAbs += delay;
                }
            }

            private void Reset()
            {
                for (var i = 0; i < lastVolume.Length; i++)
                {
                    lastVolume[i] = 0;
                }

                p = scoreStart;

                // delay = 0;
            }

            private int ReadSingleEventGroup()
            {
                eventCount = 0;
                while (true)
                {
                    var result = ReadSingleEvent();
                    if (result == ReadResult.EndOfGroup)
                    {
                        break;
                    }
                    else if (result == ReadResult.EndOfFile)
                    {
                        return -1;
                    }
                }

                var time = 0;
                while (true)
                {
                    var value = data[p++];
                    time = time * 128 + (value & 127);
                    if ((value & 128) == 0)
                    {
                        break;
                    }
                }

                return time;
            }

            private ReadResult ReadSingleEvent()
            {
                var channelNumber = data[p] & 0xF;
                if (channelNumber == 15)
                {
                    channelNumber = 9;
                }

                var eventType = (data[p] & 0x70) >> 4;
                var last = (data[p] >> 7) != 0;

                p++;

                var me = events[eventCount];
                eventCount++;

                switch (eventType)
                {
                    case 0: // RELEASE NOTE
                        me.Type = 0;
                        me.Channel = channelNumber;

                        var releaseNote = data[p++];

                        me.Data1 = releaseNote;
                        me.Data2 = 0;

                        break;

                    case 1: // PLAY NOTE
                        me.Type = 1;
                        me.Channel = channelNumber;

                        var playNote = data[p++];
                        var noteNumber = playNote & 127;
                        var noteVolume = (playNote & 128) != 0 ? data[p++] : -1;

                        me.Data1 = noteNumber;
                        if (noteVolume == -1)
                        {
                            me.Data2 = lastVolume[channelNumber];
                        }
                        else
                        {
                            me.Data2 = noteVolume;
                            lastVolume[channelNumber] = noteVolume;
                        }

                        break;

                    case 2: // PITCH WHEEL
                        me.Type = 2;
                        me.Channel = channelNumber;

                        var pitchWheel = data[p++];

                        var pw2 = (pitchWheel << 7) / 2;
                        var pw1 = pw2 & 127;
                        pw2 >>= 7;
                        me.Data1 = pw1;
                        me.Data2 = pw2;

                        break;

                    case 3: // SYSTEM EVENT
                        me.Type = 3;
                        me.Channel = -1;

                        var systemEvent = data[p++];
                        me.Data1 = systemEvent;
                        me.Data2 = 0;

                        break;

                    case 4: // CONTROL CHANGE
                        me.Type = 4;
                        me.Channel = channelNumber;

                        var controllerNumber = data[p++];
                        var controllerValue = data[p++];

                        me.Data1 = controllerNumber;
                        me.Data2 = controllerValue;

                        break;

                    case 6: // END OF FILE
                        return ReadResult.EndOfFile;

                    default:
                        throw new Exception("Unknown event type!");
                }

                if (last)
                {
                    return ReadResult.EndOfGroup;
                }
                else
                {
                    return ReadResult.Ongoing;
                }
            }

            private void SendEvents(uint tick, IMidiRenderer synthesizer)
            {
                for (var i = 0; i < eventCount; i++)
                {
                    var me = events[i];
                    switch (me.Type)
                    {
                        case 0: // RELEASE NOTE
                            Send(new MidiEvent
                            {
                                Type = 0x80,
                                Channel = (byte) me.Channel,
                                Key = (byte) me.Data1
                            });
                            // synthesizer.NoteOff(me.Channel, me.Data1);
                            break;

                        case 1: // PLAY NOTE
                            Send(new MidiEvent
                            {
                                Type = 0x90,
                                Channel = (byte) me.Channel,
                                Key = (byte) me.Data1,
                                Velocity = (byte) me.Data2
                            });
                            // synthesizer.NoteOn(me.Channel, me.Data1, me.Data2);
                            break;

                        case 2: // PITCH WHEEL
                            Send(new MidiEvent
                            {
                                Type = 0xE0,
                                Channel = (byte) me.Channel,
                                Pitch = (byte) me.Data2
                            });
                            // synthesizer.ProcessMidiMessage(me.Channel, 0xE0, me.Data1, me.Data2);
                            break;

                        case 3: // SYSTEM EVENT
                            switch (me.Data1)
                            {
                                // TODO: Unimplemented
                                case 11: // ALL NOTES OFF
                                    Send(new MidiEvent
                                    {
                                        Type = 0xF0,
                                        Control = 11,
                                        Channel = (byte) me.Channel
                                    });
                                    // synthesizer.NoteOffAll(me.Channel, false);
                                    break;

                                case 14: // RESET ALL CONTROLS
                                    Send(new MidiEvent
                                    {
                                        Type = 176,
                                        Control = 121,
                                        Value = 0,
                                    });
                                    // synthesizer.ResetAllControllers(me.Channel);
                                    break;
                            }

                            break;

                        case 4: // CONTROL CHANGE
                            switch (me.Data1)
                            {
                                case 0: // PROGRAM CHANGE
                                    if (me.Channel == 9)
                                    {
                                        break;
                                    }

                                    Send(new MidiEvent
                                    {
                                        Type = 0xC0,
                                        Channel = (byte) me.Channel,
                                        Program = (byte) me.Data2,
                                    });
                                    // synthesizer.ProcessMidiMessage(me.Channel, 0xC0, me.Data2, 0);
                                    break;

                                case 1: // BANK SELECTION
                                    Send(new MidiEvent
                                    {
                                        Type = 0xB0,
                                        Control = 0x00,
                                        Value = (byte) me.Data2,
                                    });
                                    // synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x00, me.Data2);
                                    break;

                                case 2: // MODULATION
                                    Send(new MidiEvent
                                    {
                                        Type = 0xB0,
                                        Control = 0x01,
                                        Value = (byte) me.Data2,
                                    });
                                    // synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x01, me.Data2);
                                    break;

                                case 3: // VOLUME
                                    Send(new MidiEvent
                                    {
                                        Type = 0xB0,
                                        Control = 0x02,
                                        Value = (byte) me.Data2,
                                    });
                                    // synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x07, me.Data2);
                                    break;

                                case 4: // PAN
                                    Send(new MidiEvent
                                    {
                                        Type = 0xB0,
                                        Control = 0x03,
                                        Value = (byte) me.Data2,
                                    });
                                    // synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x0A, me.Data2);
                                    break;

                                case 5: // EXPRESSION
                                    Send(new MidiEvent
                                    {
                                        Type = 0xB0,
                                        Control = 0x04,
                                        Value = (byte) me.Data2,
                                    });
                                    // synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x0B, me.Data2);
                                    break;

                                case 8: // PEDAL
                                    Send(new MidiEvent
                                    {
                                        Type = 0xB0,
                                        Control = 0x05,
                                        Value = (byte) me.Data2,
                                    });
                                    // synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x40, me.Data2);
                                    break;
                            }

                            break;
                    }
                }

                void Send(MidiEvent ev)
                {
                    synthesizer.ScheduleMidiEvent(ev, tick, true);
                }
            }

            private class MusEvent
            {
                public int Type;
                public int Channel;
                public int Data1;
                public int Data2;
            }

            private enum ReadResult
            {
                Ongoing,
                EndOfGroup,
                EndOfFile
            }
        }
    }
}
