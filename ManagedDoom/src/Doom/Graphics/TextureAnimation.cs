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
using System.Runtime.ExceptionServices;

namespace ManagedDoom
{
    public sealed class TextureAnimation
    {
        private TextureAnimationInfo[] animations;

        public TextureAnimation(TextureLookup textures, FlatLookup flats)
        {
            try
            {
                Console.Write("Load texture animation info: ");

                var list = new List<TextureAnimationInfo>();

                foreach (var animDef in DoomInfo.TextureAnimation)
                {
                    int picNum;
                    int basePic;
                    if (animDef.IsTexture)
                    {
                        if (textures.GetNumber(animDef.StartName) == -1)
                        {
                            continue;
                        }

                        picNum = textures.GetNumber(animDef.EndName);
                        basePic = textures.GetNumber(animDef.StartName);
                    }
                    else
                    {
                        if (flats.GetNumber(animDef.StartName) == -1)
                        {
                            continue;
                        }

                        picNum = flats.GetNumber(animDef.EndName);
                        basePic = flats.GetNumber(animDef.StartName);
                    }

                    var anim = new TextureAnimationInfo(
                        animDef.IsTexture,
                        picNum,
                        basePic,
                        picNum - basePic + 1,
                        animDef.Speed);

                    if (anim.NumPics < 2)
                    {
                        throw new Exception("Bad animation cycle from " + animDef.StartName + " to " + animDef.EndName + "!");
                    }

                    list.Add(anim);
                }

                animations = list.ToArray();

                Console.WriteLine("OK");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed");
                ExceptionDispatchInfo.Throw(e);
            }
        }

        public TextureAnimationInfo[] Animations => animations;
    }
}
