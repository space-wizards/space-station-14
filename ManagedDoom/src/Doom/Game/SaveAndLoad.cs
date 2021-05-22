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
using System.IO;

namespace ManagedDoom
{
    /// <summary>
    /// Vanilla-compatible save and load, full of messy binary handling code.
    /// </summary>
    public static class SaveAndLoad
    {
        public static readonly int DescriptionSize = 24;

        private static readonly int versionSize = 16;
        private static readonly int saveBufferSize = 360 * 1024;

        private enum ThinkerClass
        {
            End,
            Mobj
        }

        private enum SpecialClass
        {
            Ceiling,
            Door,
            Floor,
            Plat,
            Flash,
            Strobe,
            Glow,
            EndSpecials
        }

        public static void Save(DoomGame game, string description, string path)
        {
            var sg = new SaveGame(description);
            sg.Save(game, path);
        }

        public static void Load(DoomGame game, string path)
        {
            var options = game.Options;
            game.InitNew(options.Skill, options.Episode, options.Map);

            var lg = new LoadGame(File.ReadAllBytes(path));
            lg.Load(game);
        }



        ////////////////////////////////////////////////////////////
        // Save game
        ////////////////////////////////////////////////////////////
        
        private class SaveGame
        {
            private byte[] data;
            private int ptr;

            public SaveGame(string description)
            {
                data = new byte[saveBufferSize];
                ptr = 0;

                WriteDescription(description);
                WriteVersion();
            }

            private void WriteDescription(string description)
            {
                for (var i = 0; i < description.Length; i++)
                {
                    data[i] = (byte)description[i];
                }
                ptr += DescriptionSize;
            }

            private void WriteVersion()
            {
                var version = "version 109";
                for (var i = 0; i < version.Length; i++)
                {
                    data[ptr + i] = (byte)version[i];
                }
                ptr += versionSize;
            }

            public void Save(DoomGame game, string path)
            {
                var options = game.World.Options;
                data[ptr++] = (byte)options.Skill;
                data[ptr++] = (byte)options.Episode;
                data[ptr++] = (byte)options.Map;
                for (var i = 0; i < Player.MaxPlayerCount; i++)
                {
                    data[ptr++] = options.Players[i].InGame ? (byte)1 : (byte)0;
                }

                data[ptr++] = (byte)(game.World.LevelTime >> 16);
                data[ptr++] = (byte)(game.World.LevelTime >> 8);
                data[ptr++] = (byte)(game.World.LevelTime);

                ArchivePlayers(game.World);
                ArchiveWorld(game.World);
                ArchiveThinkers(game.World);
                ArchiveSpecials(game.World);

                data[ptr++] = 0x1d;

                using (var writer = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    writer.Write(data, 0, ptr);
                }
            }

            private void PadPointer()
            {
                ptr += (4 - (ptr & 3)) & 3;
            }

            private void ArchivePlayers(World world)
            {
                var players = world.Options.Players;
                for (var i = 0; i < Player.MaxPlayerCount; i++)
                {
                    if (!players[i].InGame)
                    {
                        continue;
                    }

                    PadPointer();

                    ptr = ArchivePlayer(players[i], data, ptr);
                }
            }

            private void ArchiveWorld(World world)
            {
                // Do sectors.
                var sectors = world.Map.Sectors;
                for (var i = 0; i < sectors.Length; i++)
                {
                    ptr = ArchiveSector(sectors[i], data, ptr);
                }

                // Do lines.
                var lines = world.Map.Lines;
                for (var i = 0; i < lines.Length; i++)
                {
                    ptr = ArchiveLine(lines[i], data, ptr);
                }
            }

