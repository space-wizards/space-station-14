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
    public class Mobj : Thinker
    {
        //
        // NOTES: mobj_t
        //
        // mobj_ts are used to tell the refresh where to draw an image,
        // tell the world simulation when objects are contacted,
        // and tell the sound driver how to position a sound.
        //
        // The refresh uses the next and prev links to follow
        // lists of things in sectors as they are being drawn.
        // The sprite, frame, and angle elements determine which patch_t
        // is used to draw the sprite if it is visible.
        // The sprite and frame values are allmost allways set
        // from state_t structures.
        // The statescr.exe utility generates the states.h and states.c
        // files that contain the sprite/frame numbers from the
        // statescr.txt source file.
        // The xyz origin point represents a point at the bottom middle
        // of the sprite (between the feet of a biped).
        // This is the default origin position for patch_ts grabbed
        // with lumpy.exe.
        // A walking creature will have its z equal to the floor
        // it is standing on.
        //
        // The sound code uses the x,y, and subsector fields
        // to do stereo positioning of any sound effited by the mobj_t.
        //
        // The play simulation uses the blocklinks, x,y,z, radius, height
        // to determine when mobj_ts are touching each other,
        // touching lines in the map, or hit by trace lines (gunshots,
        // lines of sight, etc).
        // The mobj_t->flags element has various bit flags
        // used by the simulation.
        //
        // Every mobj_t is linked into a single sector
        // based on its origin coordinates.
        // The subsector_t is found with R_PointInSubsector(x,y),
        // and the sector_t can be found with subsector->sector.
        // The sector links are only used by the rendering code,
        // the play simulation does not care about them at all.
        //
        // Any mobj_t that needs to be acted upon by something else
        // in the play world (block movement, be shot, etc) will also
        // need to be linked into the blockmap.
        // If the thing has the MF_NOBLOCK flag set, it will not use
        // the block links. It can still interact with other things,
        // but only as the instigator (missiles will run into other
        // things, but nothing can run into a missile).
        // Each block in the grid is 128*128 units, and knows about
        // every line_t that it contains a piece of, and every
        // interactable mobj_t that has its origin contained.  
        //
        // A valid mobj_t is a mobj_t that has the proper subsector_t
        // filled in for its xy coordinates and is linked into the
        // sector from which the subsector was made, or has the
        // MF_NOSECTOR flag set (the subsector_t needs to be valid
        // even if MF_NOSECTOR is set), and is linked into a blockmap
        // block or has the MF_NOBLOCKMAP flag set.
        // Links should only be modified by the P_[Un]SetThingPosition()
        // functions.
        // Do not change the MF_NO? flags while a thing is valid.
        //
        // Any questions?
        //

        public static readonly Fixed OnFloorZ = Fixed.MinValue;
        public static readonly Fixed OnCeilingZ = Fixed.MaxValue;

        private World world;

        // Info for drawing: position.
        private Fixed x;
        private Fixed y;
        private Fixed z;

        // More list: links in sector (if needed).
        private Mobj sectorNext;
        private Mobj sectorPrev;

        // More drawing info: to determine current sprite.
        private Angle angle; // Orientation.
        private Sprite sprite; // Used to find patch_t and flip value.
        private int frame; // Might be ORed with FF_FULLBRIGHT.

        // Interaction info, by BLOCKMAP.
        // Links in blocks (if needed).
        private Mobj blockNext;
        private Mobj blockPrev;

        private Subsector subsector;

        // The closest interval over all contacted Sectors.
        private Fixed floorZ;
        private Fixed ceilingZ;

        // For movement checking.
        private Fixed radius;
        private Fixed height;

        // Momentums, used to update position.
        private Fixed momX;
        private Fixed momY;
        private Fixed momZ;

        // If == validCount, already checked.
        private int validCount;

        private MobjType type;
        private MobjInfo info;

        private int tics; // State tic counter.
        private MobjStateDef state;
        private MobjFlags flags;
        private int health;

        // Movement direction, movement generation (zig-zagging).
        private Direction moveDir;
        private int moveCount; // When 0, select a new dir.

        // Thing being chased / attacked (or null),
        // also the originator for missiles.
        private Mobj target;

        // Reaction time: if non 0, don't attack yet.
        // Used by player to freeze a bit after teleporting.
        private int reactionTime;

        // If >0, the target will be chased
        // no matter what (even if shot).
        private int threshold;

        // Additional info record for player avatars only.
        // Only valid if type == MT_PLAYER
        private Player player;

        // Player number last looked for.
        private int lastLook;

        // For nightmare respawn.
        private MapThing spawnPoint;

        // Thing being chased/attacked for tracers.
        private Mobj tracer;

        public Mobj(World world)
        {
            this.world = world;
        }

        public override void Run()
        {
            // Momentum movement.
            if (momX != Fixed.Zero || momY != Fixed.Zero ||
                (flags & MobjFlags.SkullFly) != 0)
            {
                world.ThingMovement.XYMovement(this);

                if (ThinkerState == ThinkerState.Removed)
                {
                    // Mobj was removed.
                    return;
                }
            }

            if ((z != floorZ) || momZ != Fixed.Zero)
            {
                world.ThingMovement.ZMovement(this);

                if (ThinkerState == ThinkerState.Removed)
                {
                    // Mobj was removed.
                    return;
                }
            }

            // Cycle through states,
            // calling action functions at transitions.
            if (tics != -1)
            {
                tics--;

                // You can cycle through multiple states in a tic.
                if (tics == 0)
                {
                    if (!SetState(state.Next))
                    {
                        // Freed itself.
                        return;
                    }
                }
            }
            else
            {
                // Check for nightmare respawn.
                if ((flags & MobjFlags.CountKill) == 0)
                {
                    return;
                }

                var options = world.Options;
                if (!(options.Skill == GameSkill.Nightmare || options.RespawnMonsters))
                {
                    return;
                }

                moveCount++;

                if (moveCount < 12 * 35)
                {
                    return;
                }

                if ((world.LevelTime & 31) != 0)
                {
                    return;
                }

                if (world.Random.Next() > 4)
                {
                    return;
                }

                NightmareRespawn();
            }
        }

        public bool SetState(MobjState state)
        {
            do
            {
                if (state == MobjState.Null)
                {
                    this.state = DoomInfo.States[(int)MobjState.Null];
                    world.ThingAllocation.RemoveMobj(this);
                    return false;
                }

                var st = DoomInfo.States[(int)state];
                this.state = st;
                tics = GetTics(st);
                sprite = st.Sprite;
                frame = st.Frame;

                // Modified handling.
                // Call action functions when the state is set.
                if (st.MobjAction != null)
                {
                    st.MobjAction(world, this);
                }

                state = st.Next;
            }
            while (tics == 0);

            return true;
        }

        private int GetTics(MobjStateDef state)
        {
            var options = world.Options;
            if (options.FastMonsters || options.Skill == GameSkill.Nightmare)
            {
                if ((int)MobjState.SargRun1 <= state.Number &&
                    state.Number <= (int)MobjState.SargPain2)
                {
                    return state.Tics >> 1;
                }
                else
                {
                    return state.Tics;
                }
            }
            else
            {
                return state.Tics;
            }
        }

        private void NightmareRespawn()
        {
            MapThing sp;
            if (spawnPoint != null)
            {
                sp = spawnPoint;
            }
            else
            {
                sp = MapThing.Empty;
            }

            // Somthing is occupying it's position?
            if (!world.ThingMovement.CheckPosition(this, sp.X, sp.Y))
            {
                // No respwan.
                return;
            }

            var ta = world.ThingAllocation;

            // Spawn a teleport fog at old spot.
            var fog1 = ta.SpawnMobj(
                x, y,
                subsector.Sector.FloorHeight,
                MobjType.Tfog);

            // Initiate teleport sound.
            world.StartSound(fog1, Sfx.TELEPT, SfxType.Misc);

            // Spawn a teleport fog at the new spot.
            var ss = Geometry.PointInSubsector(sp.X, sp.Y, world.Map);

            var fog2 = ta.SpawnMobj(
                sp.X, sp.Y,
                ss.Sector.FloorHeight, MobjType.Tfog);

            world.StartSound(fog2, Sfx.TELEPT, SfxType.Misc);

            // Spawn the new monster.
            Fixed z;
            if ((info.Flags & MobjFlags.SpawnCeiling) != 0)
            {
                z = OnCeilingZ;
            }
            else
            {
                z = OnFloorZ;
            }

            // Inherit attributes from deceased one.
            var mobj = ta.SpawnMobj(sp.X, sp.Y, z, type);
            mobj.SpawnPoint = spawnPoint;
            mobj.Angle = sp.Angle;

            if ((sp.Flags & ThingFlags.Ambush) != 0)
            {
                mobj.Flags |= MobjFlags.Ambush;
            }

            mobj.ReactionTime = 18;

            // Remove the old monster.
            world.ThingAllocation.RemoveMobj(this);
        }

        public World World => world;

        public Fixed X
        {
            get => x;
            set => x = value;
        }

        public Fixed Y
        {
            get => y;
            set => y = value;
        }

        public Fixed Z
        {
            get => z;
            set => z = value;
        }

        public Mobj SectorNext
        {
            get => sectorNext;
            set => sectorNext = value;
        }

        public Mobj SectorPrev
        {
            get => sectorPrev;
            set => sectorPrev = value;
        }

        public Angle Angle
        {
            get => angle;
            set => angle = value;
        }

        public Sprite Sprite
        {
            get => sprite;
            set => sprite = value;
        }

        public int Frame
        {
            get => frame;
            set => frame = value;
        }

        public Mobj BlockNext
        {
            get => blockNext;
            set => blockNext = value;
        }

        public Mobj BlockPrev
        {
            get => blockPrev;
            set => blockPrev = value;
        }

        public Subsector Subsector
        {
            get => subsector;
            set => subsector = value;
        }

        public Fixed FloorZ
        {
            get => floorZ;
            set => floorZ = value;
        }

        public Fixed CeilingZ
        {
            get => ceilingZ;
            set => ceilingZ = value;
        }

        public Fixed Radius
        {
            get => radius;
            set => radius = value;
        }

        public Fixed Height
        {
            get => height;
            set => height = value;
        }

        public Fixed MomX
        {
            get => momX;
            set => momX = value;
        }

        public Fixed MomY
        {
            get => momY;
            set => momY = value;
        }

        public Fixed MomZ
        {
            get => momZ;
            set => momZ = value;
        }

        public int ValidCount
        {
            get => validCount;
            set => validCount = value;
        }

        public MobjType Type
        {
            get => type;
            set => type = value;
        }

        public MobjInfo Info
        {
            get => info;
            set => info = value;
        }

        public int Tics
        {
            get => tics;
            set => tics = value;
        }

        public MobjStateDef State
        {
            get => state;
            set => state = value;
        }

        public MobjFlags Flags
        {
            get => flags;
            set => flags = value;
        }

        public int Health
        {
            get => health;
            set => health = value;
        }

        public Direction MoveDir
        {
            get => moveDir;
            set => moveDir = value;
        }

        public int MoveCount
        {
            get => moveCount;
            set => moveCount = value;
        }

        public Mobj Target
        {
            get => target;
            set => target = value;
        }

        public int ReactionTime
        {
            get => reactionTime;
            set => reactionTime = value;
        }

        public int Threshold
        {
            get => threshold;
            set => threshold = value;
        }

        public Player Player
        {
            get => player;
            set => player = value;
        }

        public int LastLook
        {
            get => lastLook;
            set => lastLook = value;
        }

        public MapThing SpawnPoint
        {
            get => spawnPoint;
            set => spawnPoint = value;
        }

        public Mobj Tracer
        {
            get => tracer;
            set => tracer = value;
        }
    }
}
