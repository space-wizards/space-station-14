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
using System.Runtime.CompilerServices;

namespace ManagedDoom
{
    public struct Angle
    {
        public static readonly Angle Ang0 = new Angle(0x00000000);
        public static readonly Angle Ang45 = new Angle(0x20000000);
        public static readonly Angle Ang90 = new Angle(0x40000000);
        public static readonly Angle Ang180 = new Angle(0x80000000);
        public static readonly Angle Ang270 = new Angle(0xC0000000);

        private uint data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Angle(uint data)
        {
            this.data = data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Angle(int data)
        {
            this.data = (uint)data;
        }

        public static Angle FromRadian(double radian)
        {
            var data = Math.Round(0x100000000 * (radian / (2 * Math.PI)));
            return new Angle((uint)data);
        }

        public static Angle FromDegree(double degree)
        {
            var data = Math.Round(0x100000000 * (degree / 360));
            return new Angle((uint)data);
        }

        public double ToRadian()
        {
            return 2 * Math.PI * ((double)data / 0x100000000);
        }

        public double ToDegree()
        {
            return 360 * ((double)data / 0x100000000);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Angle Abs(Angle angle)
        {
            var data = (int)angle.data;
            if (data < 0)
            {
                return new Angle((uint)-data);
            }
            else
            {
                return angle;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Angle operator +(Angle a)
        {
            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Angle operator -(Angle a)
        {
            return new Angle((uint)-(int)a.data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Angle operator +(Angle a, Angle b)
        {
            return new Angle(a.data + b.data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Angle operator -(Angle a, Angle b)
        {
            return new Angle(a.data - b.data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Angle operator *(uint a, Angle b)
        {
            return new Angle(a * b.data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Angle operator *(Angle a, uint b)
        {
            return new Angle(a.data * b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Angle operator /(Angle a, uint b)
        {
            return new Angle(a.data / b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Angle a, Angle b)
        {
            return a.data == b.data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Angle a, Angle b)
        {
            return a.data != b.data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Angle a, Angle b)
        {
            return a.data < b.data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Angle a, Angle b)
        {
            return a.data > b.data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Angle a, Angle b)
        {
            return a.data <= b.data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Angle a, Angle b)
        {
            return a.data >= b.data;
        }

        public override bool Equals(object obj)
        {
            throw new NotSupportedException();
        }

        public override int GetHashCode()
        {
            return data.GetHashCode();
        }

        public override string ToString()
        {
            return ToDegree().ToString();
        }

        public uint Data
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => data;
        }
    }
}
