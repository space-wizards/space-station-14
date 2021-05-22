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
using System.Diagnostics;
using System.IO;
using ManagedDoom.Audio;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;

namespace ManagedDoom
{
    public static class ConfigUtilities
    {
        public static string GetExeDirectory()
        {
            return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        }

        public static string GetConfigPath()
        {
            return Path.Combine(GetExeDirectory(), "managed-doom.cfg");
        }

        /*
        public static VideoMode GetDefaultVideoMode()
        {
            var desktop = VideoMode.DesktopMode;

            var baseWidth = 640;
            var baseHeight = 400;

            var currentWidth = baseWidth;
            var currentHeight = baseHeight;

            while (true)
            {
                var nextWidth = currentWidth + baseWidth;
                var nextHeight = currentHeight + baseHeight;

                if (nextWidth >= 0.9 * desktop.Width ||
                    nextHeight >= 0.9 * desktop.Height)
                {
                    break;
                }

                currentWidth = nextWidth;
                currentHeight = nextHeight;
            }

            return new VideoMode((uint)currentWidth, (uint)currentHeight);
        }
        */

        public static string GetDefaultIwadPath()
        {
            var resm = IoCManager.Resolve<IResourceManager>();
            var names = new string[]
            {
                "DOOM2.WAD",
                "PLUTONIA.WAD",
                "TNT.WAD",
                "DOOM.WAD",
                "DOOM1.WAD",
                "FREEDOOM2.WAD",
                "FREEDOOM1.WAD"
            };

            foreach (var name in names)
            {
                if (resm.ContentFileExists($"/{name}"))
                {
                    return name;
                }
            }

            throw new Exception("No IWAD was found!");
        }

        /*
        public static SfmlMusic GetSfmlMusicInstance(Config config, Wad wad)
        {
            var sfPath = Path.Combine(GetExeDirectory(), config.audio_soundfont);
            if (File.Exists(sfPath))
            {
                return new SfmlMusic(config, wad, sfPath);
            }
            else
            {
                Console.WriteLine("SoundFont '" + config.audio_soundfont + "' was not found!");
                return null;
            }
        }*/
    }
}
