//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//



using System;
using System.Runtime.ExceptionServices;

namespace ManagedDoom.Audio
{
    public sealed class SfmlSound : ISound, IDisposable
    {
        private static readonly int channelCount = 8;

        private static readonly float fastDecay = (float)Math.Pow(0.5, 1.0 / (35 / 5));
        private static readonly float slowDecay = (float)Math.Pow(0.5, 1.0 / 35);

        private static readonly float clipDist = 1200;
        private static readonly float closeDist = 160;
        private static readonly float attenuator = clipDist - closeDist;

        private Config config;

        //private SoundBuffer[] buffers;
        private float[] amplitudes;

        private DoomRandom random;

        //private Sound[] channels;
        private ChannelInfo[] infos;

        //private Sound uiChannel;
        private Sfx uiReserved;

        private Mobj listener;

        private float masterVolumeDecay;

        private DateTime lastUpdate;

        public SfmlSound(Config config, Wad wad)
        {
            /*
            try
            {
                Console.Write("Initialize sound: ");

                this.config = config;

                config.audio_soundvolume = Math.Clamp(config.audio_soundvolume, 0, MaxVolume);

                buffers = new SoundBuffer[DoomInfo.SfxNames.Length];
                amplitudes = new float[DoomInfo.SfxNames.Length];

                if (config.audio_randompitch)
                {
                    random = new DoomRandom();
                }

                for (var i = 0; i < DoomInfo.SfxNames.Length; i++)
                {
                    var name = "DS" + DoomInfo.SfxNames[i].ToString().ToUpper();
                    var lump = wad.GetLumpNumber(name);
                    if (lump == -1)
                    {
                        continue;
                    }

                    int sampleRate;
                    int sampleCount;
                    var samples = GetSamples(wad, name, out sampleRate, out sampleCount);
                    if (samples != null)
                    {
                        buffers[i] = new SoundBuffer(samples, 1, (uint)sampleRate);
                        amplitudes[i] = GetAmplitude(samples, sampleRate, sampleCount);
                    }
                }

                channels = new Sound[channelCount];
                infos = new ChannelInfo[channelCount];
                for (var i = 0; i < channels.Length; i++)
                {
                    channels[i] = new Sound();
                    infos[i] = new ChannelInfo();
                }

                uiChannel = new Sound();
                uiReserved = Sfx.NONE;

                masterVolumeDecay = (float)config.audio_soundvolume / MaxVolume;

                lastUpdate = DateTime.MinValue;

                Console.WriteLine("OK");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed");
                Dispose();
                ExceptionDispatchInfo.Throw(e);
            }
        */
        }

        /*
        private static short[] GetSamples(Wad wad, string name, out int sampleRate, out int sampleCount)
        {
            var data = wad.ReadLump(name);

            if (data.Length < 8)
            {
                sampleRate = -1;
                sampleCount = -1;
                return null;
            }

            sampleRate = BitConverter.ToUInt16(data, 2);
            sampleCount = BitConverter.ToInt32(data, 4);

            var offset = 8;
            if (ContainsDmxPadding(data))
            {
                offset += 16;
                sampleCount -= 32;
            }

            if (sampleCount > 0)
            {
                var samples = new short[sampleCount];
                for (var t = 0; t < samples.Length; t++)
                {
                    samples[t] = (short)((data[offset + t] - 128) << 8);
                }
                return samples;
            }
            else
            {
                return null;
            }
        }

        // Check if the data contains pad bytes.
        // If the first and last 16 samples are the same,
        // the data should contain pad bytes.
        // https://doomwiki.org/wiki/Sound
        private static bool ContainsDmxPadding(byte[] data)
        {
            var sampleCount = BitConverter.ToInt32(data, 4);
            if (sampleCount < 32)
            {
                return false;
            }
            else
            {
                var first = data[8];
                for (var i = 1; i < 16; i++)
                {
                    if (data[8 + i] != first)
                    {
                        return false;
                    }
                }

                var last = data[8 + sampleCount - 1];
                for (var i = 1; i < 16; i++)
                {
                    if (data[8 + sampleCount - i - 1] != last)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static float GetAmplitude(short[] samples, int sampleRate, int sampleCount)
        {
            var max = 0;
            if (sampleCount > 0)
            {
                var count = Math.Min(sampleRate / 5, sampleCount);
                for (var t = 0; t < count; t++)
                {
                    var a = (int)samples[t];
                    if (a < 0)
                    {
                        a = (short)(-a);
                    }
                    if (a > max)
                    {
                        max = a;
                    }
                }
            }
            return (float)max / 32768;
        }
        */

