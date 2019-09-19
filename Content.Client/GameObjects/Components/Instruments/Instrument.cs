using System;
using System.Linq;
using Robust.Shared.GameObjects;
using Commons.Music.Midi;
using NFluidsynth;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Logger = NFluidsynth.Logger;
using MidiEvent = NFluidsynth.MidiEvent;

namespace Content.Client.GameObjects.Components.Instruments
{
    [RegisterComponent]
    public class Instrument : Component
    {
        public override string Name => "Instrument";

        private Synth synth;

        public override void Initialize()
        {
            base.Initialize();

            var settings = new Settings();
            settings["synth.sample-rate"].DoubleValue = 44100;
            settings["player.timing-source"].StringValue = "sample";
            settings["synth.lock-memory"].IntValue = 0;
            settings["audio.driver"].StringValue = "pulseaudio";
            synth = new Synth(settings);
            synth.LoadSoundFont("soundfont.sf2", false);
            for (int i = 0; i < 16; i++)
                synth.SoundFontSelect(i, 0);
            synth.ProgramChange(0, 1);
            //var driver = new AudioDriver(settings, synth);

            /*var player = new Player(synth);
            //player.Add("mysong.mid");
            player.SetLoop(1);
            player.Play();*/

            var access = MidiAccessManager.Default;
            IMidiInput input = null;
            foreach (var fluidInput in access.Inputs)
            {
                Robust.Shared.Log.Logger.Info($"{fluidInput.Id}");
                input = access.OpenInputAsync(fluidInput.Id).Result;
            }

            if (input != null)
            {
                Robust.Shared.Log.Logger.Info("Got input!");
                var entman = IoCManager.Resolve<IEntitySystemManager>();
                var audio = IoCManager.Resolve<IClydeAudio>();
                var audioSystem = entman.GetEntitySystem<AudioSystem>();

                input.MessageReceived += (sender, e) =>
                {
                    Console.WriteLine($"{e.Timestamp} {e.Start} {e.Length} {e.Data.Length} {e.Data[0].ToString("X")}");
                    for (var index = 0; index < e.Data.Length; index++)
                    {
                        var d = e.Data[index];
                        if (d != 0)
                            Console.WriteLine(index + " -> " + d.ToString());
                    }

                    //synth.Sysex(new byte[]{e.Data[0], e.Data[1], e.Data[2]}, null, false);

                    var ch = 0;
                    var msg = e.Data;

                    switch (msg[0])
                    {
                        case 0x80:
                            synth.NoteOff(ch, msg[1]);
                            break;
                        case 0x90:
                            if (msg[2] == 0)
                                synth.NoteOff(ch, msg[1]);
                            else
                                synth.NoteOn(ch, msg[1], msg[2]);
                            break;
                        case 0xA0:
                            synth.KeyPressure(ch, msg[1], msg[2]);
                            break;
                        case 0xB0:
                            synth.CC(ch, msg[1], msg[2]);
                            break;
                        case 0xC0:
                            //synth.ProgramChange(ch, msg[1]);
                            break;
                        case 0xD0:
                            synth.ChannelPressure(ch, msg[1]);
                            break;
                        case 0xE0:
                            synth.PitchBend(ch, msg[1] + msg[2] * 0x80);
                            break;
                        case 0xF0:
                            synth.Sysex(new ArraySegment<byte>(msg, 0, msg.Length).ToArray(), null);
                            break;
                        default:
                            break;
                    }
                    int length = 44100;
                    ushort[] lbuffer = new ushort[length];
                    ushort[] rbuffer = new ushort[length];
                    synth.WriteSample16(length, lbuffer, 0, 1, rbuffer, 0, 1);

                    audioSystem.Play(audio.LoadAudioRawPCM(lbuffer), Owner.Transform.GridPosition);
                    audioSystem.Play(audio.LoadAudioRawPCM(rbuffer), Owner.Transform.GridPosition);
                };
            }
        }
    }
}