            private void ArchiveThinkers(World world)
            {
                var thinkers = world.Thinkers;

                // Read in saved thinkers.
                foreach (var thinker in thinkers)
                {
                    var mobj = thinker as Mobj;
                    if (mobj != null)
                    {
                        data[ptr++] = (byte)ThinkerClass.Mobj;
                        PadPointer();

                        WriteThinkerState(data, ptr + 8, mobj.ThinkerState);
                        Write(data, ptr + 12, mobj.X.Data);
                        Write(data, ptr + 16, mobj.Y.Data);
                        Write(data, ptr + 20, mobj.Z.Data);
                        Write(data, ptr + 32, mobj.Angle.Data);
                        Write(data, ptr + 36, (int)mobj.Sprite);
                        Write(data, ptr + 40, mobj.Frame);
                        Write(data, ptr + 56, mobj.FloorZ.Data);
                        Write(data, ptr + 60, mobj.CeilingZ.Data);
                        Write(data, ptr + 64, mobj.Radius.Data);
                        Write(data, ptr + 68, mobj.Height.Data);
                        Write(data, ptr + 72, mobj.MomX.Data);
                        Write(data, ptr + 76, mobj.MomY.Data);
                        Write(data, ptr + 80, mobj.MomZ.Data);
                        Write(data, ptr + 88, (int)mobj.Type);
                        Write(data, ptr + 96, mobj.Tics);
                        Write(data, ptr + 100, mobj.State.Number);
                        Write(data, ptr + 104, (int)mobj.Flags);
                        Write(data, ptr + 108, mobj.Health);
                        Write(data, ptr + 112, (int)mobj.MoveDir);
                        Write(data, ptr + 116, mobj.MoveCount);
                        Write(data, ptr + 124, mobj.ReactionTime);
                        Write(data, ptr + 128, mobj.Threshold);
                        if (mobj.Player == null)
                        {
                            Write(data, ptr + 132, 0);
                        }
                        else
                        {
                            Write(data, ptr + 132, mobj.Player.Number + 1);
                        }
                        Write(data, ptr + 136, mobj.LastLook);
                        if (mobj.SpawnPoint == null)
                        {
                            Write(data, ptr + 140, (short)0);
                            Write(data, ptr + 142, (short)0);
                            Write(data, ptr + 144, (short)0);
                            Write(data, ptr + 146, (short)0);
                            Write(data, ptr + 148, (short)0);
                        }
                        else
                        {
                            Write(data, ptr + 140, (short)mobj.SpawnPoint.X.ToIntFloor());
                            Write(data, ptr + 142, (short)mobj.SpawnPoint.Y.ToIntFloor());
                            Write(data, ptr + 144, (short)Math.Round(mobj.SpawnPoint.Angle.ToDegree()));
                            Write(data, ptr + 146, (short)mobj.SpawnPoint.Type);
                            Write(data, ptr + 148, (short)mobj.SpawnPoint.Flags);
                        }
                        ptr += 154;
                    }
                }

                data[ptr++] = (byte)ThinkerClass.End;
            }

