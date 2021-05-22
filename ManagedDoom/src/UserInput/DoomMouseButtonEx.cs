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
    public static class DoomMouseButtonEx
    {
        public static string ToString(DoomMouseButton button)
        {
            switch (button)
            {
                case DoomMouseButton.Mouse1:
                    return "mouse1";
                case DoomMouseButton.Mouse2:
                    return "mouse2";
                case DoomMouseButton.Mouse3:
                    return "mouse3";
                case DoomMouseButton.Mouse4:
                    return "mouse4";
                case DoomMouseButton.Mouse5:
                    return "mouse5";
                default:
                    return "unknown";
            }
        }

        public static DoomMouseButton Parse(string value)
        {
            switch (value)
            {
                case "mouse1":
                    return DoomMouseButton.Mouse1;
                case "mouse2":
                    return DoomMouseButton.Mouse2;
                case "mouse3":
                    return DoomMouseButton.Mouse3;
                case "mouse4":
                    return DoomMouseButton.Mouse4;
                case "mouse5":
                    return DoomMouseButton.Mouse5;
                default:
                    return DoomMouseButton.Unknown;
            }
        }
    }
}
