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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace ManagedDoom
{
    public sealed class FlatLookup : IReadOnlyList<Flat>
    {
        private Flat[] flats;

        private Dictionary<string, Flat> nameToFlat;
        private Dictionary<string, int> nameToNumber;

        private int skyFlatNumber;
        private Flat skyFlat;

        public FlatLookup(Wad wad) : this(wad, false)
        {
        }

        public FlatLookup(Wad wad, bool useDummy)
        {
            if (!useDummy)
            {
                var fStartCount = CountLump(wad, "F_START");
                var fEndCount = CountLump(wad, "F_END");
                var ffStartCount = CountLump(wad, "FF_START");
                var ffEndCount = CountLump(wad, "FF_END");

                // Usual case.
                var standard =
                    fStartCount == 1 &&
                    fEndCount == 1 &&
                    ffStartCount == 0 &&
                    ffEndCount == 0;

                // A trick to add custom flats is used.
                // https://www.doomworld.com/tutorials/fx2.php
                var customFlatTrick =
                    fStartCount == 1 &&
                    fEndCount >= 2;

                // Need deutex to add flats.
                var deutexMerge =
                    fStartCount + ffStartCount >= 2 &&
                    fEndCount + ffEndCount >= 2;

                if (standard || customFlatTrick)
                {
                    InitStandard(wad);
                }
                else if (deutexMerge)
                {
                    InitDeuTexMerge(wad);
                }
                else
                {
                    throw new Exception("Failed to read flats.");
                }
            }
            else
            {
                InitDummy(wad);
            }
        }

        private void InitStandard(Wad wad)
        {
            try
            {
                Console.Write("Load flats: ");

                var firstFlat = wad.GetLumpNumber("F_START") + 1;
                var lastFlat = wad.GetLumpNumber("F_END") - 1;
                var count = lastFlat - firstFlat + 1;

                flats = new Flat[count];

                nameToFlat = new Dictionary<string, Flat>();
                nameToNumber = new Dictionary<string, int>();

                for (var lump = firstFlat; lump <= lastFlat; lump++)
                {
                    if (wad.GetLumpSize(lump) != 4096)
                    {
                        continue;
                    }

                    var number = lump - firstFlat;
                    var name = wad.LumpInfos[lump].Name;
                    var flat = new Flat(name, wad.ReadLump(lump));

                    flats[number] = flat;
                    nameToFlat[name] = flat;
                    nameToNumber[name] = number;
                }

                skyFlatNumber = nameToNumber["F_SKY1"];
                skyFlat = nameToFlat["F_SKY1"];

                Console.WriteLine("OK (" + nameToFlat.Count + " flats)");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed");
                ExceptionDispatchInfo.Throw(e);
            }
        }

        private void InitDeuTexMerge(Wad wad)
        {
            try
            {
                Console.Write("Load flats: ");
                
                var allFlats = new List<int>();
                var flatZone = false;
                for (var lump = 0; lump < wad.LumpInfos.Count; lump++)
                {
                    var name = wad.LumpInfos[lump].Name;
                    if (flatZone)
                    {
                        if (name == "F_END" || name == "FF_END")
                        {
                            flatZone = false;
                        }
                        else
                        {
                            allFlats.Add(lump);
                        }
                    }
                    else
                    {
                        if (name == "F_START" || name == "FF_START")
                        {
                            flatZone = true;
                        }
                    }
                }
                allFlats.Reverse();

                var dupCheck = new HashSet<string>();
                var distinctFlats = new List<int>();
                foreach (var lump in allFlats)
                {
                    if (!dupCheck.Contains(wad.LumpInfos[lump].Name))
                    {
                        distinctFlats.Add(lump);
                        dupCheck.Add(wad.LumpInfos[lump].Name);
                    }
                }
                distinctFlats.Reverse();

                flats = new Flat[distinctFlats.Count];

                nameToFlat = new Dictionary<string, Flat>();
                nameToNumber = new Dictionary<string, int>();

                for (var number = 0; number < flats.Length; number++)
                {
                    var lump = distinctFlats[number];

                    if (wad.GetLumpSize(lump) != 4096)
                    {
                        continue;
                    }

                    var name = wad.LumpInfos[lump].Name;
                    var flat = new Flat(name, wad.ReadLump(lump));

                    flats[number] = flat;
                    nameToFlat[name] = flat;
                    nameToNumber[name] = number;
                }

                skyFlatNumber = nameToNumber["F_SKY1"];
                skyFlat = nameToFlat["F_SKY1"];

                Console.WriteLine("OK (" + nameToFlat.Count + " flats)");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed");
                ExceptionDispatchInfo.Throw(e);
            }
        }

        private void InitDummy(Wad wad)
        {
            var firstFlat = wad.GetLumpNumber("F_START") + 1;
            var lastFlat = wad.GetLumpNumber("F_END") - 1;
            var count = lastFlat - firstFlat + 1;

            flats = new Flat[count];

            nameToFlat = new Dictionary<string, Flat>();
            nameToNumber = new Dictionary<string, int>();

            for (var lump = firstFlat; lump <= lastFlat; lump++)
            {
                if (wad.GetLumpSize(lump) != 4096)
                {
                    continue;
                }

                var number = lump - firstFlat;
                var name = wad.LumpInfos[lump].Name;
                var flat = name != "F_SKY1" ? Dummy.GetFlat() : Dummy.GetSkyFlat();

                flats[number] = flat;
                nameToFlat[name] = flat;
                nameToNumber[name] = number;
            }

            skyFlatNumber = nameToNumber["F_SKY1"];
            skyFlat = nameToFlat["F_SKY1"];
        }

        public int GetNumber(string name)
        {
            if (nameToNumber.ContainsKey(name))
            {
                return nameToNumber[name];
            }
            else
            {
                return -1;
            }
        }

        public IEnumerator<Flat> GetEnumerator()
        {
            return ((IEnumerable<Flat>)flats).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return flats.GetEnumerator();
        }

        private static int CountLump(Wad wad, string name)
        {
            var count = 0;
            foreach (var lump in wad.LumpInfos)
            {
                if (lump.Name == name)
                {
                    count++;
                }
            }
            return count;
        }

        public int Count => flats.Length;
        public Flat this[int num] => flats[num];
        public Flat this[string name] => nameToFlat[name];
        public int SkyFlatNumber => skyFlatNumber;
        public Flat SkyFlat => skyFlat;
    }
}
