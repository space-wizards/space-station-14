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
    public sealed class TextureAnimationInfo
    {
        private bool isTexture;
        private int picNum;
        private int basePic;
        private int numPics;
        private int speed;

        public TextureAnimationInfo(bool isTexture, int picNum, int basePic, int numPics, int speed)
        {
            this.isTexture = isTexture;
            this.picNum = picNum;
            this.basePic = basePic;
            this.numPics = numPics;
            this.speed = speed;
        }

        public bool IsTexture => isTexture;
        public int PicNum => picNum;
        public int BasePic => basePic;
        public int NumPics => numPics;
        public int Speed => speed;
    }
}