            private void ArchiveSpecials(World world)
            {
                var thinkers = world.Thinkers;
                var sa = world.SectorAction;

                // Read in saved thinkers.
                foreach (var thinker in thinkers)
                {
                    if (thinker.ThinkerState == ThinkerState.InStasis)
                    {
                        var ceiling = thinker as CeilingMove;
                        if (sa.CheckActiveCeiling(ceiling))
                        {
                            data[ptr++] = (byte)SpecialClass.Ceiling;
                            PadPointer();
                            WriteThinkerState(data, ptr + 8, ceiling.ThinkerState);
                            Write(data, ptr + 12, (int)ceiling.Type);
                            Write(data, ptr + 16, ceiling.Sector.Number);
                            Write(data, ptr + 20, ceiling.BottomHeight.Data);
                            Write(data, ptr + 24, ceiling.TopHeight.Data);
                            Write(data, ptr + 28, ceiling.Speed.Data);
                            Write(data, ptr + 32, ceiling.Crush ? 1 : 0);
                            Write(data, ptr + 36, ceiling.Direction);
                            Write(data, ptr + 40, ceiling.Tag);
                            Write(data, ptr + 44, ceiling.OldDirection);
                            ptr += 48;
                        }
                        continue;
                    }

                    {
                        var ceiling = thinker as CeilingMove;
                        if (ceiling != null)
                        {
                            data[ptr++] = (byte)SpecialClass.Ceiling;
                            PadPointer();
                            WriteThinkerState(data, ptr + 8, ceiling.ThinkerState);
                            Write(data, ptr + 12, (int)ceiling.Type);
                            Write(data, ptr + 16, ceiling.Sector.Number);
                            Write(data, ptr + 20, ceiling.BottomHeight.Data);
                            Write(data, ptr + 24, ceiling.TopHeight.Data);
                            Write(data, ptr + 28, ceiling.Speed.Data);
                            Write(data, ptr + 32, ceiling.Crush ? 1 : 0);
                            Write(data, ptr + 36, ceiling.Direction);
                            Write(data, ptr + 40, ceiling.Tag);
                            Write(data, ptr + 44, ceiling.OldDirection);
                            ptr += 48;
                            continue;
                        }
                    }

                    {
                        var door = thinker as VerticalDoor;
                        if (door != null)
                        {
                            data[ptr++] = (byte)SpecialClass.Door;
                            PadPointer();
                            WriteThinkerState(data, ptr + 8, door.ThinkerState);
                            Write(data, ptr + 12, (int)door.Type);
                            Write(data, ptr + 16, door.Sector.Number);
                            Write(data, ptr + 20, door.TopHeight.Data);
                            Write(data, ptr + 24, door.Speed.Data);
                            Write(data, ptr + 28, door.Direction);
                            Write(data, ptr + 32, door.TopWait);
                            Write(data, ptr + 36, door.TopCountDown);
                            ptr += 40;
                            continue;
                        }
                    }

                    {
                        var floor = thinker as FloorMove;
                        if (floor != null)
                        {
                            data[ptr++] = (byte)SpecialClass.Floor;
                            PadPointer();
                            WriteThinkerState(data, ptr + 8, floor.ThinkerState);
                            Write(data, ptr + 12, (int)floor.Type);
                            Write(data, ptr + 16, floor.Crush ? 1 : 0);
                            Write(data, ptr + 20, floor.Sector.Number);
                            Write(data, ptr + 24, floor.Direction);
                            Write(data, ptr + 28, (int)floor.NewSpecial);
                            Write(data, ptr + 32, floor.Texture);
                            Write(data, ptr + 36, floor.FloorDestHeight.Data);
                            Write(data, ptr + 40, floor.Speed.Data);
                            ptr += 44;
                            continue;
                        }
                    }

                    {
                        var plat = thinker as Platform;
                        if (plat != null)
                        {
                            data[ptr++] = (byte)SpecialClass.Plat;
                            PadPointer();
                            WriteThinkerState(data, ptr + 8, plat.ThinkerState);
                            Write(data, ptr + 12, plat.Sector.Number);
                            Write(data, ptr + 16, plat.Speed.Data);
                            Write(data, ptr + 20, plat.Low.Data);
                            Write(data, ptr + 24, plat.High.Data);
                            Write(data, ptr + 28, plat.Wait);
                            Write(data, ptr + 32, plat.Count);
                            Write(data, ptr + 36, (int)plat.Status);
                            Write(data, ptr + 40, (int)plat.OldStatus);
                            Write(data, ptr + 44, plat.Crush ? 1 : 0);
                            Write(data, ptr + 48, plat.Tag);
                            Write(data, ptr + 52, (int)plat.Type);
                            ptr += 56;
                            continue;
                        }
                    }

                    {
                        var flash = thinker as LightFlash;
                        if (flash != null)
                        {
                            data[ptr++] = (byte)SpecialClass.Flash;
                            PadPointer();
                            WriteThinkerState(data, ptr + 8, flash.ThinkerState);
                            Write(data, ptr + 12, flash.Sector.Number);
                            Write(data, ptr + 16, flash.Count);
                            Write(data, ptr + 20, flash.MaxLight);
                            Write(data, ptr + 24, flash.MinLight);
                            Write(data, ptr + 28, flash.MaxTime);
                            Write(data, ptr + 32, flash.MinTime);
                            ptr += 36;
                            continue;
                        }
                    }

                    {
                        var strobe = thinker as StrobeFlash;
                        if (strobe != null)
                        {
                            data[ptr++] = (byte)SpecialClass.Strobe;
                            PadPointer();
                            WriteThinkerState(data, ptr + 8, strobe.ThinkerState);
                            Write(data, ptr + 12, strobe.Sector.Number);
                            Write(data, ptr + 16, strobe.Count);
                            Write(data, ptr + 20, strobe.MinLight);
                            Write(data, ptr + 24, strobe.MaxLight);
                            Write(data, ptr + 28, strobe.DarkTime);
                            Write(data, ptr + 32, strobe.BrightTime);
                            ptr += 36;
                            continue;
                        }
                    }

                    {
                        var glow = thinker as GlowingLight;
                        if (glow != null)
                        {
                            data[ptr++] = (byte)SpecialClass.Glow;
                            PadPointer();
                            WriteThinkerState(data, ptr + 8, glow.ThinkerState);
                            Write(data, ptr + 12, glow.Sector.Number);
                            Write(data, ptr + 16, glow.MinLight);
                            Write(data, ptr + 20, glow.MaxLight);
                            Write(data, ptr + 24, glow.Direction);
                            ptr += 28;
                            continue;
                        }
                    }
                }

                data[ptr++] = (byte)SpecialClass.EndSpecials;
            }

