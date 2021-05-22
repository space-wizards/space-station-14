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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;

namespace ManagedDoom
{
    public sealed class Wad : IDisposable
    {
        private List<string> names;
        private List<Stream> streams;
        private List<LumpInfo> lumpInfos;
        private GameVersion gameVersion;
        private GameMode gameMode;
        private MissionPack missionPack;

        public Wad(params string[] fileNames)
        {
            try
            {
                Console.Write("Open WAD files: ");

                names = new List<string>();
                streams = new List<Stream>();
                lumpInfos = new List<LumpInfo>();

                foreach (var fileName in fileNames)
                {
                    AddFile(fileName);
                }

                gameMode = GetGameMode(names);
                missionPack = GetMissionPack(names);
                gameVersion = GetGameVersion(names);

                Console.WriteLine("OK (" + string.Join(", ", fileNames.Select(x => Path.GetFileName(x))) + ")");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed");
                Dispose();
                ExceptionDispatchInfo.Throw(e);
            }
        }

        private void AddFile(string fileName)
        {
            names.Add(Path.GetFileNameWithoutExtension(fileName).ToLower());

            var stream = IoCManager.Resolve<IResourceManager>().ContentFileRead($"/{fileName}");
            streams.Add(stream);

            string identification;
            int lumpCount;
            int lumpInfoTableOffset;
            {
                var data = new byte[12];
                if (stream.Read(data, 0, data.Length) != data.Length)
                {
                    throw new Exception("Failed to read the WAD file.");
                }

                identification = DoomInterop.ToString(data, 0, 4);
                lumpCount = BitConverter.ToInt32(data, 4);
                lumpInfoTableOffset = BitConverter.ToInt32(data, 8);
                if (identification != "IWAD" && identification != "PWAD")
                {
                    throw new Exception("The file is not a WAD file.");
                }
            }

            {
                var data = new byte[LumpInfo.DataSize * lumpCount];
                stream.Seek(lumpInfoTableOffset, SeekOrigin.Begin);
                if (stream.Read(data, 0, data.Length) != data.Length)
                {
                    throw new Exception("Failed to read the WAD file.");
                }

                for (var i = 0; i < lumpCount; i++)
                {
                    var offset = LumpInfo.DataSize * i;
                    var lumpInfo = new LumpInfo(
                        DoomInterop.ToString(data, offset + 8, 8),
                        stream,
                        BitConverter.ToInt32(data, offset),
                        BitConverter.ToInt32(data, offset + 4));
                    lumpInfos.Add(lumpInfo);
                }
            }
        }

        public int GetLumpNumber(string name)
        {
            for (var i = lumpInfos.Count - 1; i >= 0; i--)
            {
                if (lumpInfos[i].Name == name)
                {
                    return i;
                }
            }

            return -1;
        }

        public int GetLumpSize(int number)
        {
            return lumpInfos[number].Size;
        }

        public byte[] ReadLump(int number)
        {
            var lumpInfo = lumpInfos[number];

            var data = new byte[lumpInfo.Size];

            lumpInfo.Stream.Seek(lumpInfo.Position, SeekOrigin.Begin);
            var read = lumpInfo.Stream.Read(data, 0, lumpInfo.Size);
            if (read != lumpInfo.Size)
            {
                throw new Exception("Failed to read the lump " + number + ".");
            }

            return data;
        }

        public byte[] ReadLump(string name)
        {
            var lumpNumber = GetLumpNumber(name);

            if (lumpNumber == -1)
            {
                throw new Exception("The lump '" + name + "' was not found.");
            }

            return ReadLump(lumpNumber);
        }

        public void Dispose()
        {
            Console.WriteLine("Close WAD files.");

            foreach (var stream in streams)
            {
                stream.Dispose();
            }

            streams.Clear();
        }

        private static GameVersion GetGameVersion(IReadOnlyList<string> names)
        {
            foreach (var name in names)
            {
                switch (name.ToLower())
                {
                    case "doom2":
                    case "freedoom2":
                        return GameVersion.Version109;
                    case "doom":
                    case "doom1":
                    case "freedoom1":
                        return GameVersion.Ultimate;
                    case "plutonia":
                    case "tnt":
                        return GameVersion.Final;
                }
            }

            return GameVersion.Version109;
        }

        private static GameMode GetGameMode(IReadOnlyList<string> names)
        {
            foreach (var name in names)
            {
                switch (name.ToLower())
                {
                    case "doom2":
                    case "plutonia":
                    case "tnt":
                    case "freedoom2":
                        return GameMode.Commercial;
                    case "doom":
                    case "freedoom1":
                        return GameMode.Retail;
                    case "doom1":
                        return GameMode.Shareware;
                }
            }

            return GameMode.Indetermined;
        }

        private static MissionPack GetMissionPack(IReadOnlyList<string> names)
        {
            foreach (var name in names)
            {
                switch (name.ToLower())
                {
                    case "plutonia":
                        return MissionPack.Plutonia;
                    case "tnt":
                        return MissionPack.Tnt;
                }
            }

            return MissionPack.Doom2;
        }

        public IReadOnlyList<string> Names => names;
        public IReadOnlyList<LumpInfo> LumpInfos => lumpInfos;
        public GameVersion GameVersion => gameVersion;
        public GameMode GameMode => gameMode;
        public MissionPack MissionPack => missionPack;
    }
}
