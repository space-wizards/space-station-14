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
    public sealed class Hitscan
    {
        private World world;

        public Hitscan(World world)
        {
            this.world = world;

            aimTraverseFunc = AimTraverse;
            shootTraverseFunc = ShootTraverse;
        }

        private Func<Intercept, bool> aimTraverseFunc;
        private Func<Intercept, bool> shootTraverseFunc;

        // Who got hit (or null).
        private Mobj lineTarget;

        private Mobj currentShooter;
        private Fixed currentShooterZ;

        private Fixed currentRange;
        private Fixed currentAimSlope;
        private int currentDamage;

        // Slopes to top and bottom of target.
        private Fixed topSlope;
        private Fixed bottomSlope;

        /// <summary>
        /// Find a thing or wall which is on the aiming line.
        /// Sets lineTaget and aimSlope when a target is aimed at.
        /// </summary>
        private bool AimTraverse(Intercept intercept)
        {
            if (intercept.Line != null)
            {
                var line = intercept.Line;

                if ((line.Flags & LineFlags.TwoSided) == 0)
                {
                    // Stop.
                    return false;
                }

                var mc = world.MapCollision;

                // Crosses a two sided line.
                // A two sided line will restrict the possible target ranges.
                mc.LineOpening(line);

                if (mc.OpenBottom >= mc.OpenTop)
                {
                    // Stop.
                    return false;
                }

                var dist = currentRange * intercept.Frac;

                // The null check of the backsector below is necessary to avoid crash
                // in certain PWADs, which contain two-sided lines with no backsector.
                // These are imported from Chocolate Doom.

                if (line.BackSector == null ||
                    line.FrontSector.FloorHeight != line.BackSector.FloorHeight)
                {
                    var slope = (mc.OpenBottom - currentShooterZ) / dist;
                    if (slope > bottomSlope)
                    {
                        bottomSlope = slope;
                    }
                }

                if (line.BackSector == null ||
                    line.FrontSector.CeilingHeight != line.BackSector.CeilingHeight)
                {
                    var slope = (mc.OpenTop - currentShooterZ) / dist;
                    if (slope < topSlope)
                    {
                        topSlope = slope;
                    }
                }

                if (topSlope <= bottomSlope)
                {
                    // Stop.
                    return false;
                }

                // Shot continues.
                return true;
            }

            // Shoot a thing.
            var thing = intercept.Thing;
            if (thing == currentShooter)
            {
                // Can't shoot self.
                return true;
            }

            {
                if ((thing.Flags & MobjFlags.Shootable) == 0)
                {
                    // Corpse or something.
                    return true;
                }

                // Check angles to see if the thing can be aimed at.
                var dist = currentRange * intercept.Frac;
                var thingTopSlope = (thing.Z + thing.Height - currentShooterZ) / dist;

                if (thingTopSlope < bottomSlope)
                {
                    // Shot over the thing.
                    return true;
                }

                var thingBottomSlope = (thing.Z - currentShooterZ) / dist;

                if (thingBottomSlope > topSlope)
                {
                    // Shot under the thing.
                    return true;
                }

                // This thing can be hit!
                if (thingTopSlope > topSlope)
                {
                    thingTopSlope = topSlope;
                }

                if (thingBottomSlope < bottomSlope)
                {
                    thingBottomSlope = bottomSlope;
                }

                currentAimSlope = (thingTopSlope + thingBottomSlope) / 2;
                lineTarget = thing;

                // Don't go any farther.
                return false;
            }
        }

        /// <summary>
        /// Fire a hitscan bullet along the aiming line.
        /// </summary>
        private bool ShootTraverse(Intercept intercept)
        {
            var mi = world.MapInteraction;
            var pt = world.PathTraversal;

            if (intercept.Line != null)
            {
                var line = intercept.Line;

                if (line.Special != 0)
                {
                    mi.ShootSpecialLine(currentShooter, line);
                }

                if ((line.Flags & LineFlags.TwoSided) == 0)
                {
                    goto hitLine;
                }

                var mc = world.MapCollision;

                // Crosses a two sided line.
                mc.LineOpening(line);

                var dist = currentRange * intercept.Frac;

                // Similar to AimTraverse, the code below is imported from Chocolate Doom.
                if (line.BackSector == null)
                {
                    {
                        var slope = (mc.OpenBottom - currentShooterZ) / dist;
                        if (slope > currentAimSlope)
                        {
                            goto hitLine;
                        }
                    }

                    {
                        var slope = (mc.OpenTop - currentShooterZ) / dist;
                        if (slope < currentAimSlope)
                        {
                            goto hitLine;
                        }
                    }
                }
                else
                {
                    if (line.FrontSector.FloorHeight != line.BackSector.FloorHeight)
                    {
                        var slope = (mc.OpenBottom - currentShooterZ) / dist;
                        if (slope > currentAimSlope)
                        {
                            goto hitLine;
                        }
                    }

                    if (line.FrontSector.CeilingHeight != line.BackSector.CeilingHeight)
                    {
                        var slope = (mc.OpenTop - currentShooterZ) / dist;
                        if (slope < currentAimSlope)
                        {
                            goto hitLine;
                        }
                    }
                }

                // Shot continues.
                return true;

                // Hit line.
                hitLine:

                // Position a bit closer.
                var frac = intercept.Frac - Fixed.FromInt(4) / currentRange;
                var x = pt.Trace.X + pt.Trace.Dx * frac;
                var y = pt.Trace.Y + pt.Trace.Dy * frac;
                var z = currentShooterZ + currentAimSlope * (frac * currentRange);

                if (line.FrontSector.CeilingFlat == world.Map.SkyFlatNumber)
                {
                    // Don't shoot the sky!
                    if (z > line.FrontSector.CeilingHeight)
                    {
                        return false;
                    }

                    // It's a sky hack wall.
                    if (line.BackSector != null && line.BackSector.CeilingFlat == world.Map.SkyFlatNumber)
                    {
                        return false;
                    }
                }

                // Spawn bullet puffs.
                SpawnPuff(x, y, z);

                // Don't go any farther.
                return false;
            }

            {
                // Shoot a thing.
                var thing = intercept.Thing;
                if (thing == currentShooter)
                {
                    // Can't shoot self.
                    return true;
                }

                if ((thing.Flags & MobjFlags.Shootable) == 0)
                {
                    // Corpse or something.
                    return true;
                }

                // Check angles to see if the thing can be aimed at.
                var dist = currentRange * intercept.Frac;
                var thingTopSlope = (thing.Z + thing.Height - currentShooterZ) / dist;

                if (thingTopSlope < currentAimSlope)
                {
                    // Shot over the thing.
                    return true;
                }

                var thingBottomSlope = (thing.Z - currentShooterZ) / dist;

                if (thingBottomSlope > currentAimSlope)
                {
                    // Shot under the thing.
                    return true;
                }

                // Hit thing.
                // Position a bit closer.
                var frac = intercept.Frac - Fixed.FromInt(10) / currentRange;

                var x = pt.Trace.X + pt.Trace.Dx * frac;
                var y = pt.Trace.Y + pt.Trace.Dy * frac;
                var z = currentShooterZ + currentAimSlope * (frac * currentRange);

                // Spawn bullet puffs or blod spots, depending on target type.
                if ((intercept.Thing.Flags & MobjFlags.NoBlood) != 0)
                {
                    SpawnPuff(x, y, z);
                }
                else
                {
                    SpawnBlood(x, y, z, currentDamage);
                }

                if (currentDamage != 0)
                {
                    world.ThingInteraction.DamageMobj(thing, currentShooter, currentShooter, currentDamage);
                }

                // Don't go any farther.
                return false;
            }
        }

        /// <summary>
        /// Find a target on the aiming line.
        /// Sets LineTaget when a target is aimed at.
        /// </summary>
        public Fixed AimLineAttack(Mobj shooter, Angle angle, Fixed range)
        {
            shooter = world.SubstNullMobj(shooter);

            currentShooter = shooter;
            currentShooterZ = shooter.Z + (shooter.Height >> 1) + Fixed.FromInt(8);
            currentRange = range;

            var targetX = shooter.X + range.ToIntFloor() * Trig.Cos(angle);
            var targetY = shooter.Y + range.ToIntFloor() * Trig.Sin(angle);

            // Can't shoot outside view angles.
            topSlope = Fixed.FromInt(100) / 160;
            bottomSlope = Fixed.FromInt(-100) / 160;

            lineTarget = null;

            world.PathTraversal.PathTraverse(
                shooter.X, shooter.Y,
                targetX, targetY,
                PathTraverseFlags.AddLines | PathTraverseFlags.AddThings,
                aimTraverseFunc);

            if (lineTarget != null)
            {
                return currentAimSlope;
            }

            return Fixed.Zero;
        }

        /// <summary>
        /// Fire a hitscan bullet.
        /// If damage == 0, it is just a test trace that will leave linetarget set.
        /// </summary>
        public void LineAttack(Mobj shooter, Angle angle, Fixed range, Fixed slope, int damage)
        {
            currentShooter = shooter;
            currentShooterZ = shooter.Z + (shooter.Height >> 1) + Fixed.FromInt(8);
            currentRange = range;
            currentAimSlope = slope;
            currentDamage = damage;

            var targetX = shooter.X + range.ToIntFloor() * Trig.Cos(angle);
            var targetY = shooter.Y + range.ToIntFloor() * Trig.Sin(angle);

            world.PathTraversal.PathTraverse(
                shooter.X, shooter.Y,
                targetX, targetY,
                PathTraverseFlags.AddLines | PathTraverseFlags.AddThings,
                shootTraverseFunc);
        }

        /// <summary>
        /// Spawn a bullet puff.
        /// </summary>
        public void SpawnPuff(Fixed x, Fixed y, Fixed z)
        {
            var random = world.Random;

            z += new Fixed((random.Next() - random.Next()) << 10);

            var thing = world.ThingAllocation.SpawnMobj(x, y, z, MobjType.Puff);
            thing.MomZ = Fixed.One;
            thing.Tics -= random.Next() & 3;

            if (thing.Tics < 1)
            {
                thing.Tics = 1;
            }

            // Don't make punches spark on the wall.
            if (currentRange == WeaponBehavior.MeleeRange)
            {
                thing.SetState(MobjState.Puff3);
            }
        }

        /// <summary>
        /// Spawn blood.
        /// </summary>
        public void SpawnBlood(Fixed x, Fixed y, Fixed z, int damage)
        {
            var random = world.Random;

            z += new Fixed((random.Next() - random.Next()) << 10);

            var thing = world.ThingAllocation.SpawnMobj(x, y, z, MobjType.Blood);
            thing.MomZ = Fixed.FromInt(2);
            thing.Tics -= random.Next() & 3;

            if (thing.Tics < 1)
            {
                thing.Tics = 1;
            }

            if (damage <= 12 && damage >= 9)
            {
                thing.SetState(MobjState.Blood2);
            }
            else if (damage < 9)
            {
                thing.SetState(MobjState.Blood3);
            }
        }

        public Mobj LineTarget => lineTarget;
        public Fixed BottomSlope => bottomSlope;
        public Fixed TopSlope => topSlope;
    }
}