            private static int ArchivePlayer(Player player, byte[] data, int p)
            {
                Write(data, p + 4, (int)player.PlayerState);
                Write(data, p + 16, player.ViewZ.Data);
                Write(data, p + 20, player.ViewHeight.Data);
                Write(data, p + 24, player.DeltaViewHeight.Data);
                Write(data, p + 28, player.Bob.Data);
                Write(data, p + 32, player.Health);
                Write(data, p + 36, player.ArmorPoints);
                Write(data, p + 40, player.ArmorType);
                for (var i = 0; i < (int)PowerType.Count; i++)
                {
                    Write(data, p + 44 + 4 * i, player.Powers[i]);
                }
                for (var i = 0; i < (int)PowerType.Count; i++)
                {
                    Write(data, p + 68 + 4 * i, player.Cards[i] ? 1 : 0);
                }
                Write(data, p + 92, player.Backpack ? 1 : 0);
                for (var i = 0; i < Player.MaxPlayerCount; i++)
                {
                    Write(data, p + 96 + 4 * i, player.Frags[i]);
                }
                Write(data, p + 112, (int)player.ReadyWeapon);
                Write(data, p + 116, (int)player.PendingWeapon);
                for (var i = 0; i < (int)WeaponType.Count; i++)
                {
                    Write(data, p + 120 + 4 * i, player.WeaponOwned[i] ? 1 : 0);
                }
                for (var i = 0; i < (int)AmmoType.Count; i++)
                {
                    Write(data, p + 156 + 4 * i, player.Ammo[i]);
                }
                for (var i = 0; i < (int)AmmoType.Count; i++)
                {
                    Write(data, p + 172 + 4 * i, player.MaxAmmo[i]);
                }
                Write(data, p + 188, player.AttackDown ? 1 : 0);
                Write(data, p + 192, player.UseDown ? 1 : 0);
                Write(data, p + 196, (int)player.Cheats);
                Write(data, p + 200, player.Refire);
                Write(data, p + 204, player.KillCount);
                Write(data, p + 208, player.ItemCount);
                Write(data, p + 212, player.SecretCount);
                Write(data, p + 220, player.DamageCount);
                Write(data, p + 224, player.BonusCount);
                Write(data, p + 232, player.ExtraLight);
                Write(data, p + 236, player.FixedColorMap);
                Write(data, p + 240, player.ColorMap);
                for (var i = 0; i < (int)PlayerSprite.Count; i++)
                {
                    if (player.PlayerSprites[i].State == null)
                    {
                        Write(data, p + 244 + 16 * i, 0);
                    }
                    else
                    {
                        Write(data, p + 244 + 16 * i, player.PlayerSprites[i].State.Number);
                    }
                    Write(data, p + 244 + 16 * i + 4, player.PlayerSprites[i].Tics);
                    Write(data, p + 244 + 16 * i + 8, player.PlayerSprites[i].Sx.Data);
                    Write(data, p + 244 + 16 * i + 12, player.PlayerSprites[i].Sy.Data);
                }
                Write(data, p + 276, player.DidSecret ? 1 : 0);

                return p + 280;
            }

            private static int ArchiveSector(Sector sector, byte[] data, int p)
            {
                Write(data, p, (short)(sector.FloorHeight.ToIntFloor()));
                Write(data, p + 2, (short)(sector.CeilingHeight.ToIntFloor()));
                Write(data, p + 4, (short)sector.FloorFlat);
                Write(data, p + 6, (short)sector.CeilingFlat);
                Write(data, p + 8, (short)sector.LightLevel);
                Write(data, p + 10, (short)sector.Special);
                Write(data, p + 12, (short)sector.Tag);
                return p + 14;
            }

            private static int ArchiveLine(LineDef line, byte[] data, int p)
            {
                Write(data, p, (short)line.Flags);
                Write(data, p + 2, (short)line.Special);
                Write(data, p + 4, (short)line.Tag);
                p += 6;

                if (line.FrontSide != null)
                {
                    var side = line.FrontSide;
                    Write(data, p, (short)side.TextureOffset.ToIntFloor());
                    Write(data, p + 2, (short)side.RowOffset.ToIntFloor());
                    Write(data, p + 4, (short)side.TopTexture);
                    Write(data, p + 6, (short)side.BottomTexture);
                    Write(data, p + 8, (short)side.MiddleTexture);
                    p += 10;
                }

                if (line.BackSide != null)
                {
                    var side = line.BackSide;
                    Write(data, p, (short)side.TextureOffset.ToIntFloor());
                    Write(data, p + 2, (short)side.RowOffset.ToIntFloor());
                    Write(data, p + 4, (short)side.TopTexture);
                    Write(data, p + 6, (short)side.BottomTexture);
                    Write(data, p + 8, (short)side.MiddleTexture);
                    p += 10;
                }

                return p;
            }

            private static void Write(byte[] data, int p, int value)
            {
                data[p] = (byte)value;
                data[p + 1] = (byte)(value >> 8);
                data[p + 2] = (byte)(value >> 16);
                data[p + 3] = (byte)(value >> 24);
            }

            private static void Write(byte[] data, int p, uint value)
            {
                data[p] = (byte)value;
                data[p + 1] = (byte)(value >> 8);
                data[p + 2] = (byte)(value >> 16);
                data[p + 3] = (byte)(value >> 24);
            }

            private static void Write(byte[] data, int p, short value)
            {
                data[p] = (byte)value;
                data[p + 1] = (byte)(value >> 8);
            }

            private static void WriteThinkerState(byte[] data, int p, ThinkerState state)
            {
                switch (state)
                {
                    case ThinkerState.InStasis:
                        Write(data, p, 0);
                        break;
                    default:
                        Write(data, p, 1);
                        break;
                }
            }
        }



        ////////////////////////////////////////////////////////////
        // Load game
        ////////////////////////////////////////////////////////////

        private class LoadGame
        {
            private byte[] data;
            private int ptr;

            public LoadGame(byte[] data)
            {
                this.data = data;
                ptr = 0;

                ReadDescription();

                var version = ReadVersion();
                if (version != "VERSION 109")
                {
                    throw new Exception("Unsupported version!");
                }
            }

