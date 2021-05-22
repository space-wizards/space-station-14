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
    public sealed class AnimationDef
    {
        private bool isTexture;
        private string endName;
        private string startName;
        private int speed;

        public AnimationDef(bool isTexture, string endName, string startName, int speed)
        {
            this.isTexture = isTexture;
            this.endName = endName;
            this.startName = startName;
            this.speed = speed;
        }

        public bool IsTexture => isTexture;
        public string EndName => endName;
        public string StartName => startName;
        public int Speed => speed;
    }
}
