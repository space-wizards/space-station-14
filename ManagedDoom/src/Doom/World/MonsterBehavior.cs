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
    public sealed class MonsterBehavior
    {
        private World world;

        public MonsterBehavior(World world)
        {
            this.world = world;

            InitVile();
            InitBossDeath();
            InitBrain();
        }



        ////////////////////////////////////////////////////////////
        // Sleeping monster
        ////////////////////////////////////////////////////////////

        private bool LookForPlayers(Mobj actor, bool allAround)
        {
            var players = world.Options.Players;

            var count = 0;
            var stop = (actor.LastLook - 1) & 3;

            for (; ; actor.LastLook = (actor.LastLook + 1) & 3)
            {
                if (!players[actor.LastLook].InGame)
                {
                    continue;
                }

                if (count++ == 2 || actor.LastLook == stop)
                {
                    // Done looking.
                    return false;
                }

                var player = players[actor.LastLook];

                if (player.Health <= 0)
                {
                    // Player is dead.
                    continue;
                }

                if (!world.VisibilityCheck.CheckSight(actor, player.Mobj))
                {
                    // Out of sight.
                    continue;
                }

                if (!allAround)
                {
                    var angle = Geometry.PointToAngle(
                        actor.X, actor.Y,
                        player.Mobj.X, player.Mobj.Y) - actor.Angle;

                    if (angle > Angle.Ang90 && angle < Angle.Ang270)
                    {
                        var dist = Geometry.AproxDistance(
                            player.Mobj.X - actor.X,
                            player.Mobj.Y - actor.Y);

                        // If real close, react anyway.
                        if (dist > WeaponBehavior.MeleeRange)
                        {
                            // Behind back.
                            continue;
                        }
                    }
                }

                actor.Target = player.Mobj;

                return true;
            }
        }


        public void Look(Mobj actor)
        {
            // Any shot will wake up.
            actor.Threshold = 0;

            var target = actor.Subsector.Sector.SoundTarget;

            if (target != null && (target.Flags & MobjFlags.Shootable) != 0)
            {
                actor.Target = target;

                if ((actor.Flags & MobjFlags.Ambush) != 0)
                {
                    if (world.VisibilityCheck.CheckSight(actor, actor.Target))
                    {
                        goto seeYou;
                    }
                }
                else
                {
                    goto seeYou;
                }
            }

            if (!LookForPlayers(actor, false))
            {
                return;
            }

            // Go into chase state.
            seeYou:
            if (actor.Info.SeeSound != 0)
            {
                int sound;

                switch (actor.Info.SeeSound)
                {
                    case Sfx.POSIT1:
                    case Sfx.POSIT2:
                    case Sfx.POSIT3:
                        sound = (int)Sfx.POSIT1 + world.Random.Next() % 3;
                        break;

                    case Sfx.BGSIT1:
                    case Sfx.BGSIT2:
                        sound = (int)Sfx.BGSIT1 + world.Random.Next() % 2;
                        break;

                    default:
                        sound = (int)actor.Info.SeeSound;
                        break;
                }

                if (actor.Type == MobjType.Spider || actor.Type == MobjType.Cyborg)
                {
                    // Full volume for boss monsters.
                    world.StartSound(actor, (Sfx)sound, SfxType.Diffuse);
                }
                else
                {
                    world.StartSound(actor, (Sfx)sound, SfxType.Voice);
                }
            }

            actor.SetState(actor.Info.SeeState);
        }



        ////////////////////////////////////////////////////////////
        // Monster AI
        ////////////////////////////////////////////////////////////

        private static readonly Fixed[] xSpeed =
        {
            new Fixed(Fixed.FracUnit),
            new Fixed(47000),
            new Fixed(0),
            new Fixed(-47000),
            new Fixed(-Fixed.FracUnit),
            new Fixed(-47000),
            new Fixed(0),
            new Fixed(47000)
        };

        private static readonly Fixed[] ySpeed =
        {
            new Fixed(0),
            new Fixed(47000),
            new Fixed(Fixed.FracUnit),
            new Fixed(47000),
            new Fixed(0),
            new Fixed(-47000),
            new Fixed(-Fixed.FracUnit),
            new Fixed(-47000)
        };

        private bool Move(Mobj actor)
        {
            if (actor.MoveDir == Direction.None)
            {
                return false;
            }

            if ((int)actor.MoveDir >= 8)
            {
                throw new Exception("Weird actor->movedir!");
            }

            var tryX = actor.X + actor.Info.Speed * xSpeed[(int)actor.MoveDir];
            var tryY = actor.Y + actor.Info.Speed * ySpeed[(int)actor.MoveDir];

            var tm = world.ThingMovement;

            var tryOk = tm.TryMove(actor, tryX, tryY);

            if (!tryOk)
            {
                // Open any specials.
                if ((actor.Flags & MobjFlags.Float) != 0 && tm.FloatOk)
                {
                    // Must adjust height.
                    if (actor.Z < tm.CurrentFloorZ)
                    {
                        actor.Z += ThingMovement.FloatSpeed;
                    }
                    else
                    {
                        actor.Z -= ThingMovement.FloatSpeed;
                    }

                    actor.Flags |= MobjFlags.InFloat;

                    return true;
                }

                if (tm.crossedSpecialCount == 0)
                {
                    return false;
                }

                actor.MoveDir = Direction.None;
                var good = false;
                while (tm.crossedSpecialCount-- > 0)
                {
                    var line = tm.crossedSpecials[tm.crossedSpecialCount];
                    // If the special is not a door that can be opened,
                    // return false.
                    if (world.MapInteraction.UseSpecialLine(actor, line, 0))
                    {
                        good = true;
                    }
                }
                return good;
            }
            else
            {
                actor.Flags &= ~MobjFlags.InFloat;
            }

            if ((actor.Flags & MobjFlags.Float) == 0)
            {
                actor.Z = actor.FloorZ;
            }

            return true;
        }


        private bool TryWalk(Mobj actor)
        {
            if (!Move(actor))
            {
                return false;
            }

            actor.MoveCount = world.Random.Next() & 15;

            return true;
        }


        private static readonly Direction[] opposite =
        {
            Direction.west,
            Direction.Southwest,
            Direction.South,
            Direction.Southeast,
            Direction.East,
            Direction.Northeast,
            Direction.North,
            Direction.Northwest,
            Direction.None
        };

        private static readonly Direction[] diags =
        {
            Direction.Northwest,
            Direction.Northeast,
            Direction.Southwest,
            Direction.Southeast
        };

        private readonly Direction[] choices = new Direction[3];

        private void NewChaseDir(Mobj actor)
        {
            if (actor.Target == null)
            {
                throw new Exception("Called with no target.");
            }

            var oldDir = actor.MoveDir;
            var turnAround = opposite[(int)oldDir];

            var deltaX = actor.Target.X - actor.X;
            var deltaY = actor.Target.Y - actor.Y;

            if (deltaX > Fixed.FromInt(10))
            {
                choices[1] = Direction.East;
            }
            else if (deltaX < Fixed.FromInt(-10))
            {
                choices[1] = Direction.west;
            }
            else
            {
                choices[1] = Direction.None;
            }

            if (deltaY < Fixed.FromInt(-10))
            {
                choices[2] = Direction.South;
            }
            else if (deltaY > Fixed.FromInt(10))
            {
                choices[2] = Direction.North;
            }
            else
            {
                choices[2] = Direction.None;
            }

            // Try direct route.
            if (choices[1] != Direction.None && choices[2] != Direction.None)
            {
                var a = (deltaY < Fixed.Zero) ? 1 : 0;
                var b = (deltaX > Fixed.Zero) ? 1 : 0;
                actor.MoveDir = diags[(a << 1) + b];

                if (actor.MoveDir != turnAround && TryWalk(actor))
                {
                    return;
                }
            }

            // Try other directions.
            if (world.Random.Next() > 200 || Fixed.Abs(deltaY) > Fixed.Abs(deltaX))
            {
                var temp = choices[1];
                choices[1] = choices[2];
                choices[2] = temp;
            }

            if (choices[1] == turnAround)
            {
                choices[1] = Direction.None;
            }

            if (choices[2] == turnAround)
            {
                choices[2] = Direction.None;
            }

            if (choices[1] != Direction.None)
            {
                actor.MoveDir = choices[1];

                if (TryWalk(actor))
                {
                    // Either moved forward or attacked.
                    return;
                }
            }

            if (choices[2] != Direction.None)
            {
                actor.MoveDir = choices[2];

                if (TryWalk(actor))
                {
                    return;
                }
            }

            // There is no direct path to the player, so pick another direction.
            if (oldDir != Direction.None)
            {
                actor.MoveDir = oldDir;

                if (TryWalk(actor))
                {
                    return;
                }
            }

            // Randomly determine direction of search.
            if ((world.Random.Next() & 1) != 0)
            {
                for (var dir = (int)Direction.East; dir <= (int)Direction.Southeast; dir++)
                {
                    if ((Direction)dir != turnAround)
                    {
                        actor.MoveDir = (Direction)dir;

                        if (TryWalk(actor))
                        {
                            return;
                        }
                    }
                }
            }
            else
            {
                for (var dir = (int)Direction.Southeast; dir != ((int)Direction.East - 1); dir--)
                {
                    if ((Direction)dir != turnAround)
                    {
                        actor.MoveDir = (Direction)dir;

                        if (TryWalk(actor))
                        {
                            return;
                        }
                    }
                }
            }

            if (turnAround != Direction.None)
            {
                actor.MoveDir = turnAround;

                if (TryWalk(actor))
                {
                    return;
                }
            }

            // Can not move.
            actor.MoveDir = Direction.None;
        }


        private bool CheckMeleeRange(Mobj actor)
        {
            if (actor.Target == null)
            {
                return false;
            }

            var target = actor.Target;

            var dist = Geometry.AproxDistance(target.X - actor.X, target.Y - actor.Y);

            if (dist >= WeaponBehavior.MeleeRange - Fixed.FromInt(20) + target.Info.Radius)
            {
                return false;
            }

            if (!world.VisibilityCheck.CheckSight(actor, actor.Target))
            {
                return false;
            }

            return true;
        }


        private bool CheckMissileRange(Mobj actor)
        {
            if (!world.VisibilityCheck.CheckSight(actor, actor.Target))
            {
                return false;
            }

            if ((actor.Flags & MobjFlags.JustHit) != 0)
            {
                // The target just hit the enemy, so fight back!
                actor.Flags &= ~MobjFlags.JustHit;

                return true;
            }

            if (actor.ReactionTime > 0)
            {
                // Do not attack yet
                return false;
            }

            // OPTIMIZE:
            //     Get this from a global checksight.
            var dist = Geometry.AproxDistance(
                actor.X - actor.Target.X,
                actor.Y - actor.Target.Y) - Fixed.FromInt(64);

            if (actor.Info.MeleeState == 0)
            {
                // No melee attack, so fire more.
                dist -= Fixed.FromInt(128);
            }

            var attackDist = dist.Data >> 16;

            if (actor.Type == MobjType.Vile)
            {
                if (attackDist > 14 * 64)
                {
                    // Too far away.
                    return false;
                }
            }

            if (actor.Type == MobjType.Undead)
            {
                if (attackDist < 196)
                {
                    // Close for fist attack.
                    return false;
                }

                attackDist >>= 1;
            }


            if (actor.Type == MobjType.Cyborg ||
                actor.Type == MobjType.Spider ||
                actor.Type == MobjType.Skull)
            {
                attackDist >>= 1;
            }

            if (attackDist > 200)
            {
                attackDist = 200;
            }

            if (actor.Type == MobjType.Cyborg && attackDist > 160)
            {
                attackDist = 160;
            }

            if (world.Random.Next() < attackDist)
            {
                return false;
            }

            return true;
        }


        public void Chase(Mobj actor)
        {
            if (actor.ReactionTime > 0)
            {
                actor.ReactionTime--;
            }

            // Modify target threshold.
            if (actor.Threshold > 0)
            {
                if (actor.Target == null || actor.Target.Health <= 0)
                {
                    actor.Threshold = 0;
                }
                else
                {
                    actor.Threshold--;
                }
            }

            // Turn towards movement direction if not there yet.
            if ((int)actor.MoveDir < 8)
            {
                actor.Angle = new Angle((int)actor.Angle.Data & (7 << 29));

                var delta = (int)(actor.Angle - new Angle((int)actor.MoveDir << 29)).Data;

                if (delta > 0)
                {
                    actor.Angle -= new Angle(Angle.Ang90.Data / 2);
                }
                else if (delta < 0)
                {
                    actor.Angle += new Angle(Angle.Ang90.Data / 2);
                }
            }

            if (actor.Target == null || (actor.Target.Flags & MobjFlags.Shootable) == 0)
            {
                // Look for a new target.
                if (LookForPlayers(actor, true))
                {
                    // Got a new target.
                    return;
                }

                actor.SetState(actor.Info.SpawnState);

                return;
            }

            // Do not attack twice in a row.
            if ((actor.Flags & MobjFlags.JustAttacked) != 0)
            {
                actor.Flags &= ~MobjFlags.JustAttacked;

                if (world.Options.Skill != GameSkill.Nightmare &&
                    !world.Options.FastMonsters)
                {
                    NewChaseDir(actor);
                }

                return;
            }

            // Check for melee attack.
            if (actor.Info.MeleeState != 0 && CheckMeleeRange(actor))
            {
                if (actor.Info.AttackSound != 0)
                {
                    world.StartSound(actor, actor.Info.AttackSound, SfxType.Weapon);
                }

                actor.SetState(actor.Info.MeleeState);

                return;
            }

            // Check for missile attack.
            if (actor.Info.MissileState != 0)
            {
                if (world.Options.Skill < GameSkill.Nightmare &&
                    !world.Options.FastMonsters &&
                    actor.MoveCount != 0)
                {
                    goto noMissile;
                }

                if (!CheckMissileRange(actor))
                {
                    goto noMissile;
                }

                actor.SetState(actor.Info.MissileState);
                actor.Flags |= MobjFlags.JustAttacked;

                return;
            }

            noMissile:
            // Possibly choose another target.
            if (world.Options.NetGame &&
                actor.Threshold == 0 &&
                !world.VisibilityCheck.CheckSight(actor, actor.Target))
            {
                if (LookForPlayers(actor, true))
                {
                    // Got a new target.
                    return;
                }
            }

            // Chase towards player.
            if (--actor.MoveCount < 0 || !Move(actor))
            {
                NewChaseDir(actor);
            }

            // Make active sound.
            if (actor.Info.ActiveSound != 0 && world.Random.Next() < 3)
            {
                world.StartSound(actor, actor.Info.ActiveSound, SfxType.Voice);
            }
        }



        ////////////////////////////////////////////////////////////
        // Monster death
        ////////////////////////////////////////////////////////////

        public void Pain(Mobj actor)
        {
            if (actor.Info.PainSound != 0)
            {
                world.StartSound(actor, actor.Info.PainSound, SfxType.Voice);
            }
        }

        public void Scream(Mobj actor)
        {
            int sound;

            switch (actor.Info.DeathSound)
            {
                case 0:
                    return;

                case Sfx.PODTH1:
                case Sfx.PODTH2:
                case Sfx.PODTH3:
                    sound = (int)Sfx.PODTH1 + world.Random.Next() % 3;
                    break;

                case Sfx.BGDTH1:
                case Sfx.BGDTH2:
                    sound = (int)Sfx.BGDTH1 + world.Random.Next() % 2;
                    break;

                default:
                    sound = (int)actor.Info.DeathSound;
                    break;
            }

            // Check for bosses.
            if (actor.Type == MobjType.Spider || actor.Type == MobjType.Cyborg)
            {
                // Full volume.
                world.StartSound(actor, (Sfx)sound, SfxType.Diffuse);
            }
            else
            {
                world.StartSound(actor, (Sfx)sound, SfxType.Voice);
            }
        }

        public void XScream(Mobj actor)
        {
            world.StartSound(actor, Sfx.SLOP, SfxType.Voice);
        }

        public void Fall(Mobj actor)
        {
            // Actor is on ground, it can be walked over.
            actor.Flags &= ~MobjFlags.Solid;
        }



        ////////////////////////////////////////////////////////////
        // Monster attack
        ////////////////////////////////////////////////////////////

        public void FaceTarget(Mobj actor)
        {
            if (actor.Target == null)
            {
                return;
            }

            actor.Flags &= ~MobjFlags.Ambush;

            actor.Angle = Geometry.PointToAngle(
                actor.X, actor.Y,
                actor.Target.X, actor.Target.Y);

            var random = world.Random;

            if ((actor.Target.Flags & MobjFlags.Shadow) != 0)
            {
                actor.Angle += new Angle((random.Next() - random.Next()) << 21);
            }
        }


        public void PosAttack(Mobj actor)
        {
            if (actor.Target == null)
            {
                return;
            }

            FaceTarget(actor);

            var angle = actor.Angle;
            var slope = world.Hitscan.AimLineAttack(actor, angle, WeaponBehavior.MissileRange);

            world.StartSound(actor, Sfx.PISTOL, SfxType.Weapon);

            var random = world.Random;
            angle += new Angle((random.Next() - random.Next()) << 20);
            var damage = ((random.Next() % 5) + 1) * 3;

            world.Hitscan.LineAttack(actor, angle, WeaponBehavior.MissileRange, slope, damage);
        }


        public void SPosAttack(Mobj actor)
        {
            if (actor.Target == null)
            {
                return;
            }

            world.StartSound(actor, Sfx.SHOTGN, SfxType.Weapon);

            FaceTarget(actor);

            var center = actor.Angle;
            var slope = world.Hitscan.AimLineAttack(actor, center, WeaponBehavior.MissileRange);

            var random = world.Random;

            for (var i = 0; i < 3; i++)
            {
                var angle = center + new Angle((random.Next() - random.Next()) << 20);
                var damage = ((random.Next() % 5) + 1) * 3;

                world.Hitscan.LineAttack(actor, angle, WeaponBehavior.MissileRange, slope, damage);
            }
        }


        public void CPosAttack(Mobj actor)
        {
            if (actor.Target == null)
            {
                return;
            }

            world.StartSound(actor, Sfx.SHOTGN, SfxType.Weapon);

            FaceTarget(actor);

            var center = actor.Angle;
            var slope = world.Hitscan.AimLineAttack(actor, center, WeaponBehavior.MissileRange);

            var random = world.Random;
            var angle = center + new Angle((random.Next() - random.Next()) << 20);
            var damage = ((random.Next() % 5) + 1) * 3;

            world.Hitscan.LineAttack(actor, angle, WeaponBehavior.MissileRange, slope, damage);
        }


        public void CPosRefire(Mobj actor)
        {
            // Keep firing unless target got out of sight.
            FaceTarget(actor);

            if (world.Random.Next() < 40)
            {
                return;
            }

            if (actor.Target == null ||
                actor.Target.Health <= 0 ||
                !world.VisibilityCheck.CheckSight(actor, actor.Target))
            {
                actor.SetState(actor.Info.SeeState);
            }
        }


        public void TroopAttack(Mobj actor)
        {
            if (actor.Target == null)
            {
                return;
            }

            FaceTarget(actor);

            if (CheckMeleeRange(actor))
            {
                world.StartSound(actor, Sfx.CLAW, SfxType.Weapon);

                var damage = (world.Random.Next() % 8 + 1) * 3;
                world.ThingInteraction.DamageMobj(actor.Target, actor, actor, damage);

                return;
            }

            // Launch a missile.
            world.ThingAllocation.SpawnMissile(actor, actor.Target, MobjType.Troopshot);
        }


        public void SargAttack(Mobj actor)
        {
            if (actor.Target == null)
            {
                return;
            }

            FaceTarget(actor);

            if (CheckMeleeRange(actor))
            {
                var damage = ((world.Random.Next() % 10) + 1) * 4;
                world.ThingInteraction.DamageMobj(actor.Target, actor, actor, damage);
            }
        }


        public void HeadAttack(Mobj actor)
        {
            if (actor.Target == null)
            {
                return;
            }

            FaceTarget(actor);

            if (CheckMeleeRange(actor))
            {
                var damage = (world.Random.Next() % 6 + 1) * 10;
                world.ThingInteraction.DamageMobj(actor.Target, actor, actor, damage);

                return;
            }

            // Launch a missile.
            world.ThingAllocation.SpawnMissile(actor, actor.Target, MobjType.Headshot);
        }


        public void BruisAttack(Mobj actor)
        {
            if (actor.Target == null)
            {
                return;
            }

            if (CheckMeleeRange(actor))
            {
                world.StartSound(actor, Sfx.CLAW, SfxType.Weapon);

                var damage = (world.Random.Next() % 8 + 1) * 10;
                world.ThingInteraction.DamageMobj(actor.Target, actor, actor, damage);

                return;
            }

            // Launch a missile.
            world.ThingAllocation.SpawnMissile(actor, actor.Target, MobjType.Bruisershot);
        }


        private static readonly Fixed skullSpeed = Fixed.FromInt(20);

        public void SkullAttack(Mobj actor)
        {
            if (actor.Target == null)
            {
                return;
            }

            var dest = actor.Target;

            actor.Flags |= MobjFlags.SkullFly;

            world.StartSound(actor, actor.Info.AttackSound, SfxType.Voice);

            FaceTarget(actor);

            var angle = actor.Angle;
            actor.MomX = skullSpeed * Trig.Cos(angle);
            actor.MomY = skullSpeed * Trig.Sin(angle);

            var dist = Geometry.AproxDistance(dest.X - actor.X, dest.Y - actor.Y);

            var num = (dest.Z + (dest.Height >> 1) - actor.Z).Data;
            var den = dist.Data / skullSpeed.Data;
            if (den < 1)
            {
                den = 1;
            }

            actor.MomZ = new Fixed(num / den);
        }


        public void FatRaise(Mobj actor)
        {
            FaceTarget(actor);

            world.StartSound(actor, Sfx.MANATK, SfxType.Voice);
        }


        private static readonly Angle fatSpread = Angle.Ang90 / 8;

        public void FatAttack1(Mobj actor)
        {
            FaceTarget(actor);

            var ta = world.ThingAllocation;

            // Change direction to...
            actor.Angle += fatSpread;
            var target = world.SubstNullMobj(actor.Target);
            ta.SpawnMissile(actor, target, MobjType.Fatshot);

            var missile = ta.SpawnMissile(actor, target, MobjType.Fatshot);
            missile.Angle += fatSpread;
            var angle = missile.Angle;
            missile.MomX = new Fixed(missile.Info.Speed) * Trig.Cos(angle);
            missile.MomY = new Fixed(missile.Info.Speed) * Trig.Sin(angle);
        }

        public void FatAttack2(Mobj actor)
        {
            FaceTarget(actor);

            var ta = world.ThingAllocation;

            // Now here choose opposite deviation.
            actor.Angle -= fatSpread;
            var target = world.SubstNullMobj(actor.Target);
            ta.SpawnMissile(actor, target, MobjType.Fatshot);

            var missile = ta.SpawnMissile(actor, target, MobjType.Fatshot);
            missile.Angle -= fatSpread * 2;
            var angle = missile.Angle;
            missile.MomX = new Fixed(missile.Info.Speed) * Trig.Cos(angle);
            missile.MomY = new Fixed(missile.Info.Speed) * Trig.Sin(angle);
        }

        public void FatAttack3(Mobj actor)
        {
            FaceTarget(actor);

            var ta = world.ThingAllocation;

            var target = world.SubstNullMobj(actor.Target);

            var missile1 = ta.SpawnMissile(actor, target, MobjType.Fatshot);
            missile1.Angle -= fatSpread / 2;
            var angle1 = missile1.Angle;
            missile1.MomX = new Fixed(missile1.Info.Speed) * Trig.Cos(angle1);
            missile1.MomY = new Fixed(missile1.Info.Speed) * Trig.Sin(angle1);

            var missile2 = ta.SpawnMissile(actor, target, MobjType.Fatshot);
            missile2.Angle += fatSpread / 2;
            var angle2 = missile2.Angle;
            missile2.MomX = new Fixed(missile2.Info.Speed) * Trig.Cos(angle2);
            missile2.MomY = new Fixed(missile2.Info.Speed) * Trig.Sin(angle2);
        }


        public void BspiAttack(Mobj actor)
        {
            if (actor.Target == null)
            {
                return;
            }

            FaceTarget(actor);

            // Launch a missile.
            world.ThingAllocation.SpawnMissile(actor, actor.Target, MobjType.Arachplaz);
        }


        public void SpidRefire(Mobj actor)
        {
            // Keep firing unless target got out of sight.
            FaceTarget(actor);

            if (world.Random.Next() < 10)
            {
                return;
            }

            if (actor.Target == null ||
                actor.Target.Health <= 0 ||
                !world.VisibilityCheck.CheckSight(actor, actor.Target))
            {
                actor.SetState(actor.Info.SeeState);
            }
        }


        public void CyberAttack(Mobj actor)
        {
            if (actor.Target == null)
            {
                return;
            }

            FaceTarget(actor);

            world.ThingAllocation.SpawnMissile(actor, actor.Target, MobjType.Rocket);
        }



        ////////////////////////////////////////////////////////////
        // Miscellaneous
        ////////////////////////////////////////////////////////////

        public void Explode(Mobj actor)
        {
            world.ThingInteraction.RadiusAttack(actor, actor.Target, 128);
        }


        public void Metal(Mobj actor)
        {
            world.StartSound(actor, Sfx.METAL, SfxType.Footstep);

            Chase(actor);
        }


        public void BabyMetal(Mobj actor)
        {
            world.StartSound(actor, Sfx.BSPWLK, SfxType.Footstep);

            Chase(actor);
        }


        public void Hoof(Mobj actor)
        {
            world.StartSound(actor, Sfx.HOOF, SfxType.Footstep);

            Chase(actor);
        }



        ////////////////////////////////////////////////////////////
        // Arch vile
        ////////////////////////////////////////////////////////////

        private Func<Mobj, bool> vileCheckFunc;
        private Mobj vileTargetCorpse;
        private Fixed vileTryX;
        private Fixed vileTryY;

        private void InitVile()
        {
            vileCheckFunc = VileCheck;
        }


        private bool VileCheck(Mobj thing)
        {
            if ((thing.Flags & MobjFlags.Corpse) == 0)
            {
                // Not a monster.
                return true;
            }

            if (thing.Tics != -1)
            {
                // Not lying still yet.
                return true;
            }

            if (thing.Info.Raisestate == MobjState.Null)
            {
                // Monster doesn't have a raise state.
                return true;
            }

            var maxDist = thing.Info.Radius + DoomInfo.MobjInfos[(int)MobjType.Vile].Radius;

            if (Fixed.Abs(thing.X - vileTryX) > maxDist ||
                Fixed.Abs(thing.Y - vileTryY) > maxDist)
            {
                // Not actually touching.
                return true;
            }

            vileTargetCorpse = thing;
            vileTargetCorpse.MomX = vileTargetCorpse.MomY = Fixed.Zero;
            vileTargetCorpse.Height <<= 2;

            var check = world.ThingMovement.CheckPosition(
                vileTargetCorpse,
                vileTargetCorpse.X,
                vileTargetCorpse.Y);

            vileTargetCorpse.Height >>= 2;

            if (!check)
            {
                // Doesn't fit here.
                return true;
            }

            // Got one, so stop checking.
            return false;
        }


        public void VileChase(Mobj actor)
        {
            if (actor.MoveDir != Direction.None)
            {
                // Check for corpses to raise.
                vileTryX = actor.X + actor.Info.Speed * xSpeed[(int)actor.MoveDir];
                vileTryY = actor.Y + actor.Info.Speed * ySpeed[(int)actor.MoveDir];

                var bm = world.Map.BlockMap;

                var maxRadius = GameConst.MaxThingRadius * 2;
                var blockX1 = bm.GetBlockX(vileTryX - maxRadius);
                var blockX2 = bm.GetBlockX(vileTryX + maxRadius);
                var blockY1 = bm.GetBlockY(vileTryY - maxRadius);
                var blockY2 = bm.GetBlockY(vileTryY + maxRadius);

                for (var bx = blockX1; bx <= blockX2; bx++)
                {
                    for (var by = blockY1; by <= blockY2; by++)
                    {
                        // Call VileCheck to check whether object is a corpse that canbe raised.
                        if (!bm.IterateThings(bx, by, vileCheckFunc))
                        {
                            // Got one!
                            var temp = actor.Target;
                            actor.Target = vileTargetCorpse;
                            FaceTarget(actor);
                            actor.Target = temp;
                            actor.SetState(MobjState.VileHeal1);

                            world.StartSound(vileTargetCorpse, Sfx.SLOP, SfxType.Misc);

                            var info = vileTargetCorpse.Info;
                            vileTargetCorpse.SetState(info.Raisestate);
                            vileTargetCorpse.Height <<= 2;
                            vileTargetCorpse.Flags = info.Flags;
                            vileTargetCorpse.Health = info.SpawnHealth;
                            vileTargetCorpse.Target = null;

                            return;
                        }
                    }
                }
            }

            // Return to normal attack.
            Chase(actor);
        }


        public void VileStart(Mobj actor)
        {
            world.StartSound(actor, Sfx.VILATK, SfxType.Weapon);
        }


        public void StartFire(Mobj actor)
        {
            world.StartSound(actor, Sfx.FLAMST, SfxType.Weapon);

            Fire(actor);
        }


        public void FireCrackle(Mobj actor)
        {
            world.StartSound(actor, Sfx.FLAME, SfxType.Weapon);

            Fire(actor);
        }


        public void Fire(Mobj actor)
        {
            var dest = actor.Tracer;

            if (dest == null)
            {
                return;
            }

            var target = world.SubstNullMobj(actor.Target);

            // Don't move it if the vile lost sight.
            if (!world.VisibilityCheck.CheckSight(target, dest))
            {
                return;
            }

            world.ThingMovement.UnsetThingPosition(actor);

            var angle = dest.Angle;
            actor.X = dest.X + Fixed.FromInt(24) * Trig.Cos(angle);
            actor.Y = dest.Y + Fixed.FromInt(24) * Trig.Sin(angle);
            actor.Z = dest.Z;

            world.ThingMovement.SetThingPosition(actor);
        }


        public void VileTarget(Mobj actor)
        {
            if (actor.Target == null)
            {
                return;
            }

            FaceTarget(actor);

            var fog = world.ThingAllocation.SpawnMobj(
                actor.Target.X,
                actor.Target.X,
                actor.Target.Z,
                MobjType.Fire);

            actor.Tracer = fog;
            fog.Target = actor;
            fog.Tracer = actor.Target;
            Fire(fog);
        }


        public void VileAttack(Mobj actor)
        {
            if (actor.Target == null)
            {
                return;
            }

            FaceTarget(actor);

            if (!world.VisibilityCheck.CheckSight(actor, actor.Target))
            {
                return;
            }

            world.StartSound(actor, Sfx.BAREXP, SfxType.Weapon);
            world.ThingInteraction.DamageMobj(actor.Target, actor, actor, 20);
            actor.Target.MomZ = Fixed.FromInt(1000) / actor.Target.Info.Mass;

            var fire = actor.Tracer;
            if (fire == null)
            {
                return;
            }

            var angle = actor.Angle;

            // Move the fire between the vile and the player.
            fire.X = actor.Target.X - Fixed.FromInt(24) * Trig.Cos(angle);
            fire.Y = actor.Target.Y - Fixed.FromInt(24) * Trig.Sin(angle);
            world.ThingInteraction.RadiusAttack(fire, actor, 70);
        }



        ////////////////////////////////////////////////////////////
        // Revenant
        ////////////////////////////////////////////////////////////

        public void SkelMissile(Mobj actor)
        {
            if (actor.Target == null)
            {
                return;
            }

            FaceTarget(actor);

            // Missile spawns higher.
            actor.Z += Fixed.FromInt(16);

            var missile = world.ThingAllocation.SpawnMissile(actor, actor.Target, MobjType.Tracer);

            // Back to normal.
            actor.Z -= Fixed.FromInt(16);

            missile.X += missile.MomX;
            missile.Y += missile.MomY;
            missile.Tracer = actor.Target;
        }


        private static Angle traceAngle = new Angle(0xc000000);

        public void Tracer(Mobj actor)
        {
            if ((world.GameTic & 3) != 0)
            {
                return;
            }

            // Spawn a puff of smoke behind the rocket.
            world.Hitscan.SpawnPuff(actor.X, actor.Y, actor.Z);

            var smoke = world.ThingAllocation.SpawnMobj(
                actor.X - actor.MomX,
                actor.Y - actor.MomY,
                actor.Z,
                MobjType.Smoke);

            smoke.MomZ = Fixed.One;
            smoke.Tics -= world.Random.Next() & 3;
            if (smoke.Tics < 1)
            {
                smoke.Tics = 1;
            }

            // Adjust direction.
            var dest = actor.Tracer;

            if (dest == null || dest.Health <= 0)
            {
                return;
            }

            // Change angle.
            var exact = Geometry.PointToAngle(
                actor.X, actor.Y,
                dest.X, dest.Y);

            if (exact != actor.Angle)
            {
                if (exact - actor.Angle > Angle.Ang180)
                {
                    actor.Angle -= traceAngle;
                    if (exact - actor.Angle < Angle.Ang180)
                    {
                        actor.Angle = exact;
                    }
                }
                else
                {
                    actor.Angle += traceAngle;
                    if (exact - actor.Angle > Angle.Ang180)
                    {
                        actor.Angle = exact;
                    }
                }
            }

            exact = actor.Angle;
            actor.MomX = new Fixed(actor.Info.Speed) * Trig.Cos(exact);
            actor.MomY = new Fixed(actor.Info.Speed) * Trig.Sin(exact);

            // Change slope.
            var dist = Geometry.AproxDistance(
                dest.X - actor.X,
                dest.Y - actor.Y);

            var num = (dest.Z + Fixed.FromInt(40) - actor.Z).Data;
            var den = dist.Data / actor.Info.Speed;
            if (den < 1)
            {
                den = 1;
            }

            var slope = new Fixed(num / den);

            if (slope < actor.MomZ)
            {
                actor.MomZ -= Fixed.One / 8;
            }
            else
            {
                actor.MomZ += Fixed.One / 8;
            }
        }


        public void SkelWhoosh(Mobj actor)
        {
            if (actor.Target == null)
            {
                return;
            }

            FaceTarget(actor);

            world.StartSound(actor, Sfx.SKESWG, SfxType.Weapon);
        }


        public void SkelFist(Mobj actor)
        {
            if (actor.Target == null)
            {
                return;
            }

            FaceTarget(actor);

            if (CheckMeleeRange(actor))
            {
                var damage = ((world.Random.Next() % 10) + 1) * 6;
                world.StartSound(actor, Sfx.SKEPCH, SfxType.Weapon);
                world.ThingInteraction.DamageMobj(actor.Target, actor, actor, damage);
            }
        }



        ////////////////////////////////////////////////////////////
        // Pain elemental
        ////////////////////////////////////////////////////////////

        private void PainShootSkull(Mobj actor, Angle angle)
        {
            // Count total number of skull currently on the level.
            var count = 0;

            foreach (var thinker in world.Thinkers)
            {
                var mobj = thinker as Mobj;
                if (mobj != null && mobj.Type == MobjType.Skull)
                {
                    count++;
                }
            }

            // If there are allready 20 skulls on the level,
            // don't spit another one.
            if (count > 20)
            {
                return;
            }

            // Okay, there's playe for another one.

            var preStep = Fixed.FromInt(4) +
                3 * (actor.Info.Radius + DoomInfo.MobjInfos[(int)MobjType.Skull].Radius) / 2;

            var x = actor.X + preStep * Trig.Cos(angle);
            var y = actor.Y + preStep * Trig.Sin(angle);
            var z = actor.Z + Fixed.FromInt(8);

            var skull = world.ThingAllocation.SpawnMobj(x, y, z, MobjType.Skull);

            // Check for movements.
            if (!world.ThingMovement.TryMove(skull, skull.X, skull.Y))
            {
                // Kill it immediately.
                world.ThingInteraction.DamageMobj(skull, actor, actor, 10000);
                return;
            }

            skull.Target = actor.Target;

            SkullAttack(skull);
        }


        public void PainAttack(Mobj actor)
        {
            if (actor.Target == null)
            {
                return;
            }

            FaceTarget(actor);

            PainShootSkull(actor, actor.Angle);
        }


        public void PainDie(Mobj actor)
        {
            Fall(actor);

            PainShootSkull(actor, actor.Angle + Angle.Ang90);
            PainShootSkull(actor, actor.Angle + Angle.Ang180);
            PainShootSkull(actor, actor.Angle + Angle.Ang270);
        }



        ////////////////////////////////////////////////////////////
        // Boss death
        ////////////////////////////////////////////////////////////

        private LineDef junk;

        private void InitBossDeath()
        {
            var v = new Vertex(Fixed.Zero, Fixed.Zero);
            junk = new LineDef(v, v, 0, 0, 0, null, null);
        }


        public void BossDeath(Mobj actor)
        {
            var options = world.Options;
            if (options.GameMode == GameMode.Commercial)
            {
                if (options.Map != 7)
                {
                    return;
                }

                if ((actor.Type != MobjType.Fatso) && (actor.Type != MobjType.Baby))
                {
                    return;
                }
            }
            else
            {
                switch (options.Episode)
                {
                    case 1:
                        if (options.Map != 8)
                        {
                            return;
                        }

                        if (actor.Type != MobjType.Bruiser)
                        {
                            return;
                        }

                        break;

                    case 2:
                        if (options.Map != 8)
                        {
                            return;
                        }

                        if (actor.Type != MobjType.Cyborg)
                        {
                            return;
                        }

                        break;

                    case 3:
                        if (options.Map != 8)
                        {
                            return;
                        }

                        if (actor.Type != MobjType.Spider)
                        {
                            return;
                        }

                        break;

                    case 4:
                        switch (options.Map)
                        {
                            case 6:
                                if (actor.Type != MobjType.Cyborg)
                                {
                                    return;
                                }

                                break;

                            case 8:
                                if (actor.Type != MobjType.Spider)
                                {
                                    return;
                                }

                                break;

                            default:
                                return;
                        }
                        break;

                    default:
                        if (options.Map != 8)
                        {
                            return;
                        }

                        break;
                }
            }

            // Make sure there is a player alive for victory.
            var players = world.Options.Players;
            int i;
            for (i = 0; i < Player.MaxPlayerCount; i++)
            {
                if (players[i].InGame && players[i].Health > 0)
                {
                    break;
                }
            }

            if (i == Player.MaxPlayerCount)
            {
                // No one left alive, so do not end game.
                return;
            }

            // Scan the remaining thinkers to see if all bosses are dead.
            foreach (var thinker in world.Thinkers)
            {
                var mo2 = thinker as Mobj;
                if (mo2 == null)
                {
                    continue;
                }

                if (mo2 != actor && mo2.Type == actor.Type && mo2.Health > 0)
                {
                    // Other boss not dead.
                    return;
                }
            }

            // Victory!
            if (options.GameMode == GameMode.Commercial)
            {
                if (options.Map == 7)
                {
                    if (actor.Type == MobjType.Fatso)
                    {
                        junk.Tag = 666;
                        world.SectorAction.DoFloor(junk, FloorMoveType.LowerFloorToLowest);
                        return;
                    }

                    if (actor.Type == MobjType.Baby)
                    {
                        junk.Tag = 667;
                        world.SectorAction.DoFloor(junk, FloorMoveType.RaiseToTexture);
                        return;
                    }
                }
            }
            else
            {
                switch (options.Episode)
                {
                    case 1:
                        junk.Tag = 666;
                        world.SectorAction.DoFloor(junk, FloorMoveType.LowerFloorToLowest);
                        return;

                    case 4:
                        switch (options.Map)
                        {
                            case 6:
                                junk.Tag = 666;
                                world.SectorAction.DoDoor(junk, VerticalDoorType.BlazeOpen);
                                return;

                            case 8:
                                junk.Tag = 666;
                                world.SectorAction.DoFloor(junk, FloorMoveType.LowerFloorToLowest);
                                return;
                        }
                        break;
                }
            }

            world.ExitLevel();
        }


        public void KeenDie(Mobj actor)
        {
            Fall(actor);

            // scan the remaining thinkers
            // to see if all Keens are dead
            foreach (var thinker in world.Thinkers)
            {
                var mo2 = thinker as Mobj;
                if (mo2 == null)
                {
                    continue;
                }

                if (mo2 != actor && mo2.Type == actor.Type && mo2.Health > 0)
                {
                    // other Keen not dead
                    return;
                }
            }

            junk.Tag = 666;
            world.SectorAction.DoDoor(junk, VerticalDoorType.Open);
        }



        ////////////////////////////////////////////////////////////
        // Icon of sin
        ////////////////////////////////////////////////////////////

        private Mobj[] brainTargets;
        private int brainTargetCount;
        private int currentBrainTarget;
        private bool easy;

        private void InitBrain()
        {
            brainTargets = new Mobj[32];
            brainTargetCount = 0;
            currentBrainTarget = 0;
            easy = false;
        }


        public void BrainAwake(Mobj actor)
        {
            // Find all the target spots.
            brainTargetCount = 0;
            currentBrainTarget = 0;

            foreach (var thinker in world.Thinkers)
            {
                var mobj = thinker as Mobj;
                if (mobj == null)
                {
                    // Not a mobj.
                    continue;
                }

                if (mobj.Type == MobjType.Bosstarget)
                {
                    brainTargets[brainTargetCount] = mobj;
                    brainTargetCount++;
                }
            }

            world.StartSound(actor, Sfx.BOSSIT, SfxType.Diffuse);
        }


        public void BrainPain(Mobj actor)
        {
            world.StartSound(actor, Sfx.BOSPN, SfxType.Diffuse);
        }


        public void BrainScream(Mobj actor)
        {
            var random = world.Random;

            for (var x = actor.X - Fixed.FromInt(196); x < actor.X + Fixed.FromInt(320); x += Fixed.FromInt(8))
            {
                var y = actor.Y - Fixed.FromInt(320);
                var z = new Fixed(128) + random.Next() * Fixed.FromInt(2);

                var explosion = world.ThingAllocation.SpawnMobj(x, y, z, MobjType.Rocket);
                explosion.MomZ = new Fixed(random.Next() * 512);
                explosion.SetState(MobjState.Brainexplode1);
                explosion.Tics -= random.Next() & 7;
                if (explosion.Tics < 1)
                {
                    explosion.Tics = 1;
                }
            }

            world.StartSound(actor, Sfx.BOSDTH, SfxType.Diffuse);
        }


        public void BrainExplode(Mobj actor)
        {
            var random = world.Random;

            var x = actor.X + new Fixed((random.Next() - random.Next()) * 2048);
            var y = actor.Y;
            var z = new Fixed(128) + random.Next() * Fixed.FromInt(2);

            var explosion = world.ThingAllocation.SpawnMobj(x, y, z, MobjType.Rocket);
            explosion.MomZ = new Fixed(random.Next() * 512);
            explosion.SetState(MobjState.Brainexplode1);
            explosion.Tics -= random.Next() & 7;
            if (explosion.Tics < 1)
            {
                explosion.Tics = 1;
            }
        }


        public void BrainDie(Mobj actor)
        {
            world.ExitLevel();
        }


        public void BrainSpit(Mobj actor)
        {
            easy = !easy;
            if (world.Options.Skill <= GameSkill.Easy && (!easy))
            {
                return;
            }

            // If the game is reconstructed from a savedata, brain targets might be cleared.
            // If so, re-initialize them to avoid crash.
            if (brainTargetCount == 0)
            {
                BrainAwake(actor);
            }

            // Shoot a cube at current target.
            var target = brainTargets[currentBrainTarget];
            currentBrainTarget = (currentBrainTarget + 1) % brainTargetCount;

            // Spawn brain missile.
            var missile = world.ThingAllocation.SpawnMissile(actor, target, MobjType.Spawnshot);
            missile.Target = target;
            missile.ReactionTime = ((target.Y - actor.Y).Data / missile.MomY.Data) / missile.State.Tics;

            world.StartSound(actor, Sfx.BOSPIT, SfxType.Diffuse);
        }


        public void SpawnSound(Mobj actor)
        {
            world.StartSound(actor, Sfx.BOSCUB, SfxType.Misc);
            SpawnFly(actor);
        }


        public void SpawnFly(Mobj actor)
        {
            if (--actor.ReactionTime > 0)
            {
                // Still flying.
                return;
            }

            var target = actor.Target;

            // If the game is reconstructed from a savedata, the target might be null.
            // If so, use own position to spawn the monster.
            if (target == null)
            {
                target = actor;
                actor.Z = actor.Subsector.Sector.FloorHeight;
            }

            var ta = world.ThingAllocation;

            // First spawn teleport fog.
            var fog = ta.SpawnMobj(target.X, target.Y, target.Z, MobjType.Spawnfire);
            world.StartSound(fog, Sfx.TELEPT, SfxType.Misc);

            // Randomly select monster to spawn.
            var r = world.Random.Next();

            // Probability distribution (kind of :), decreasing likelihood.
            MobjType type;
            if (r < 50)
            {
                type = MobjType.Troop;
            }
            else if (r < 90)
            {
                type = MobjType.Sergeant;
            }
            else if (r < 120)
            {
                type = MobjType.Shadows;
            }
            else if (r < 130)
            {
                type = MobjType.Pain;
            }
            else if (r < 160)
            {
                type = MobjType.Head;
            }
            else if (r < 162)
            {
                type = MobjType.Vile;
            }
            else if (r < 172)
            {
                type = MobjType.Undead;
            }
            else if (r < 192)
            {
                type = MobjType.Baby;
            }
            else if (r < 222)
            {
                type = MobjType.Fatso;
            }
            else if (r < 246)
            {
                type = MobjType.Knight;
            }
            else
            {
                type = MobjType.Bruiser;
            }

            var monster = ta.SpawnMobj(target.X, target.Y, target.Z, type);
            if (LookForPlayers(monster, true))
            {
                monster.SetState(monster.Info.SeeState);
            }

            // Telefrag anything in this spot.
            world.ThingMovement.TeleportMove(monster, monster.X, monster.Y);

            // Remove self (i.e., cube).
            world.ThingAllocation.RemoveMobj(actor);
        }
    }
}
