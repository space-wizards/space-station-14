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

namespace ManagedDoom
{
    public sealed class Sector
    {
        private static readonly int dataSize = 26;

        private int number;
        private Fixed floorHeight;
        private Fixed ceilingHeight;
        private int floorFlat;
        private int ceilingFlat;
        private int lightLevel;
        private SectorSpecial special;
        private int tag;

        // 0 = untraversed, 1, 2 = sndlines - 1.
        private int soundTraversed;

        // Thing that made a sound (or null).
        private Mobj soundTarget;

        // Mapblock bounding box for height changes.
        private int[] blockBox;

        // Origin for any sounds played by the sector.
        private Mobj soundOrigin;

        // If == validcount, already checked.
        private int validCount;

        // List of mobjs in sector.
        private Mobj thingList;

        // Thinker for reversable actions.
        private Thinker specialData;

        private LineDef[] lines;

        public Sector(
            int number,
            Fixed floorHeight,
            Fixed ceilingHeight,
            int floorFlat,
            int ceilingFlat,
            int lightLevel,
            SectorSpecial special,
            int tag)
        {
            this.number = number;
            this.floorHeight = floorHeight;
            this.ceilingHeight = ceilingHeight;
            this.floorFlat = floorFlat;
            this.ceilingFlat = ceilingFlat;
            this.lightLevel = lightLevel;
            this.special = special;
            this.tag = tag;
        }

        public static Sector FromData(byte[] data, int offset, int number, FlatLookup flats)
        {
            var floorHeight = BitConverter.ToInt16(data, offset);
            var ceilingHeight = BitConverter.ToInt16(data, offset + 2);
            var floorFlatName = DoomInterop.ToString(data, offset + 4, 8);
            var ceilingFlatName = DoomInterop.ToString(data, offset + 12, 8);
            var lightLevel = BitConverter.ToInt16(data, offset + 20);
            var special = BitConverter.ToInt16(data, offset + 22);
            var tag = BitConverter.ToInt16(data, offset + 24);

            return new Sector(
                number,
                Fixed.FromInt(floorHeight),
                Fixed.FromInt(ceilingHeight),
                flats.GetNumber(floorFlatName),
                flats.GetNumber(ceilingFlatName),
                lightLevel,
                (SectorSpecial)special,
                tag);
        }

        public static Sector[] FromWad(Wad wad, int lump, FlatLookup flats)
        {
            var length = wad.GetLumpSize(lump);
            if (length % dataSize != 0)
            {
                throw new Exception();
            }

            var data = wad.ReadLump(lump);
            var count = length / dataSize;
            var sectors = new Sector[count]; ;

            for (var i = 0; i < count; i++)
            {
                var offset = dataSize * i;
                sectors[i] = FromData(data, offset, i, flats);
            }

            return sectors;
        }

        public ThingEnumerator GetEnumerator()
        {
            return new ThingEnumerator(this);
        }



        public struct ThingEnumerator : IEnumerator<Mobj>
        {
            private Sector sector;
            private Mobj thing;
            private Mobj current;

            public ThingEnumerator(Sector sector)
            {
                this.sector = sector;
                thing = sector.thingList;
                current = null;
            }

            public bool MoveNext()
            {
                if (thing != null)
                {
                    current = thing;
                    thing = thing.SectorNext;
                    return true;
                }
                else
                {
                    current = null;
                    return false;
                }
            }

            public void Reset()
            {
                thing = sector.thingList;
                current = null;
            }

            public void Dispose()
            {
            }

            public Mobj Current => current;

            object IEnumerator.Current => throw new NotImplementedException();
        }

        public int Number => number;

        public Fixed FloorHeight
        {
            get => floorHeight;
            set => floorHeight = value;
        }

        public Fixed CeilingHeight
        {
            get => ceilingHeight;
            set => ceilingHeight = value;
        }

        public int FloorFlat
        {
            get => floorFlat;
            set => floorFlat = value;
        }

        public int CeilingFlat
        {
            get => ceilingFlat;
            set => ceilingFlat = value;
        }

        public int LightLevel
        {
            get => lightLevel;
            set => lightLevel = value;
        }

        public SectorSpecial Special
        {
            get => special;
            set => special = value;
        }

        public int Tag
        {
            get => tag;
            set => tag = value;
        }

        public int SoundTraversed
        {
            get => soundTraversed;
            set => soundTraversed = value;
        }

        public Mobj SoundTarget
        {
            get => soundTarget;
            set => soundTarget = value;
        }

        public int[] BlockBox
        {
            get => blockBox;
            set => blockBox = value;
        }

        public Mobj SoundOrigin
        {
            get => soundOrigin;
            set => soundOrigin = value;
        }

        public int ValidCount
        {
            get => validCount;
            set => validCount = value;
        }

        public Mobj ThingList
        {
            get => thingList;
            set => thingList = value;
        }

        public Thinker SpecialData
        {
            get => specialData;
            set => specialData = value;
        }

        public LineDef[] Lines
        {
            get => lines;
            set => lines = value;
        }
    }
}
