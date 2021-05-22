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
    public sealed class SpriteLookup
    {
        private SpriteDef[] spriteDefs;

        public SpriteLookup(Wad wad) : this(wad, false)
        {
        }

        public SpriteLookup(Wad wad, bool useDummy)
        {
            try
            {
                Console.Write("Load sprites: ");

                var temp = new Dictionary<string, List<SpriteInfo>>();
                for (var i = 0; i < (int)Sprite.Count; i++)
                {
                    temp.Add(DoomInfo.SpriteNames[i], new List<SpriteInfo>());
                }

                var cache = new Dictionary<int, Patch>();

                foreach (var lump in EnumerateSprites(wad))
                {
                    var name = wad.LumpInfos[lump].Name.Substring(0, 4);

                    if (!temp.ContainsKey(name))
                    {
                        continue;
                    }

                    var list = temp[name];

                    {
                        var frame = wad.LumpInfos[lump].Name[4] - 'A';
                        var rotation = wad.LumpInfos[lump].Name[5] - '0';

                        while (list.Count < frame + 1)
                        {
                            list.Add(new SpriteInfo());
                        }

                        if (rotation == 0)
                        {
                            for (var i = 0; i < 8; i++)
                            {
                                if (list[frame].Patches[i] == null)
                                {
                                    list[frame].Patches[i] = CachedRead(lump, wad, cache, useDummy);
                                    list[frame].Flip[i] = false;
                                }
                            }
                        }
                        else
                        {
                            if (list[frame].Patches[rotation - 1] == null)
                            {
                                list[frame].Patches[rotation - 1] = CachedRead(lump, wad, cache, useDummy);
                                list[frame].Flip[rotation - 1] = false;
                            }
                        }
                    }

                    if (wad.LumpInfos[lump].Name.Length == 8)
                    {
                        var frame = wad.LumpInfos[lump].Name[6] - 'A';
                        var rotation = wad.LumpInfos[lump].Name[7] - '0';

                        while (list.Count < frame + 1)
                        {
                            list.Add(new SpriteInfo());
                        }

                        if (rotation == 0)
                        {
                            for (var i = 0; i < 8; i++)
                            {
                                if (list[frame].Patches[i] == null)
                                {
                                    list[frame].Patches[i] = CachedRead(lump, wad, cache, useDummy);
                                    list[frame].Flip[i] = true;
                                }
                            }
                        }
                        else
                        {
                            if (list[frame].Patches[rotation - 1] == null)
                            {
                                list[frame].Patches[rotation - 1] = CachedRead(lump, wad, cache, useDummy);
                                list[frame].Flip[rotation - 1] = true;
                            }
                        }
                    }
                }

                spriteDefs = new SpriteDef[(int)Sprite.Count];
                for (var i = 0; i < spriteDefs.Length; i++)
                {
                    var list = temp[DoomInfo.SpriteNames[i]];

                    var frames = new SpriteFrame[list.Count];
                    for (var j = 0; j < frames.Length; j++)
                    {
                        list[j].CheckCompletion();

                        var frame = new SpriteFrame(list[j].HasRotation(), list[j].Patches, list[j].Flip);
                        frames[j] = frame;
                    }

                    spriteDefs[i] = new SpriteDef(frames);
                }

                Console.WriteLine("OK (" + cache.Count + " sprites)");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed");
                ExceptionDispatchInfo.Throw(e);
            }
        }

        private static IEnumerable<int> EnumerateSprites(Wad wad)
        {
            var spriteSection = false;

            for (var lump = wad.LumpInfos.Count - 1; lump >= 0; lump--)
            {
                var name = wad.LumpInfos[lump].Name;

                if (name.StartsWith("S"))
                {
                    if (name.EndsWith("_END"))
                    {
                        spriteSection = true;
                        continue;
                    }
                    else if (name.EndsWith("_START"))
                    {
                        spriteSection = false;
                        continue;
                    }
                }

                if (spriteSection)
                {
                    if (wad.LumpInfos[lump].Size > 0)
                    {
                        yield return lump;
                    }
                }
            }
        }

        private static Patch CachedRead(int lump, Wad wad, Dictionary<int, Patch> cache, bool useDummy)
        {
            if (useDummy)
            {
                return Dummy.GetPatch();
            }

            if (!cache.ContainsKey(lump))
            {
                var name = wad.LumpInfos[lump].Name;
                cache.Add(lump, Patch.FromData(name, wad.ReadLump(lump)));
            }

            return cache[lump];
        }


        private class SpriteInfo
        {
            public Patch[] Patches;
            public bool[] Flip;

            public SpriteInfo()
            {
                Patches = new Patch[8];
                Flip = new bool[8];
            }

            public void CheckCompletion()
            {
                for (var i = 0; i < Patches.Length; i++)
                {
                    if (Patches[i] == null)
                    {
                        throw new Exception("Missing sprite!");
                    }
                }
            }

            public bool HasRotation()
            {
                for (var i = 1; i < Patches.Length; i++)
                {
                    if (Patches[i] != Patches[0])
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public SpriteDef this[Sprite sprite]
        {
            get
            {
                return spriteDefs[(int)sprite];
            }
        }
    }
}