            public void Load(DoomGame game)
            {
                var options = game.World.Options;
                options.Skill = (GameSkill)data[ptr++];
                options.Episode = data[ptr++];
                options.Map = data[ptr++];
                for (var i = 0; i < Player.MaxPlayerCount; i++)
                {
                    options.Players[i].InGame = data[ptr++] != 0;
                }

                game.InitNew(options.Skill, options.Episode, options.Map);

                var a = data[ptr++];
                var b = data[ptr++];
                var c = data[ptr++];
                var levelTime = (a << 16) + (b << 8) + c;

                UnArchivePlayers(game.World);
                UnArchiveWorld(game.World);
                UnArchiveThinkers(game.World);
                UnArchiveSpecials(game.World);

                if (data[ptr] != 0x1d)
                {
                    throw new Exception("Bad savegame!");
                }

                game.World.LevelTime = levelTime;

                options.Sound.SetListener(game.World.ConsolePlayer.Mobj);
            }

            private void PadPointer()
            {
                ptr += (4 - (ptr & 3)) & 3;
            }

            private string ReadDescription()
            {
                var value = DoomInterop.ToString(data, ptr, DescriptionSize);
                ptr += DescriptionSize;
                return value;
            }

            private string ReadVersion()
            {
                var value = DoomInterop.ToString(data, ptr, versionSize);
                ptr += versionSize;
                return value;
            }

            private void UnArchivePlayers(World world)
            {
                var players = world.Options.Players;
                for (var i = 0; i < Player.MaxPlayerCount; i++)
                {
                    if (!players[i].InGame)
                    {
                        continue;
                    }

                    PadPointer();

                    ptr = UnArchivePlayer(players[i], data, ptr);
                }
            }

            private void UnArchiveWorld(World world)
            {
                // Do sectors.
                var sectors = world.Map.Sectors;
                for (var i = 0; i < sectors.Length; i++)
                {
                    ptr = UnArchiveSector(sectors[i], data, ptr);
                }

                // Do lines.
                var lines = world.Map.Lines;
                for (var i = 0; i < lines.Length; i++)
                {
                    ptr = UnArchiveLine(lines[i], data, ptr);
                }
            }

            private void UnArchiveThinkers(World world)
            {
                var thinkers = world.Thinkers;
                var ta = world.ThingAllocation;

                // Remove all the current thinkers.
                foreach (var thinker in thinkers)
                {
                    var mobj = thinker as Mobj;
                    if (mobj != null)
                    {
                        ta.RemoveMobj(mobj);
                    }
                }
                thinkers.Reset();

                // Read in saved thinkers.
                while (true)
                {
                    var tclass = (ThinkerClass)data[ptr++];
                    switch (tclass)
                    {
                        case ThinkerClass.End:
                            // End of list.
                            return;

                        case ThinkerClass.Mobj:
                            PadPointer();
                            var mobj = new Mobj(world);
                            mobj.ThinkerState = ReadThinkerState(data, ptr + 8);
                            mobj.X = new Fixed(BitConverter.ToInt32(data, ptr + 12));
                            mobj.Y = new Fixed(BitConverter.ToInt32(data, ptr + 16));
                            mobj.Z = new Fixed(BitConverter.ToInt32(data, ptr + 20));
                            mobj.Angle = new Angle(BitConverter.ToInt32(data, ptr + 32));
                            mobj.Sprite = (Sprite)BitConverter.ToInt32(data, ptr + 36);
                            mobj.Frame = BitConverter.ToInt32(data, ptr + 40);
                            mobj.FloorZ = new Fixed(BitConverter.ToInt32(data, ptr + 56));
                            mobj.CeilingZ = new Fixed(BitConverter.ToInt32(data, ptr + 60));
                            mobj.Radius = new Fixed(BitConverter.ToInt32(data, ptr + 64));
                            mobj.Height = new Fixed(BitConverter.ToInt32(data, ptr + 68));
                            mobj.MomX = new Fixed(BitConverter.ToInt32(data, ptr + 72));
                            mobj.MomY = new Fixed(BitConverter.ToInt32(data, ptr + 76));
                            mobj.MomZ = new Fixed(BitConverter.ToInt32(data, ptr + 80));
                            mobj.Type = (MobjType)BitConverter.ToInt32(data, ptr + 88);
                            mobj.Info = DoomInfo.MobjInfos[(int)mobj.Type];
                            mobj.Tics = BitConverter.ToInt32(data, ptr + 96);
                            mobj.State = DoomInfo.States[BitConverter.ToInt32(data, ptr + 100)];
                            mobj.Flags = (MobjFlags)BitConverter.ToInt32(data, ptr + 104);
                            mobj.Health = BitConverter.ToInt32(data, ptr + 108);
                            mobj.MoveDir = (Direction)BitConverter.ToInt32(data, ptr + 112);
                            mobj.MoveCount = BitConverter.ToInt32(data, ptr + 116);
                            mobj.ReactionTime = BitConverter.ToInt32(data, ptr + 124);
                            mobj.Threshold = BitConverter.ToInt32(data, ptr + 128);
                            var playerNumber = BitConverter.ToInt32(data, ptr + 132);
                            if (playerNumber != 0)
                            {
                                mobj.Player = world.Options.Players[playerNumber - 1];
                                mobj.Player.Mobj = mobj;
                            }
                            mobj.LastLook = BitConverter.ToInt32(data, ptr + 136);
                            mobj.SpawnPoint = new MapThing(
                                Fixed.FromInt(BitConverter.ToInt16(data, ptr + 140)),
                                Fixed.FromInt(BitConverter.ToInt16(data, ptr + 142)),
                                new Angle(Angle.Ang45.Data * (uint)(BitConverter.ToInt16(data, ptr + 144) / 45)),
                                BitConverter.ToInt16(data, ptr + 146),
                                (ThingFlags)BitConverter.ToInt16(data, ptr + 148));
                            ptr += 154;

                            world.ThingMovement.SetThingPosition(mobj);
                            // mobj.FloorZ = mobj.Subsector.Sector.FloorHeight;
                            // mobj.CeilingZ = mobj.Subsector.Sector.CeilingHeight;
                            thinkers.Add(mobj);
                            break;

                        default:
                            throw new Exception("Unknown thinker class in savegame!");
                    }
                }
            }

