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
    public sealed class ThingAllocation
    {
        private World world;

        public ThingAllocation(World world)
        {
            this.world = world;

            InitSpawnMapThing();
            InitMultiPlayerRespawn();
            InitRespawnSpecials();
        }



        ////////////////////////////////////////////////////////////
        // Spawn functions for level start
        ////////////////////////////////////////////////////////////

        private MapThing[] playerStarts;
        private List<MapThing> deathmatchStarts;

        private void InitSpawnMapThing()
        {
            playerStarts = new MapThing[Player.MaxPlayerCount];
            deathmatchStarts = new List<MapThing>();
        }

        /// <summary>
        /// Spawn a mobj at the mapthing.
        /// </summary>
        public void SpawnMapThing(MapThing mt)
        {
            // Count deathmatch start positions.
            if (mt.Type == 11)
            {
                if (deathmatchStarts.Count < 10)
                {
                    deathmatchStarts.Add(mt);
                }

                return;
            }

            // Check for players specially.
            if (mt.Type <= 4)
            {
                var playerNumber = mt.Type - 1;

                // This check is neccesary in Plutonia MAP12,
                // which contains an unknown thing with type 0.
                if (playerNumber < 0)
                {
                    return;
                }

                // Save spots for respawning in network games.
                playerStarts[playerNumber] = mt;

                if (world.Options.Deathmatch == 0)
                {
                    SpawnPlayer(mt);
                }

                return;
            }

            if (mt.Type == 11 || mt.Type <= 4)
            {
                return;
            }

            // Check for apropriate skill level.
            if (!world.Options.NetGame && ((int)mt.Flags & 16) != 0)
            {
                return;
            }

            int bit;
            if (world.Options.Skill == GameSkill.Baby)
            {
                bit = 1;
            }
            else if (world.Options.Skill == GameSkill.Nightmare)
            {
                bit = 4;
            }
            else
            {
                bit = 1 << ((int)world.Options.Skill - 1);
            }

            if (((int)mt.Flags & bit) == 0)
            {
                return;
            }

            // Find which type to spawn.
            int i;
            for (i = 0; i < DoomInfo.MobjInfos.Length; i++)
            {
                if (mt.Type == DoomInfo.MobjInfos[i].DoomEdNum)
                {
                    break;
                }
            }

            if (i == DoomInfo.MobjInfos.Length)
            {
                throw new Exception("Unknown type!");
            }

            // Don't spawn keycards and players in deathmatch.
            if (world.Options.Deathmatch != 0 &&
                (DoomInfo.MobjInfos[i].Flags & MobjFlags.NotDeathmatch) != 0)
            {
                return;
            }

            // Don't spawn any monsters if -nomonsters.
            if (world.Options.NoMonsters &&
                    (i == (int)MobjType.Skull ||
                    (DoomInfo.MobjInfos[i].Flags & MobjFlags.CountKill) != 0))
            {
                return;
            }

            // Spawn it.
            Fixed x = mt.X;
            Fixed y = mt.Y;
            Fixed z;
            if ((DoomInfo.MobjInfos[i].Flags & MobjFlags.SpawnCeiling) != 0)
            {
                z = Mobj.OnCeilingZ;
            }
            else
            {
                z = Mobj.OnFloorZ;
            }

            var mobj = SpawnMobj(x, y, z, (MobjType)i);

            mobj.SpawnPoint = mt;

            if (mobj.Tics > 0)
            {
                mobj.Tics = 1 + (world.Random.Next() % mobj.Tics);
            }

            if ((mobj.Flags & MobjFlags.CountKill) != 0)
            {
                world.TotalKills++;
            }

            if ((mobj.Flags & MobjFlags.CountItem) != 0)
            {
                world.TotalItems++;
            }

            mobj.Angle = mt.Angle;

            if ((mt.Flags & ThingFlags.Ambush) != 0)
            {
                mobj.Flags |= MobjFlags.Ambush;
            }
        }

        /// <summary>
        /// Called when a player is spawned on the level.
        /// Most of the player structure stays unchanged between levels.
        /// </summary>
        public void SpawnPlayer(MapThing mt)
        {
            var players = world.Options.Players;
            var playerNumber = mt.Type - 1;

            // Not playing?
            if (!players[playerNumber].InGame)
            {
                return;
            }

            var player = players[playerNumber];

            if (player.PlayerState == PlayerState.Reborn)
            {
                players[playerNumber].Reborn();
            }

            var x = mt.X;
            var y = mt.Y;
            var z = Mobj.OnFloorZ;
            var mobj = SpawnMobj(x, y, z, MobjType.Player);

            if (mt.Type - 1 == world.Options.ConsolePlayer)
            {
                world.StatusBar.Reset();
                world.Options.Sound.SetListener(mobj);
            }

            // Set color translations for player sprites.
            if (playerNumber >= 1)
            {
                mobj.Flags |= (MobjFlags)((mt.Type - 1) << (int)MobjFlags.TransShift);
            }

            mobj.Angle = mt.Angle;
            mobj.Player = player;
            mobj.Health = player.Health;

            player.Mobj = mobj;
            player.PlayerState = PlayerState.Live;
            player.Refire = 0;
            player.Message = null;
            player.MessageTime = 0;
            player.DamageCount = 0;
            player.BonusCount = 0;
            player.ExtraLight = 0;
            player.FixedColorMap = 0;
            player.ViewHeight = Player.NormalViewHeight;

            // Setup gun psprite.
            world.PlayerBehavior.SetupPlayerSprites(player);

            // Give all cards in death match mode.
            if (world.Options.Deathmatch != 0)
            {
                for (var i = 0; i < (int)CardType.Count; i++)
                {
                    player.Cards[i] = true;
                }
            }
        }

        public IReadOnlyList<MapThing> PlayerStarts => playerStarts;
        public IReadOnlyList<MapThing> DeathmatchStarts => deathmatchStarts;



        ////////////////////////////////////////////////////////////
        // Thing spawn functions for the middle of a game
        ////////////////////////////////////////////////////////////

        /// <summary>
        /// Spawn a mobj at the given position as the given type.
        /// </summary>
        public Mobj SpawnMobj(Fixed x, Fixed y, Fixed z, MobjType type)
        {
            var mobj = new Mobj(world);

            var info = DoomInfo.MobjInfos[(int)type];

            mobj.Type = type;
            mobj.Info = info;
            mobj.X = x;
            mobj.Y = y;
            mobj.Radius = info.Radius;
            mobj.Height = info.Height;
            mobj.Flags = info.Flags;
            mobj.Health = info.SpawnHealth;

            if (world.Options.Skill != GameSkill.Nightmare)
            {
                mobj.ReactionTime = info.ReactionTime;
            }

            mobj.LastLook = world.Random.Next() % Player.MaxPlayerCount;

            // Do not set the state with P_SetMobjState,
            // because action routines can not be called yet.
            var st = DoomInfo.States[(int)info.SpawnState];

            mobj.State = st;
            mobj.Tics = st.Tics;
            mobj.Sprite = st.Sprite;
            mobj.Frame = st.Frame;

            // Set subsector and/or block links.
            world.ThingMovement.SetThingPosition(mobj);

            mobj.FloorZ = mobj.Subsector.Sector.FloorHeight;
            mobj.CeilingZ = mobj.Subsector.Sector.CeilingHeight;

            if (z == Mobj.OnFloorZ)
            {
                mobj.Z = mobj.FloorZ;
            }
            else if (z == Mobj.OnCeilingZ)
            {
                mobj.Z = mobj.CeilingZ - mobj.Info.Height;
            }
            else
            {
                mobj.Z = z;
            }

            world.Thinkers.Add(mobj);

            return mobj;
        }

        /// <summary>
        /// Remove the mobj from the level.
        /// </summary>
        public void RemoveMobj(Mobj mobj)
        {
            var tm = world.ThingMovement;

            if ((mobj.Flags & MobjFlags.Special) != 0 &&
                (mobj.Flags & MobjFlags.Dropped) == 0 &&
                (mobj.Type != MobjType.Inv) &&
                (mobj.Type != MobjType.Ins))
            {
                itemRespawnQue[itemQueHead] = mobj.SpawnPoint;
                itemRespawnTime[itemQueHead] = world.LevelTime;
                itemQueHead = (itemQueHead + 1) & (itemQueSize - 1);

                // Lose one off the end?
                if (itemQueHead == itemQueTail)
                {
                    itemQueTail = (itemQueTail + 1) & (itemQueSize - 1);
                }
            }

            // Unlink from sector and block lists.
            tm.UnsetThingPosition(mobj);

            // Stop any playing sound.
            world.StopSound(mobj);

            // Free block.
            world.Thinkers.Remove(mobj);
        }

        /// <summary>
        /// Get the speed of the given missile type.
        /// Some missiles have different speeds according to the game setting.
        /// </summary>
        private int GetMissileSpeed(MobjType type)
        {
            if (world.Options.FastMonsters || world.Options.Skill == GameSkill.Nightmare)
            {
                switch (type)
                {
                    case MobjType.Bruisershot:
                    case MobjType.Headshot:
                    case MobjType.Troopshot:
                        return 20 * Fixed.FracUnit;
                    default:
                        return DoomInfo.MobjInfos[(int)type].Speed;
                }
            }
            else
            {
                return DoomInfo.MobjInfos[(int)type].Speed;
            }
        }

        /// <summary>
        /// Moves the missile forward a bit and possibly explodes it right there.
        /// </summary>
        private void CheckMissileSpawn(Mobj missile)
        {
            missile.Tics -= world.Random.Next() & 3;
            if (missile.Tics < 1)
            {
                missile.Tics = 1;
            }

            // Move a little forward so an angle can be computed if it immediately explodes.
            missile.X += (missile.MomX >> 1);
            missile.Y += (missile.MomY >> 1);
            missile.Z += (missile.MomZ >> 1);

            if (!world.ThingMovement.TryMove(missile, missile.X, missile.Y))
            {
                world.ThingInteraction.ExplodeMissile(missile);
            }
        }

        /// <summary>
        /// Shoot a missile from the source to the destination.
        /// For monsters.
        /// </summary>
        public Mobj SpawnMissile(Mobj source, Mobj dest, MobjType type)
        {
            var missile = SpawnMobj(
                source.X,
                source.Y,
                source.Z + Fixed.FromInt(32), type);

            if (missile.Info.SeeSound != 0)
            {
                world.StartSound(missile, missile.Info.SeeSound, SfxType.Misc);
            }

            // Where it came from?
            missile.Target = source;

            var angle = Geometry.PointToAngle(
                source.X, source.Y,
                dest.X, dest.Y);

            // Fuzzy player.
            if ((dest.Flags & MobjFlags.Shadow) != 0)
            {
                var random = world.Random;
                angle += new Angle((random.Next() - random.Next()) << 20);
            }

            var speed = GetMissileSpeed(missile.Type);

            missile.Angle = angle;
            missile.MomX = new Fixed(speed) * Trig.Cos(angle);
            missile.MomY = new Fixed(speed) * Trig.Sin(angle);

            var dist = Geometry.AproxDistance(
                dest.X - source.X,
                dest.Y - source.Y);

            var num = (dest.Z - source.Z).Data;
            var den = (dist / speed).Data;
            if (den < 1)
            {
                den = 1;
            }

            missile.MomZ = new Fixed(num / den);

            CheckMissileSpawn(missile);

            return missile;
        }

        /// <summary>
        /// Shoot a missile from the source.
        /// For players.
        /// </summary>
        public void SpawnPlayerMissile(Mobj source, MobjType type)
        {
            var hs = world.Hitscan;

            // See which target is to be aimed at.
            var angle = source.Angle;
            var slope = hs.AimLineAttack(source, angle, Fixed.FromInt(16 * 64));

            if (hs.LineTarget == null)
            {
                angle += new Angle(1 << 26);
                slope = hs.AimLineAttack(source, angle, Fixed.FromInt(16 * 64));

                if (hs.LineTarget == null)
                {
                    angle -= new Angle(2 << 26);
                    slope = hs.AimLineAttack(source, angle, Fixed.FromInt(16 * 64));
                }

                if (hs.LineTarget == null)
                {
                    angle = source.Angle;
                    slope = Fixed.Zero;
                }
            }

            var x = source.X;
            var y = source.Y;
            var z = source.Z + Fixed.FromInt(32);

            var missile = SpawnMobj(x, y, z, type);

            if (missile.Info.SeeSound != 0)
            {
                world.StartSound(missile, missile.Info.SeeSound, SfxType.Misc);
            }

            missile.Target = source;
            missile.Angle = angle;
            missile.MomX = new Fixed(missile.Info.Speed) * Trig.Cos(angle);
            missile.MomY = new Fixed(missile.Info.Speed) * Trig.Sin(angle);
            missile.MomZ = new Fixed(missile.Info.Speed) * slope;

            CheckMissileSpawn(missile);
        }



        ////////////////////////////////////////////////////////////
        // Multi-player related functions
        ////////////////////////////////////////////////////////////

        private static readonly int bodyQueSize = 32;
        private int bodyQueSlot;
        private Mobj[] bodyQue;

        private void InitMultiPlayerRespawn()
        {
            bodyQueSlot = 0;
            bodyQue = new Mobj[bodyQueSize];
        }

        /// <summary>
        /// Returns false if the player cannot be respawned at the given
        /// mapthing spot because something is occupying it.
        /// </summary>
        public bool CheckSpot(int playernum, MapThing mthing)
        {
            var players = world.Options.Players;

            if (players[playernum].Mobj == null)
            {
                // First spawn of level, before corpses.
                for (var i = 0; i < playernum; i++)
                {
                    if (players[i].Mobj.X == mthing.X && players[i].Mobj.Y == mthing.Y)
                    {
                        return false;
                    }
                }
                return true;
            }

            var x = mthing.X;
            var y = mthing.Y;

            if (!world.ThingMovement.CheckPosition(players[playernum].Mobj, x, y))
            {
                return false;
            }

            // Flush an old corpse if needed.
            if (bodyQueSlot >= bodyQueSize)
            {
                RemoveMobj(bodyQue[bodyQueSlot % bodyQueSize]);
            }
            bodyQue[bodyQueSlot % bodyQueSize] = players[playernum].Mobj;
            bodyQueSlot++;

            // Spawn a teleport fog.
            var subsector = Geometry.PointInSubsector(x, y, world.Map);

            var angle = (Angle.Ang45.Data >> Trig.AngleToFineShift) *
                ((int)Math.Round(mthing.Angle.ToDegree()) / 45);

            //
            // The code below to reproduce respawn fog bug in deathmath
            // is based on Chocolate Doom's implementation.
            //

            Fixed xa;
            Fixed ya;
            switch (angle)
            {
                case 4096: // -4096:
                    xa = Trig.Tan(2048); // finecosine[-4096]
                    ya = Trig.Tan(0);    // finesine[-4096]
                    break;
                case 5120: // -3072:
                    xa = Trig.Tan(3072); // finecosine[-3072]
                    ya = Trig.Tan(1024); // finesine[-3072]
                    break;
                case 6144: // -2048:
                    xa = Trig.Sin(0);    // finecosine[-2048]
                    ya = Trig.Tan(2048); // finesine[-2048]
                    break;
                case 7168: // -1024:
                    xa = Trig.Sin(1024); // finecosine[-1024]
                    ya = Trig.Tan(3072); // finesine[-1024]
                    break;
                case 0:
                case 1024:
                case 2048:
                case 3072:
                    xa = Trig.Cos((int)angle);
                    ya = Trig.Sin((int)angle);
                    break;
                default:
                    throw new Exception("Unexpected angle: " + angle);
            }

            var mo = SpawnMobj(
                x + 20 * xa, y + 20 * ya,
                subsector.Sector.FloorHeight,
                MobjType.Tfog);

            if (!world.FirstTicIsNotYetDone)
            {
                // Don't start sound on first frame.
                world.StartSound(mo, Sfx.TELEPT, SfxType.Misc);
            }

            return true;
        }

        /// <summary>
        /// Spawns a player at one of the random death match spots.
        /// Called at level load and each death.
        /// </summary>
        public void DeathMatchSpawnPlayer(int playerNumber)
        {
            var selections = deathmatchStarts.Count;
            if (selections < 4)
            {
                throw new Exception("Only " + selections + " deathmatch spots, 4 required");
            }

            var random = world.Random;
            for (var j = 0; j < 20; j++)
            {
                var i = random.Next() % selections;
                if (CheckSpot(playerNumber, deathmatchStarts[i]))
                {
                    deathmatchStarts[i].Type = playerNumber + 1;
                    SpawnPlayer(deathmatchStarts[i]);
                    return;
                }
            }

            // No good spot, so the player will probably get stuck .
            SpawnPlayer(playerStarts[playerNumber]);
        }



        ////////////////////////////////////////////////////////////
        // Item respawn
        ////////////////////////////////////////////////////////////

        private static readonly int itemQueSize = 128;
        private MapThing[] itemRespawnQue;
        private int[] itemRespawnTime;
        private int itemQueHead;
        private int itemQueTail;

        private void InitRespawnSpecials()
        {
            itemRespawnQue = new MapThing[itemQueSize];
            itemRespawnTime = new int[itemQueSize];
            itemQueHead = 0;
            itemQueTail = 0;
        }

        /// <summary>
        /// Respawn items if the game mode is altdeath.
        /// </summary>
        public void RespawnSpecials()
        {
            // Only respawn items in deathmatch.
            if (world.Options.Deathmatch != 2)
            {
                return;
            }

            // Nothing left to respawn?
            if (itemQueHead == itemQueTail)
            {
                return;
            }

            // Wait at least 30 seconds.
            if (world.LevelTime - itemRespawnTime[itemQueTail] < 30 * 35)
            {
                return;
            }

            var mthing = itemRespawnQue[itemQueTail];

            var x = mthing.X;
            var y = mthing.Y;

            // Spawn a teleport fog at the new spot.
            var ss = Geometry.PointInSubsector(x, y, world.Map);
            var mo = SpawnMobj(x, y, ss.Sector.FloorHeight, MobjType.Ifog);
            world.StartSound(mo, Sfx.ITMBK, SfxType.Misc);

            int i;
            // Find which type to spawn.
            for (i = 0; i < DoomInfo.MobjInfos.Length; i++)
            {
                if (mthing.Type == DoomInfo.MobjInfos[i].DoomEdNum)
                {
                    break;
                }
            }

            // Spawn it.
            Fixed z;
            if ((DoomInfo.MobjInfos[i].Flags & MobjFlags.SpawnCeiling) != 0)
            {
                z = Mobj.OnCeilingZ;
            }
            else
            {
                z = Mobj.OnFloorZ;
            }

            mo = SpawnMobj(x, y, z, (MobjType)i);
            mo.SpawnPoint = mthing;
            mo.Angle = mthing.Angle;

            // Pull it from the que.
            itemQueTail = (itemQueTail + 1) & (itemQueSize - 1);
        }
    }
}
