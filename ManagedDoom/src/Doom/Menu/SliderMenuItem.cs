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
    public class SliderMenuItem : MenuItem
    {
        private string name;
        private int itemX;
        private int itemY;

        private int sliderLength;
        private int sliderPosition;

        private Func<int> reset;
        private Action<int> action;

        public SliderMenuItem(
            string name,
            int skullX, int skullY,
            int itemX, int itemY,
            int sliderLength,
            Func<int> reset,
            Action<int> action)
            : base(skullX, skullY, null)
        {
            this.name = name;
            this.itemX = itemX;
            this.itemY = itemY;

            this.sliderLength = sliderLength;
            sliderPosition = 0;

            this.action = action;
            this.reset = reset;
        }

        public void Reset()
        {
            if (reset != null)
            {
                sliderPosition = reset();
            }
        }

        public void Up()
        {
            if (sliderPosition < SliderLength - 1)
            {
                sliderPosition++;
            }

            if (action != null)
            {
                action(sliderPosition);
            }
        }

        public void Down()
        {
            if (sliderPosition > 0)
            {
                sliderPosition--;
            }

            if (action != null)
            {
                action(sliderPosition);
            }
        }

        public string Name => name;
        public int ItemX => itemX;
        public int ItemY => itemY;

        public int SliderX => itemX;
        public int SliderY => itemY + 16;
        public int SliderLength => sliderLength;
        public int SliderPosition => sliderPosition;
    }
}
