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
    public class SimpleMenuItem : MenuItem
    {
        private string name;
        private int itemX;
        private int itemY;
        private Action action;
        private Func<bool> selectable;

        public SimpleMenuItem(
            string name,
            int skullX, int skullY,
            int itemX, int itemY,
            Action action, MenuDef next)
            : base(skullX, skullY, next)
        {
            this.name = name;
            this.itemX = itemX;
            this.itemY = itemY;
            this.action = action;
            this.selectable = null;
        }

        public SimpleMenuItem(
            string name,
            int skullX, int skullY,
            int itemX, int itemY,
            Action action, MenuDef next, Func<bool> selectable)
            : base(skullX, skullY, next)
        {
            this.name = name;
            this.itemX = itemX;
            this.itemY = itemY;
            this.action = action;
            this.selectable = selectable;
        }

        public string Name => name;
        public int ItemX => itemX;
        public int ItemY => itemY;
        public Action Action => action;

        public bool Selectable
        {
            get
            {
                if (selectable == null)
                {
                    return true;
                }
                else
                {
                    return selectable();
                }
            }
        }
    }
}
