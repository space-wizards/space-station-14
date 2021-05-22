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
    public sealed class PlayerBehavior
    {
        public static readonly int[] ForwardMove =
        {
            0x19,
            0x32
        };

        public static readonly int[] SideMove =
        {
            0x18,
            0x28
        };

        public static readonly int[] AngleTurn =
        {
            640,
            1280,
            320 // For slow turn.
        };

        public static readonly int MaxMove = ForwardMove[1];
        public static readonly int SlowTurnTics = 6;



        private World world;

        public PlayerBehavior(World world)
        {
            this.world = world;
        }



        ////////////////////////////////////////////////////////////
        // Player movement
        ////////////////////////////////////////////////////////////

        /// <summary>
        /// Called every frame to update player state.
        /// </summary>
        public void PlayerThink(Player player)
        {
            if (player.MessageTime > 0)
            {
                player.MessageTime--;
            }

            if ((player.Cheats & CheatFlags.NoClip) != 0)
            {
                player.Mobj.Flags |= MobjFlags.NoClip;
            }
            else
            {
                player.Mobj.Flags &= ~MobjFlags.NoClip;
            }

            // Chain saw run forward.
            var cmd = player.Cmd;
            if ((player.Mobj.Flags & MobjFlags.JustAttacked) != 0)
            {
                cmd.AngleTurn = 0;
                cmd.ForwardMove = 0xC800 / 512;
                cmd.SideMove = 0;
                player.Mobj.Flags &= ~MobjFlags.JustAttacked;
            }

            if (player.PlayerState == PlayerState.Dead)
            {
                DeathThink(player);
                return;
            }

            // Move around.
            // Reactiontime is used to prevent movement for a bit after a teleport.
            if (player.Mobj.ReactionTime > 0)
            {
                player.Mobj.ReactionTime--;
            }
            else
            {
                MovePlayer(player);
            }

            CalcHeight(player);

            if (player.Mobj.Subsector.Sector.Special != 0)
            {
                PlayerInSpecialSector(player);
            }

            // Check for weapon change.

            // A special event has no other buttons.
            if ((cmd.Buttons & TicCmdButtons.Special) != 0)
            {
                cmd.Buttons = 0;
            }

            if ((cmd.Buttons & TicCmdButtons.Change) != 0)
            {
                // The actual changing of the weapon is done when the weapon psprite can do it.
                // Not in the middle of an attack.
                var newWeapon = (cmd.Buttons & TicCmdButtons.WeaponMask) >> TicCmdButtons.WeaponShift;

                if (newWeapon == (int)WeaponType.Fist &&
                    player.WeaponOwned[(int)WeaponType.Chainsaw] &&
                    !(player.ReadyWeapon == WeaponType.Chainsaw && player.Powers[(int)PowerType.Strength] != 0))
                {
                    newWeapon = (int)WeaponType.Chainsaw;
                }

                if ((world.Options.GameMode == GameMode.Commercial) &&
                    newWeapon == (int)WeaponType.Shotgun &&
                    player.WeaponOwned[(int)WeaponType.SuperShotgun] &&
                    player.ReadyWeapon != WeaponType.SuperShotgun)
                {
                    newWeapon = (int)WeaponType.SuperShotgun;
                }

                if (player.WeaponOwned[newWeapon] &&
                    newWeapon != (int)player.ReadyWeapon)
                {
                    // Do not go to plasma or BFG in shareware, even if cheated.
                    if ((newWeapon != (int)WeaponType.Plasma && newWeapon != (int)WeaponType.Bfg) ||
                        (world.Options.GameMode != GameMode.Shareware))
                    {
                        player.PendingWeapon = (WeaponType)newWeapon;
                    }
                }
            }

            // Check for use.
            if ((cmd.Buttons & TicCmdButtons.Use) != 0)
            {
                if (!player.UseDown)
                {
                    world.MapInteraction.UseLines(player);
                    player.UseDown = true;
                }
            }
            else
            {
                player.UseDown = false;
            }

            // Cycle player sprites.
            MovePlayerSprites(player);

            // Counters, time dependend power ups.

            // Strength counts up to diminish fade.
            if (player.Powers[(int)PowerType.Strength] != 0)
            {
                player.Powers[(int)PowerType.Strength]++;
            }

            if (player.Powers[(int)PowerType.Invulnerability] > 0)
            {
                player.Powers[(int)PowerType.Invulnerability]--;
            }

            if (player.Powers[(int)PowerType.Invisibility] > 0)
            {
                if (--player.Powers[(int)PowerType.Invisibility] == 0)
                {
                    player.Mobj.Flags &= ~MobjFlags.Shadow;
                }
            }

            if (player.Powers[(int)PowerType.Infrared] > 0)
            {
                player.Powers[(int)PowerType.Infrared]--;
            }

            if (player.Powers[(int)PowerType.IronFeet] > 0)
            {
                player.Powers[(int)PowerType.IronFeet]--;
            }

            if (player.DamageCount > 0)
            {
                player.DamageCount--;
            }

            if (player.BonusCount > 0)
            {
                player.BonusCount--;
            }

            // Handling colormaps.
            if (player.Powers[(int)PowerType.Invulnerability] > 0)
            {
                if (player.Powers[(int)PowerType.Invulnerability] > 4 * 32 ||
                    (player.Powers[(int)PowerType.Invulnerability] & 8) != 0)
                {
                    player.FixedColorMap = ColorMap.Inverse;
                }
                else
                {
                    player.FixedColorMap = 0;
                }
            }
            else if (player.Powers[(int)PowerType.Infrared] > 0)
            {
                if (player.Powers[(int)PowerType.Infrared] > 4 * 32 ||
                    (player.Powers[(int)PowerType.Infrared] & 8) != 0)
                {
                    // Almost full bright.
                    player.FixedColorMap = 1;
                }
                else
                {
                    player.FixedColorMap = 0;
                }
            }
            else
            {
                player.FixedColorMap = 0;
            }
        }


        private static readonly Fixed maxBob = new Fixed(0x100000);

        private bool onGround;

        /// <summary>
        /// Move the player according to TicCmd.
        /// </summary>
        public void MovePlayer(Player player)
        {
            var cmd = player.Cmd;

            player.Mobj.Angle += new Angle(cmd.AngleTurn << 16);

            // Do not let the player control movement if not onground.
            onGround = (player.Mobj.Z <= player.Mobj.FloorZ);

            if (cmd.ForwardMove != 0 && onGround)
            {
                Thrust(player, player.Mobj.Angle, new Fixed(cmd.ForwardMove * 2048));
            }

            if (cmd.SideMove != 0 && onGround)
            {
                Thrust(player, player.Mobj.Angle - Angle.Ang90, new Fixed(cmd.SideMove * 2048));
            }

            if ((cmd.ForwardMove != 0 || cmd.SideMove != 0) &&
                player.Mobj.State == DoomInfo.States[(int)MobjState.Play])
            {
                player.Mobj.SetState(MobjState.PlayRun1);
            }
        }


        /// <summary>
        /// Calculate the walking / running height adjustment.
        /// </summary>
        public void CalcHeight(Player player)
        {
            // Regular movement bobbing.
            // It needs to be calculated for gun swing even if not on ground.
            player.Bob = player.Mobj.MomX * player.Mobj.MomX + player.Mobj.MomY * player.Mobj.MomY;
            player.Bob >>= 2;
            if (player.Bob > maxBob)
            {
                player.Bob = maxBob;
            }

            if ((player.Cheats & CheatFlags.NoMomentum) != 0 || !onGround)
            {
                player.ViewZ = player.Mobj.Z + Player.NormalViewHeight;

                if (player.ViewZ > player.Mobj.CeilingZ - Fixed.FromInt(4))
                {
                    player.ViewZ = player.Mobj.CeilingZ - Fixed.FromInt(4);
                }

                player.ViewZ = player.Mobj.Z + player.ViewHeight;

                return;
            }

            var angle = (Trig.FineAngleCount / 20 * world.LevelTime) & Trig.FineMask;

            var bob = (player.Bob / 2) * Trig.Sin(angle);

            // Move viewheight.
            if (player.PlayerState == PlayerState.Live)
            {
                player.ViewHeight += player.DeltaViewHeight;

                if (player.ViewHeight > Player.NormalViewHeight)
                {
                    player.ViewHeight = Player.NormalViewHeight;
                    player.DeltaViewHeight = Fixed.Zero;
                }

                if (player.ViewHeight < Player.NormalViewHeight / 2)
                {
                    player.ViewHeight = Player.NormalViewHeight / 2;

                    if (player.DeltaViewHeight <= Fixed.Zero)
                    {
                        player.DeltaViewHeight = new Fixed(1);
                    }
                }

                if (player.DeltaViewHeight != Fixed.Zero)
                {
                    player.DeltaViewHeight += Fixed.One / 4;

                    if (player.DeltaViewHeight == Fixed.Zero)
                    {
                        player.DeltaViewHeight = new Fixed(1);
                    }
                }
            }

            player.ViewZ = player.Mobj.Z + player.ViewHeight + bob;

            if (player.ViewZ > player.Mobj.CeilingZ - Fixed.FromInt(4))
            {
                player.ViewZ = player.Mobj.CeilingZ - Fixed.FromInt(4);
            }
        }


        /// <summary>
        /// Moves the given origin along a given angle.
        /// </summary>
        public void Thrust(Player player, Angle angle, Fixed move)
        {
            player.Mobj.MomX += move * Trig.Cos(angle);
            player.Mobj.MomY += move * Trig.Sin(angle);
        }


        /// <summary>
        /// Called every tic frame that the player origin is in a special sector.
        /// </summary>
        private void PlayerInSpecialSector(Player player)
        {
            var sector = player.Mobj.Subsector.Sector;

            // Falling, not all the way down yet?
            if (player.Mobj.Z != sector.FloorHeight)
            {
                return;
            }

            var ti = world.ThingInteraction;

            // Has hitten ground.
            switch ((int)sector.Special)
            {
                case 5:
                    // Hell slime damage.
                    if (player.Powers[(int)PowerType.IronFeet] == 0)
                    {
                        if ((world.LevelTime & 0x1f) == 0)
                        {
                            ti.DamageMobj(player.Mobj, null, null, 10);
                        }
                    }
                    break;

                case 7:
                    // Nukage damage.
                    if (player.Powers[(int)PowerType.IronFeet] == 0)
                    {
                        if ((world.LevelTime & 0x1f) == 0)
                        {
                            ti.DamageMobj(player.Mobj, null, null, 5);
                        }
                    }
                    break;

                case 16:
                    // Super hell slime damage.
                case 4:
                    // Strobe hurt.
                    if (player.Powers[(int)PowerType.IronFeet] == 0 || (world.Random.Next() < 5))
                    {
                        if ((world.LevelTime & 0x1f) == 0)
                        {
                            ti.DamageMobj(player.Mobj, null, null, 20);
                        }
                    }
                    break;

                case 9:
                    // Secret sector.
                    player.SecretCount++;
                    sector.Special = 0;
                    break;

                case 11:
                    // Exit super damage for E1M8 finale.
                    player.Cheats &= ~CheatFlags.GodMode;
                    if ((world.LevelTime & 0x1f) == 0)
                    {
                        ti.DamageMobj(player.Mobj, null, null, 20);
                    }
                    if (player.Health <= 10)
                    {
                        world.ExitLevel();
                    }
                    break;

                default:
                    throw new Exception("Unknown sector special: " + (int)sector.Special);
            }
        }


        private static Angle ang5 = new Angle(Angle.Ang90.Data / 18);

        /// <summary>
        /// Fall on your face when dying.
        /// Decrease POV height to floor height.
        /// </summary>
        private void DeathThink(Player player)
        {
            MovePlayerSprites(player);

            // Fall to the ground.
            if (player.ViewHeight > Fixed.FromInt(6))
            {
                player.ViewHeight -= Fixed.One;
            }

            if (player.ViewHeight < Fixed.FromInt(6))
            {
                player.ViewHeight = Fixed.FromInt(6);
            }

            player.DeltaViewHeight = Fixed.Zero;
            onGround = (player.Mobj.Z <= player.Mobj.FloorZ);
            CalcHeight(player);

            if (player.Attacker != null && player.Attacker != player.Mobj)
            {
                var angle = Geometry.PointToAngle(
                    player.Mobj.X, player.Mobj.Y,
                    player.Attacker.X, player.Attacker.Y);

                var delta = angle - player.Mobj.Angle;

                if (delta < ang5 || delta.Data > (-ang5).Data)
                {
                    // Looking at killer, so fade damage flash down.
                    player.Mobj.Angle = angle;

                    if (player.DamageCount > 0)
                    {
                        player.DamageCount--;
                    }
                }
                else if (delta < Angle.Ang180)
                {
                    player.Mobj.Angle += ang5;
                }
                else
                {
                    player.Mobj.Angle -= ang5;
                }
            }
            else if (player.DamageCount > 0)
            {
                player.DamageCount--;
            }

            if ((player.Cmd.Buttons & TicCmdButtons.Use) != 0)
            {
                player.PlayerState = PlayerState.Reborn;
            }
        }



        ////////////////////////////////////////////////////////////
        // Player's weapon sprites
        ////////////////////////////////////////////////////////////

        /// <summary>
        /// Called at start of level for each player.
        /// </summary>
        public void SetupPlayerSprites(Player player)
        {
            // Remove all psprites.
            for (var i = 0; i < (int)PlayerSprite.Count; i++)
            {
                player.PlayerSprites[i].State = null;
            }

            // Spawn the gun.
            player.PendingWeapon = player.ReadyWeapon;
            BringUpWeapon(player);
        }

        /// <summary>
        /// Starts bringing the pending weapon up from the bottom of the screen.
        /// </summary>
        public void BringUpWeapon(Player player)
        {
            if (player.PendingWeapon == WeaponType.NoChange)
            {
                player.PendingWeapon = player.ReadyWeapon;
            }

            if (player.PendingWeapon == WeaponType.Chainsaw)
            {
                world.StartSound(player.Mobj, Sfx.SAWUP, SfxType.Weapon);
            }

            var newState = DoomInfo.WeaponInfos[(int)player.PendingWeapon].UpState;

            player.PendingWeapon = WeaponType.NoChange;
            player.PlayerSprites[(int)PlayerSprite.Weapon].Sy = WeaponBehavior.WeaponBottom;

            SetPlayerSprite(player, PlayerSprite.Weapon, newState);
        }

        /// <summary>
        /// Change the player's weapon sprite.
        /// </summary>
        public void SetPlayerSprite(Player player, PlayerSprite position, MobjState state)
        {
            var psp = player.PlayerSprites[(int)position];

            do
            {
                if (state == MobjState.Null)
                {
                    // Object removed itself.
                    psp.State = null;
                    break;
                }

                var stateDef = DoomInfo.States[(int)state];
                psp.State = stateDef;
                psp.Tics = stateDef.Tics; // Could be 0.

                if (stateDef.Misc1 != 0)
                {
                    // Coordinate set.
                    psp.Sx = Fixed.FromInt(stateDef.Misc1);
                    psp.Sy = Fixed.FromInt(stateDef.Misc2);
                }

                // Call action routine.
                // Modified handling.
                if (stateDef.PlayerAction != null)
                {
                    stateDef.PlayerAction(world, player, psp);
                    if (psp.State == null)
                    {
                        break;
                    }
                }

                state = psp.State.Next;

            } while (psp.Tics == 0);
            // An initial state of 0 could cycle through.
        }

        /// <summary>
        /// Called every tic by player thinking routine.
        /// </summary>
        private void MovePlayerSprites(Player player)
        {
            for (var i = 0; i < (int)PlayerSprite.Count; i++)
            {
                var psp = player.PlayerSprites[i];

                MobjStateDef stateDef;

                // A null state means not active.
                if ((stateDef = psp.State) != null)
                {
                    // Drop tic count and possibly change state.

                    // A -1 tic count never changes.
                    if (psp.Tics != -1)
                    {
                        psp.Tics--;
                        if (psp.Tics == 0)
                        {
                            SetPlayerSprite(player, (PlayerSprite)i, psp.State.Next);
                        }
                    }
                }
            }

            player.PlayerSprites[(int)PlayerSprite.Flash].Sx = player.PlayerSprites[(int)PlayerSprite.Weapon].Sx;
            player.PlayerSprites[(int)PlayerSprite.Flash].Sy = player.PlayerSprites[(int)PlayerSprite.Weapon].Sy;
        }

        /// <summary>
        /// Player died, so put the weapon away.
        /// </summary>
        public void DropWeapon(Player player)
        {
            SetPlayerSprite(
                player,
                PlayerSprite.Weapon,
                DoomInfo.WeaponInfos[(int)player.ReadyWeapon].DownState);
        }



        ////////////////////////////////////////////////////////////
        // Miscellaneous
        ////////////////////////////////////////////////////////////

        /// <summary>
        /// Play the player's death sound.
        /// </summary>
        public void PlayerScream(Mobj player)
        {
            // Default death sound.
            var sound = Sfx.PLDETH;

            if ((world.Options.GameMode == GameMode.Commercial) && (player.Health < -50))
            {
                // If the player dies less than -50% without gibbing.
                sound = Sfx.PDIEHI;
            }

            world.StartSound(player, sound, SfxType.Voice);
        }
    }
}
