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

namespace ManagedDoom
{
    public static class WorldMap
    {
        public static readonly IReadOnlyList<IReadOnlyList<Point>> Locations = new Point[][]
        {
            // Episode 0 world map.
            new Point[]
            {
                new Point(185, 164), // location of level 0 (CJ)
	            new Point(148, 143), // location of level 1 (CJ)
	            new Point(69, 122),  // location of level 2 (CJ)
	            new Point(209, 102), // location of level 3 (CJ)
	            new Point(116, 89),  // location of level 4 (CJ)
	            new Point(166, 55),  // location of level 5 (CJ)
	            new Point(71, 56),   // location of level 6 (CJ)
	            new Point(135, 29),  // location of level 7 (CJ)
	            new Point(71, 24)    // location of level 8 (CJ)
            },

            // Episode 1 world map should go here.
            new Point[]
            {
                new Point(254, 25),  // location of level 0 (CJ)
	            new Point(97, 50),   // location of level 1 (CJ)
	            new Point(188, 64),  // location of level 2 (CJ)
	            new Point(128, 78),  // location of level 3 (CJ)
	            new Point(214, 92),  // location of level 4 (CJ)
	            new Point(133, 130), // location of level 5 (CJ)
	            new Point(208, 136), // location of level 6 (CJ)
	            new Point(148, 140), // location of level 7 (CJ)
	            new Point(235, 158)  // location of level 8 (CJ)
            },

            // Episode 2 world map should go here.
            new Point[]
            {
                new Point(156, 168), // location of level 0 (CJ)
                new Point(48, 154),  // location of level 1 (CJ)
                new Point(174, 95),  // location of level 2 (CJ)
                new Point(265, 75),  // location of level 3 (CJ)
                new Point(130, 48),  // location of level 4 (CJ)
                new Point(279, 23),  // location of level 5 (CJ)
                new Point(198, 48),  // location of level 6 (CJ)
                new Point(140, 25),  // location of level 7 (CJ)
                new Point(281, 136)  // location of level 8 (CJ)
            }
        };



        public class Point
        {
            private int x;
            private int y;

            public Point(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public int X => x;
            public int Y => y;
        }
    }
}
