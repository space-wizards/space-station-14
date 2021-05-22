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
    public abstract class MenuItem
    {
        private int skullX;
        private int skullY;
        private MenuDef next;

        private MenuItem()
        {
        }

        public MenuItem(int skullX, int skullY, MenuDef next)
        {
            this.skullX = skullX;
            this.skullY = skullY;
            this.next = next;
        }

        public int SkullX => skullX;
        public int SkullY => skullY;
        public MenuDef Next => next;
    }
}