            private void UnArchiveSpecials(World world)
            {
                var thinkers = world.Thinkers;
                var sa = world.SectorAction;

                // Read in saved thinkers.
                while (true)
                {
                    var tclass = (SpecialClass)data[ptr++];
                    switch (tclass)
                    {
                        case SpecialClass.EndSpecials:
                            // End of list.
                            return;

                        case SpecialClass.Ceiling:
                            PadPointer();
                            var ceiling = new CeilingMove(world);
                            ceiling.ThinkerState = ReadThinkerState(data, ptr + 8);
                            ceiling.Type = (CeilingMoveType)BitConverter.ToInt32(data, ptr + 12);
                            ceiling.Sector = world.Map.Sectors[BitConverter.ToInt32(data, ptr + 16)];
                            ceiling.Sector.SpecialData = ceiling;
                            ceiling.BottomHeight = new Fixed(BitConverter.ToInt32(data, ptr + 20));
                            ceiling.TopHeight = new Fixed(BitConverter.ToInt32(data, ptr + 24));
                            ceiling.Speed = new Fixed(BitConverter.ToInt32(data, ptr + 28));
                            ceiling.Crush = BitConverter.ToInt32(data, ptr + 32) != 0;
                            ceiling.Direction = BitConverter.ToInt32(data, ptr + 36);
                            ceiling.Tag = BitConverter.ToInt32(data, ptr + 40);
                            ceiling.OldDirection = BitConverter.ToInt32(data, ptr + 44);
                            ptr += 48;

                            thinkers.Add(ceiling);
                            sa.AddActiveCeiling(ceiling);
                            break;

                        case SpecialClass.Door:
                            PadPointer();
                            var door = new VerticalDoor(world);
                            door.ThinkerState = ReadThinkerState(data, ptr + 8);
                            door.Type = (VerticalDoorType)BitConverter.ToInt32(data, ptr + 12);
                            door.Sector = world.Map.Sectors[BitConverter.ToInt32(data, ptr + 16)];
                            door.Sector.SpecialData = door;
                            door.TopHeight = new Fixed(BitConverter.ToInt32(data, ptr + 20));
                            door.Speed = new Fixed(BitConverter.ToInt32(data, ptr + 24));
                            door.Direction = BitConverter.ToInt32(data, ptr + 28);
                            door.TopWait = BitConverter.ToInt32(data, ptr + 32);
                            door.TopCountDown = BitConverter.ToInt32(data, ptr + 36);
                            ptr += 40;

                            thinkers.Add(door);
                            break;

                        case SpecialClass.Floor:
                            PadPointer();
                            var floor = new FloorMove(world);
                            floor.ThinkerState = ReadThinkerState(data, ptr + 8);
                            floor.Type = (FloorMoveType)BitConverter.ToInt32(data, ptr + 12);
                            floor.Crush = BitConverter.ToInt32(data, ptr + 16) != 0;
                            floor.Sector = world.Map.Sectors[BitConverter.ToInt32(data, ptr + 20)];
                            floor.Sector.SpecialData = floor;
                            floor.Direction = BitConverter.ToInt32(data, ptr + 24);
                            floor.NewSpecial = (SectorSpecial)BitConverter.ToInt32(data, ptr + 28);
                            floor.Texture = BitConverter.ToInt32(data, ptr + 32);
                            floor.FloorDestHeight = new Fixed(BitConverter.ToInt32(data, ptr + 36));
                            floor.Speed = new Fixed(BitConverter.ToInt32(data, ptr + 40));
                            ptr += 44;

                            thinkers.Add(floor);
                            break;

                        case SpecialClass.Plat:
                            PadPointer();
                            var plat = new Platform(world);
                            plat.ThinkerState = ReadThinkerState(data, ptr + 8);
                            plat.Sector = world.Map.Sectors[BitConverter.ToInt32(data, ptr + 12)];
                            plat.Sector.SpecialData = plat;
                            plat.Speed = new Fixed(BitConverter.ToInt32(data, ptr + 16));
                            plat.Low = new Fixed(BitConverter.ToInt32(data, ptr + 20));
                            plat.High = new Fixed(BitConverter.ToInt32(data, ptr + 24));
                            plat.Wait = BitConverter.ToInt32(data, ptr + 28);
                            plat.Count = BitConverter.ToInt32(data, ptr + 32);
                            plat.Status = (PlatformState)BitConverter.ToInt32(data, ptr + 36);
                            plat.OldStatus = (PlatformState)BitConverter.ToInt32(data, ptr + 40);
                            plat.Crush = BitConverter.ToInt32(data, ptr + 44) != 0;
                            plat.Tag = BitConverter.ToInt32(data, ptr + 48);
                            plat.Type = (PlatformType)BitConverter.ToInt32(data, ptr + 52);
                            ptr += 56;

                            thinkers.Add(plat);
                            sa.AddActivePlatform(plat);
                            break;

                        case SpecialClass.Flash:
                            PadPointer();
                            var flash = new LightFlash(world);
                            flash.ThinkerState = ReadThinkerState(data, ptr + 8);
                            flash.Sector = world.Map.Sectors[BitConverter.ToInt32(data, ptr + 12)];
                            flash.Count = BitConverter.ToInt32(data, ptr + 16);
                            flash.MaxLight = BitConverter.ToInt32(data, ptr + 20);
                            flash.MinLight = BitConverter.ToInt32(data, ptr + 24);
                            flash.MaxTime = BitConverter.ToInt32(data, ptr + 28);
                            flash.MinTime = BitConverter.ToInt32(data, ptr + 32);
                            ptr += 36;

                            thinkers.Add(flash);
                            break;

                        case SpecialClass.Strobe:
                            PadPointer();
                            var strobe = new StrobeFlash(world);
                            strobe.ThinkerState = ReadThinkerState(data, ptr + 8);
                            strobe.Sector = world.Map.Sectors[BitConverter.ToInt32(data, ptr + 12)];
                            strobe.Count = BitConverter.ToInt32(data, ptr + 16);
                            strobe.MinLight = BitConverter.ToInt32(data, ptr + 20);
                            strobe.MaxLight = BitConverter.ToInt32(data, ptr + 24);
                            strobe.DarkTime = BitConverter.ToInt32(data, ptr + 28);
                            strobe.BrightTime = BitConverter.ToInt32(data, ptr + 32);
                            ptr += 36;

                            thinkers.Add(strobe);
                            break;

                        case SpecialClass.Glow:
                            PadPointer();
                            var glow = new GlowingLight(world);
                            glow.ThinkerState = ReadThinkerState(data, ptr + 8);
                            glow.Sector = world.Map.Sectors[BitConverter.ToInt32(data, ptr + 12)];
                            glow.MinLight = BitConverter.ToInt32(data, ptr + 16);
                            glow.MaxLight = BitConverter.ToInt32(data, ptr + 20);
                            glow.Direction = BitConverter.ToInt32(data, ptr + 24);
                            ptr += 28;

                            thinkers.Add(glow);
                            break;

                        default:
                            throw new Exception("Unknown special in savegame!");
                    }
                }
            }

