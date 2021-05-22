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
	public sealed class ThingInteraction
	{
		private World world;

		public ThingInteraction(World world)
		{
			this.world = world;

			InitRadiusAttack();
		}


		/// <summary>
		/// Called when the target is killed.
		/// </summary>
		public void KillMobj(Mobj source, Mobj target)
		{
			target.Flags &= ~(MobjFlags.Shootable | MobjFlags.Float | MobjFlags.SkullFly);

			if (target.Type != MobjType.Skull)
			{
				target.Flags &= ~MobjFlags.NoGravity;
			}

			target.Flags |= MobjFlags.Corpse | MobjFlags.DropOff;
			target.Height = new Fixed(target.Height.Data >> 2);

			if (source != null && source.Player != null)
			{
				// Count for intermission.
				if ((target.Flags & MobjFlags.CountKill) != 0)
				{
					source.Player.KillCount++;
				}

				if (target.Player != null)
				{
					source.Player.Frags[target.Player.Number]++;
				}
			}
			else if (!world.Options.NetGame && (target.Flags & MobjFlags.CountKill) != 0)
			{
				// Count all monster deaths, even those caused by other monsters.
				world.Options.Players[0].KillCount++;
			}

			if (target.Player != null)
			{
				// Count environment kills against you.
				if (source == null)
				{
					target.Player.Frags[target.Player.Number]++;
				}

				target.Flags &= ~MobjFlags.Solid;
				target.Player.PlayerState = PlayerState.Dead;
				world.PlayerBehavior.DropWeapon(target.Player);

				var am = world.AutoMap;

				if (target.Player.Number == world.Options.ConsolePlayer && am.Visible)
				{
					// Don't die in auto map, switch view prior to dying.
					am.Close();
				}
			}

			if (target.Health < -target.Info.SpawnHealth && target.Info.XdeathState != 0)
			{
				target.SetState(target.Info.XdeathState);
			}
			else
			{
				target.SetState(target.Info.DeathState);
			}

			target.Tics -= world.Random.Next() & 3;
			if (target.Tics < 1)
			{
				target.Tics = 1;
			}

			// Drop stuff.
			// This determines the kind of object spawned during the death frame of a thing.
			MobjType item;
			switch (target.Type)
			{
				case MobjType.Wolfss:
				case MobjType.Possessed:
					item = MobjType.Clip;
					break;

				case MobjType.Shotguy:
					item = MobjType.Shotgun;
					break;

				case MobjType.Chainguy:
					item = MobjType.Chaingun;
					break;

				default:
					return;
			}

			var mo = world.ThingAllocation.SpawnMobj(target.X, target.Y, Mobj.OnFloorZ, item);

			// Special versions of items.
			mo.Flags |= MobjFlags.Dropped;
		}


		private static readonly int baseThreshold = 100;

		/// <summary>
		/// Damages both enemies and players.
		/// "inflictor" is the thing that caused the damage creature
		/// or missile, can be null (slime, etc).
		/// "source" is the thing to target after taking damage creature
		/// or null.
		/// Source and inflictor are the same for melee attacks.
		/// Source can be null for slime, barrel explosions and other
		/// environmental stuff.
		/// </summary>
		public void DamageMobj(Mobj target, Mobj inflictor, Mobj source, int damage)
		{
			if ((target.Flags & MobjFlags.Shootable) == 0)
			{
				// Shouldn't happen...
				return;
			}

			if (target.Health <= 0)
			{
				return;
			}

			if ((target.Flags & MobjFlags.SkullFly) != 0)
			{
				target.MomX = target.MomY = target.MomZ = Fixed.Zero;
			}

			var player = target.Player;
			if (player != null && world.Options.Skill == GameSkill.Baby)
			{
				// Take half damage in trainer mode.
				damage >>= 1;
			}

			// Some close combat weapons should not inflict thrust and
			// push the victim out of reach, thus kick away unless using the chainsaw.
			var notChainsawAttack =
				source == null ||
				source.Player == null ||
				source.Player.ReadyWeapon != WeaponType.Chainsaw;

			if (inflictor != null && (target.Flags & MobjFlags.NoClip) == 0 && notChainsawAttack)
			{
				var ang = Geometry.PointToAngle(
					inflictor.X,
					inflictor.Y,
					target.X,
					target.Y);

				var thrust = new Fixed(damage * (Fixed.FracUnit >> 3) * 100 / target.Info.Mass);

				// Make fall forwards sometimes.
				if (damage < 40 &&
					damage > target.Health &&
					target.Z - inflictor.Z > Fixed.FromInt(64) &&
					(world.Random.Next() & 1) != 0)
				{
					ang += Angle.Ang180;
					thrust *= 4;
				}

				target.MomX += thrust * Trig.Cos(ang);
				target.MomY += thrust * Trig.Sin(ang);
			}

			// Player specific.
			if (player != null)
			{
				// End of game hell hack.
				if (target.Subsector.Sector.Special == (SectorSpecial)11 && damage >= target.Health)
				{
					damage = target.Health - 1;
				}

				// Below certain threshold, ignore damage in GOD mode, or with INVUL power.
				if (damage < 1000 && ((player.Cheats & CheatFlags.GodMode) != 0 ||
					player.Powers[(int)PowerType.Invulnerability] > 0))
				{
					return;
				}

				int saved;

				if (player.ArmorType != 0)
				{
					if (player.ArmorType == 1)
					{
						saved = damage / 3;
					}
					else
					{
						saved = damage / 2;
					}

					if (player.ArmorPoints <= saved)
					{
						// Armor is used up.
						saved = player.ArmorPoints;
						player.ArmorType = 0;
					}

					player.ArmorPoints -= saved;
					damage -= saved;
				}

				// Mirror mobj health here for Dave.
				player.Health -= damage;
				if (player.Health < 0)
				{
					player.Health = 0;
				}

				player.Attacker = source;

				// Add damage after armor / invuln.
				player.DamageCount += damage;

				if (player.DamageCount > 100)
				{
					// Teleport stomp does 10k points...
					player.DamageCount = 100;
				}
			}

			// Do the damage.
			target.Health -= damage;
			if (target.Health <= 0)
			{
				KillMobj(source, target);
				return;
			}

			if ((world.Random.Next() < target.Info.PainChance) &&
				(target.Flags & MobjFlags.SkullFly) == 0)
			{
				// Fight back!
				target.Flags |= MobjFlags.JustHit;

				target.SetState(target.Info.PainState);
			}

			// We're awake now...
			target.ReactionTime = 0;

			if ((target.Threshold == 0 || target.Type == MobjType.Vile) &&
				source != null &&
				source != target &&
				source.Type != MobjType.Vile)
			{
				// If not intent on another player, chase after this one.
				target.Target = source;
				target.Threshold = baseThreshold;
				if (target.State == DoomInfo.States[(int)target.Info.SpawnState] &&
					target.Info.SeeState != MobjState.Null)
				{
					target.SetState(target.Info.SeeState);
				}
			}
		}


		/// <summary>
		/// Called when the missile hits something (wall or thing).
		/// </summary>
		public void ExplodeMissile(Mobj thing)
		{
			thing.MomX = thing.MomY = thing.MomZ = Fixed.Zero;

			thing.SetState(DoomInfo.MobjInfos[(int)thing.Type].DeathState);

			thing.Tics -= world.Random.Next() & 3;

			if (thing.Tics < 1)
			{
				thing.Tics = 1;
			}

			thing.Flags &= ~MobjFlags.Missile;

			if (thing.Info.DeathSound != 0)
			{
				world.StartSound(thing, thing.Info.DeathSound, SfxType.Misc);
			}
		}


		private Mobj bombSource;
		private Mobj bombSpot;
		private int bombDamage;

		private Func<Mobj, bool> radiusAttackFunc;

		private void InitRadiusAttack()
		{
			radiusAttackFunc = DoRadiusAttack;
		}

		/// <summary>
		/// "bombSource" is the creature that caused the explosion at "bombSpot".
		/// </summary>
		private bool DoRadiusAttack(Mobj thing)
		{
			if ((thing.Flags & MobjFlags.Shootable) == 0)
			{
				return true;
			}

			// Boss spider and cyborg take no damage from concussion.
			if (thing.Type == MobjType.Cyborg || thing.Type == MobjType.Spider)
			{
				return true;
			}

			var dx = Fixed.Abs(thing.X - bombSpot.X);
			var dy = Fixed.Abs(thing.Y - bombSpot.Y);

			var dist = dx > dy ? dx : dy;
			dist = new Fixed((dist - thing.Radius).Data >> Fixed.FracBits);

			if (dist < Fixed.Zero)
			{
				dist = Fixed.Zero;
			}

			if (dist.Data >= bombDamage)
			{
				// Out of range.
				return true;
			}

			if (world.VisibilityCheck.CheckSight(thing, bombSpot))
			{
				// Must be in direct path.
				DamageMobj(thing, bombSpot, bombSource, bombDamage - dist.Data);
			}

			return true;
		}

		/// <summary>
		/// Source is the creature that caused the explosion at spot.
		/// </summary>
		public void RadiusAttack(Mobj spot, Mobj source, int damage)
		{
			var bm = world.Map.BlockMap;

			var dist = Fixed.FromInt(damage + GameConst.MaxThingRadius.Data);

			var blockY1 = bm.GetBlockY(spot.Y - dist);
			var blockY2 = bm.GetBlockY(spot.Y + dist);
			var blockX1 = bm.GetBlockX(spot.X - dist);
			var blockX2 = bm.GetBlockX(spot.X + dist);

			bombSpot = spot;
			bombSource = source;
			bombDamage = damage;

			for (var by = blockY1; by <= blockY2; by++)
			{
				for (var bx = blockX1; bx <= blockX2; bx++)
				{
					bm.IterateThings(bx, by, radiusAttackFunc);
				}
			}
		}
	}
}
