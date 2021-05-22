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
    public sealed class Vertex
    {
        private static readonly int dataSize = 4;

        private Fixed x;
        private Fixed y;

        public Vertex(Fixed x, Fixed y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vertex FromData(byte[] data, int offset)
        {
            var x = BitConverter.ToInt16(data, offset);
            var y = BitConverter.ToInt16(data, offset + 2);

            return new Vertex(Fixed.FromInt(x), Fixed.FromInt(y));
        }

        public static Vertex[] FromWad(Wad wad, int lump)
        {
            var length = wad.GetLumpSize(lump);
            if (length % dataSize != 0)
            {
                throw new Exception();
            }

            var data = wad.ReadLump(lump);
            var count = length / dataSize;
            var vertices = new Vertex[count]; ;

            for (var i = 0; i < count; i++)
            {
                var offset = dataSize * i;
                vertices[i] = FromData(data, offset);
            }

            return vertices;
        }

        public Fixed X => x;
        public Fixed Y => y;
    }
}