            private static ThinkerState ReadThinkerState(byte[] data, int p)
            {
                switch (BitConverter.ToInt32(data, p))
                {
                    case 0:
                        return ThinkerState.InStasis;
                    default:
                        return ThinkerState.Active;
                }
            }

            private static int UnArchivePlayer(Player player, byte[] data, int p)
            {
                player.Clear();

                player.PlayerState = (PlayerState)BitConverter.ToInt32(data, p + 4);
                player.ViewZ = new Fixed(BitConverter.ToInt32(data, p + 16));
                player.ViewHeight = new Fixed(BitConverter.ToInt32(data, p + 20));
                player.DeltaViewHeight = new Fixed(BitConverter.ToInt32(data, p + 24));
                player.Bob = new Fixed(BitConverter.ToInt32(data, p + 28));
                player.Health = BitConverter.ToInt32(data, p + 32);
                player.ArmorPoints = BitConverter.ToInt32(data, p + 36);
                player.ArmorType = BitConverter.ToInt32(data, p + 40);
                for (var i = 0; i < (int)PowerType.Count; i++)
                {
                    player.Powers[i] = BitConverter.ToInt32(data, p + 44 + 4 * i);
                }
                for (var i = 0; i < (int)PowerType.Count; i++)
                {
                    player.Cards[i] = BitConverter.ToInt32(data, p + 68 + 4 * i) != 0;
                }
                player.Backpack = BitConverter.ToInt32(data, p + 92) != 0;
                for (var i = 0; i < Player.MaxPlayerCount; i++)
                {
                    player.Frags[i] = BitConverter.ToInt32(data, p + 96 + 4 * i);
                }
                player.ReadyWeapon = (WeaponType)BitConverter.ToInt32(data, p + 112);
                player.PendingWeapon = (WeaponType)BitConverter.ToInt32(data, p + 116);
                for (var i = 0; i < (int)WeaponType.Count; i++)
                {
                    player.WeaponOwned[i] = BitConverter.ToInt32(data, p + 120 + 4 * i) != 0;
                }
                for (var i = 0; i < (int)AmmoType.Count; i++)
                {
                    player.Ammo[i] = BitConverter.ToInt32(data, p + 156 + 4 * i);
                }
                for (var i = 0; i < (int)AmmoType.Count; i++)
                {
                    player.MaxAmmo[i] = BitConverter.ToInt32(data, p + 172 + 4 * i);
                }
                player.AttackDown = BitConverter.ToInt32(data, p + 188) != 0;
                player.UseDown = BitConverter.ToInt32(data, p + 192) != 0;
                player.Cheats = (CheatFlags)BitConverter.ToInt32(data, p + 196);
                player.Refire = BitConverter.ToInt32(data, p + 200);
                player.KillCount = BitConverter.ToInt32(data, p + 204);
                player.ItemCount = BitConverter.ToInt32(data, p + 208);
                player.SecretCount = BitConverter.ToInt32(data, p + 212);
                player.DamageCount = BitConverter.ToInt32(data, p + 220);
                player.BonusCount = BitConverter.ToInt32(data, p + 224);
                player.ExtraLight = BitConverter.ToInt32(data, p + 232);
                player.FixedColorMap = BitConverter.ToInt32(data, p + 236);
                player.ColorMap = BitConverter.ToInt32(data, p + 240);
                for (var i = 0; i < (int)PlayerSprite.Count; i++)
                {
                    player.PlayerSprites[i].State = DoomInfo.States[BitConverter.ToInt32(data, p + 244 + 16 * i)];
                    if (player.PlayerSprites[i].State.Number == (int)MobjState.Null)
                    {
                        player.PlayerSprites[i].State = null;
                    }
                    player.PlayerSprites[i].Tics = BitConverter.ToInt32(data, p + 244 + 16 * i + 4);
                    player.PlayerSprites[i].Sx = new Fixed(BitConverter.ToInt32(data, p + 244 + 16 * i + 8));
                    player.PlayerSprites[i].Sy = new Fixed(BitConverter.ToInt32(data, p + 244 + 16 * i + 12));
                }
                player.DidSecret = BitConverter.ToInt32(data, p + 276) != 0;

                return p + 280;
            }