        public void SetListener(Mobj listener)
        {
            this.listener = listener;
        }

        public void Update()
        {
            /*
            var now = DateTime.Now;
            if ((now - lastUpdate).TotalSeconds < 0.01)
            {
                // Don't update so frequently (for timedemo).
                return;
            }

            for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                var channel = channels[i];

                if (info.Playing != Sfx.NONE)
                {
                    if (channel.Status != SoundStatus.Stopped)
                    {
                        if (info.Type == SfxType.Diffuse)
                        {
                            info.Priority *= slowDecay;
                        }
                        else
                        {
                            info.Priority *= fastDecay;
                        }
                        SetParam(channel, info);
                    }
                    else
                    {
                        info.Playing = Sfx.NONE;
                        if (info.Reserved == Sfx.NONE)
                        {
                            info.Source = null;
                        }
                    }
                }

                if (info.Reserved != Sfx.NONE)
                {
                    if (info.Playing != Sfx.NONE)
                    {
                        channel.Stop();
                    }

                    channel.SoundBuffer = buffers[(int)info.Reserved];
                    SetParam(channel, info);
                    channel.Pitch = GetPitch(info.Type, info.Reserved);
                    channel.Play();
                    info.Playing = info.Reserved;
                    info.Reserved = Sfx.NONE;
                }
            }

            if (uiReserved != Sfx.NONE)
            {
                if (uiChannel.Status == SoundStatus.Playing)
                {
                    uiChannel.Stop();
                }
                uiChannel.Volume = 100 * masterVolumeDecay;
                uiChannel.SoundBuffer = buffers[(int)uiReserved];
                uiChannel.Play();
                uiReserved = Sfx.NONE;
            }

            lastUpdate = now;
        */
        }

        public void StartSound(Sfx sfx)
        {
            /*
            if (buffers[(int)sfx] == null)
            {
                return;
            }

            uiReserved = sfx;
        */
        }

        public void StartSound(Mobj mobj, Sfx sfx, SfxType type)
        {
            StartSound(mobj, sfx, type, 100);
        }

        public void StartSound(Mobj mobj, Sfx sfx, SfxType type, int volume)
        {
            /*
            if (buffers[(int)sfx] == null)
            {
                return;
            }

            var x = (mobj.X - listener.X).ToFloat();
            var y = (mobj.Y - listener.Y).ToFloat();
            var dist = MathF.Sqrt(x * x + y * y);

            float priority;
            if (type == SfxType.Diffuse)
            {
                priority = volume;
            }
            else
            {
                priority = amplitudes[(int)sfx] * GetDistanceDecay(dist) * volume;
            }

            for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                if (info.Source == mobj && info.Type == type)
                {
                    info.Reserved = sfx;
                    info.Priority = priority;
                    info.Volume = volume;
                    return;
                }
            }

            for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                if (info.Reserved == Sfx.NONE && info.Playing == Sfx.NONE)
                {
                    info.Reserved = sfx;
                    info.Priority = priority;
                    info.Source = mobj;
                    info.Type = type;
                    info.Volume = volume;
                    return;
                }
            }

            var minPriority = float.MaxValue;
            var minChannel = -1;
            for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                if (info.Priority < minPriority)
                {
                    minPriority = info.Priority;
                    minChannel = i;
                }
            }
            if (priority >= minPriority)
            {
                var info = infos[minChannel];
                info.Reserved = sfx;
                info.Priority = priority;
                info.Source = mobj;
                info.Type = type;
                info.Volume = volume;
            }
        */
        }

        public void StopSound(Mobj mobj)
        {
            /*for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                if (info.Source == mobj)
                {
                    info.LastX = info.Source.X;
                    info.LastY = info.Source.Y;
                    info.Source = null;
                    info.Volume /= 5;
                }
            }*/
        }

        public void Reset()
        {
            /*
            if (random != null)
            {
                random.Clear();
            }

            for (var i = 0; i < infos.Length; i++)
            {
                channels[i].Stop();
                infos[i].Clear();
            }

            listener = null;
        */
        }

