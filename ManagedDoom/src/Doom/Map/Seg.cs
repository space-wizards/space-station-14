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

namespace ManagedDoom
{
    public sealed class Seg
    {
        private static readonly int dataSize = 12;

        private Vertex vertex1;
        private Vertex vertex2;
        private Fixed offset;
        private Angle angle;
        private SideDef sideDef;
        private LineDef lineDef;
        private Sector frontSector;
        private Sector backSector;

        public Seg(
            Vertex vertex1,
            Vertex vertex2,
            Fixed offset,
            Angle angle,
            SideDef sideDef,
            LineDef lineDef,
            Sector frontSector,
            Sector backSector)
        {
            this.vertex1 = vertex1;
            this.vertex2 = vertex2;
            this.offset = offset;
            this.angle = angle;
            this.sideDef = sideDef;
            this.lineDef = lineDef;
            this.frontSector = frontSector;
            this.backSector = backSector;
        }

        public static Seg FromData(byte[] data, int offset, Vertex[] vertices, LineDef[] lines)
        {
            var vertex1Number = BitConverter.ToInt16(data, offset);
            var vertex2Number = BitConverter.ToInt16(data, offset + 2);
            var angle = BitConverter.ToInt16(data, offset + 4);
            var lineNumber = BitConverter.ToInt16(data, offset + 6);
            var side = BitConverter.ToInt16(data, offset + 8);
            var segOffset = BitConverter.ToInt16(data, offset + 10);

            var lineDef = lines[lineNumber];
            var frontSide = side == 0 ? lineDef.FrontSide : lineDef.BackSide;
            var backSide = side == 0 ? lineDef.BackSide : lineDef.FrontSide;

            return new Seg(
                vertices[vertex1Number],
                vertices[vertex2Number],
                Fixed.FromInt(segOffset),
                new Angle((uint)angle << 16),
                frontSide,
                lineDef,
                frontSide.Sector,
                (lineDef.Flags & LineFlags.TwoSided) != 0 ? backSide?.Sector : null);
        }

        public static Seg[] FromWad(Wad wad, int lump, Vertex[] vertices, LineDef[] lines)
        {
            var length = wad.GetLumpSize(lump);
            if (length % Seg.dataSize != 0)
            {
                throw new Exception();
            }

            var data = wad.ReadLump(lump);
            var count = length / Seg.dataSize;
            var segs = new Seg[count]; ;

            for (var i = 0; i < count; i++)
            {
                var offset = Seg.dataSize * i;
                segs[i] = Seg.FromData(data, offset, vertices, lines);
            }

            return segs;
        }

        public Vertex Vertex1 => vertex1;
        public Vertex Vertex2 => vertex2;
        public Fixed Offset => offset;
        public Angle Angle => angle;
        public SideDef SideDef => sideDef;
        public LineDef LineDef => lineDef;
        public Sector FrontSector => frontSector;
        public Sector BackSector => backSector;
    }
}