            private static int UnArchiveSector(Sector sector, byte[] data, int p)
            {
                sector.FloorHeight = Fixed.FromInt(BitConverter.ToInt16(data, p));
                sector.CeilingHeight = Fixed.FromInt(BitConverter.ToInt16(data, p + 2));
                sector.FloorFlat = BitConverter.ToInt16(data, p + 4);
                sector.CeilingFlat = BitConverter.ToInt16(data, p + 6);
                sector.LightLevel = BitConverter.ToInt16(data, p + 8);
                sector.Special = (SectorSpecial)BitConverter.ToInt16(data, p + 10);
                sector.Tag = BitConverter.ToInt16(data, p + 12);
                sector.SpecialData = null;
                sector.SoundTarget = null;
                return p + 14;
            }

            private static int UnArchiveLine(LineDef line, byte[] data, int p)
            {
                line.Flags = (LineFlags)BitConverter.ToInt16(data, p);
                line.Special = (LineSpecial)BitConverter.ToInt16(data, p + 2);
                line.Tag = BitConverter.ToInt16(data, p + 4);
                p += 6;

                if (line.FrontSide != null)
                {
                    var side = line.FrontSide;
                    side.TextureOffset = Fixed.FromInt(BitConverter.ToInt16(data, p));
                    side.RowOffset = Fixed.FromInt(BitConverter.ToInt16(data, p + 2));
                    side.TopTexture = BitConverter.ToInt16(data, p + 4);
                    side.BottomTexture = BitConverter.ToInt16(data, p + 6);
                    side.MiddleTexture = BitConverter.ToInt16(data, p + 8);
                    p += 10;
                }

                if (line.BackSide != null)
                {
                    var side = line.BackSide;
                    side.TextureOffset = Fixed.FromInt(BitConverter.ToInt16(data, p));
                    side.RowOffset = Fixed.FromInt(BitConverter.ToInt16(data, p + 2));
                    side.TopTexture = BitConverter.ToInt16(data, p + 4);
                    side.BottomTexture = BitConverter.ToInt16(data, p + 6);
                    side.MiddleTexture = BitConverter.ToInt16(data, p + 8);
                    p += 10;
                }

                return p;
            }
        }
    }
}