        public void Pause()
        {
            /*
            for (var i = 0; i < infos.Length; i++)
            {
                var channel = channels[i];

                if (channel.Status == SoundStatus.Playing &&
                    channel.SoundBuffer.Duration - channel.PlayingOffset > Time.FromMilliseconds(200))
                {
                    channels[i].Pause();
                }
            }
        */
        }

        public void Resume()
        {
            /*
            for (var i = 0; i < infos.Length; i++)
            {
                var channel = channels[i];

                if (channel.Status == SoundStatus.Paused)
                {
                    channel.Play();
                }
            }
        */
        }

        /*
        private void SetParam(Sound sound, ChannelInfo info)
        {
            if (info.Type == SfxType.Diffuse)
            {
                sound.Position = new Vector3f(0, 1, 0);
                sound.Volume = masterVolumeDecay * info.Volume;
            }
            else
            {
                Fixed sourceX;
                Fixed sourceY;
                if (info.Source == null)
                {
                    sourceX = info.LastX;
                    sourceY = info.LastY;
                }
                else
                {
                    sourceX = info.Source.X;
                    sourceY = info.Source.Y;
                }

                var x = (sourceX - listener.X).ToFloat();
                var y = (sourceY - listener.Y).ToFloat();

                if (Math.Abs(x) < 16 && Math.Abs(y) < 16)
                {
                    sound.Position = new Vector3f(0, 1, 0);
                    sound.Volume = masterVolumeDecay * info.Volume;
                }
                else
                {
                    var dist = MathF.Sqrt(x * x + y * y);
                    var angle = MathF.Atan2(y, x) - (float)listener.Angle.ToRadian() + MathF.PI / 2;
                    sound.Position = new Vector3f(MathF.Cos(angle), MathF.Sin(angle), 0);
                    sound.Volume = masterVolumeDecay * GetDistanceDecay(dist) * info.Volume;
                }
            }
        }
        */

        private float GetDistanceDecay(float dist)
        {
            if (dist < closeDist)
            {
                return 1F;
            }
            else
            {
                return Math.Max((clipDist - dist) / attenuator, 0F);
            }
        }

        private float GetPitch(SfxType type, Sfx sfx)
        {
            if (random != null)
            {
                if (sfx == Sfx.ITEMUP || sfx == Sfx.TINK || sfx == Sfx.RADIO)
                {
                    return 1.0F;
                }
                else if (type == SfxType.Voice)
                {
                    return 1.0F + 0.075F * (random.Next() - 128) / 128;
                }
                else
                {
                    return 1.0F + 0.025F * (random.Next() - 128) / 128;
                }
            }
            else
            {
                return 1.0F;
            }
        }

        public void Dispose()
        {
            /*Console.WriteLine("Shutdown sound.");

            if (channels != null)
            {
                for (var i = 0; i < channels.Length; i++)
                {
                    if (channels[i] != null)
                    {
                        channels[i].Stop();
                        channels[i].Dispose();
                        channels[i] = null;
                    }
                }
                channels = null;
            }

            if (buffers != null)
            {
                for (var i = 0; i < buffers.Length; i++)
                {
                    if (buffers[i] != null)
                    {
                        buffers[i].Dispose();
                        buffers[i] = null;
                    }
                }
                buffers = null;
            }

            if (uiChannel != null)
            {
                uiChannel.Dispose();
                uiChannel = null;
            }*/
        }

        public int MaxVolume
        {
            get
            {
                return 15;
            }
        }

        public int Volume
        {
            get
            {
                return config.audio_soundvolume;
            }

            set
            {
                config.audio_soundvolume = value;
                masterVolumeDecay = (float)config.audio_soundvolume / MaxVolume;
            }
        }



        private class ChannelInfo
        {
            public Sfx Reserved;
            public Sfx Playing;
            public float Priority;

            public Mobj Source;
            public SfxType Type;
            public int Volume;
            public Fixed LastX;
            public Fixed LastY;

            public void Clear()
            {
                Reserved = Sfx.NONE;
                Playing = Sfx.NONE;
                Priority = 0;
                Source = null;
                Type = 0;
                Volume = 0;
                LastX = Fixed.Zero;
                LastY = Fixed.Zero;
            }
        }
    }
}
