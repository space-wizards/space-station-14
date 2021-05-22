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
	public sealed class StatusBar
	{
		private World world;

		// Used for appopriately pained face.
		private int oldHealth;

		// Used for evil grin.
		private bool[] oldWeaponsOwned;

		// Count until face changes.
		private int faceCount;

		// Current face index.
		private int faceIndex;

		// A random number per tick.
		private int randomNumber;

		private int priority;

		private int lastAttackDown;
		private int lastPainOffset;

		private DoomRandom random;

		public StatusBar(World world)
		{
			this.world = world;

			oldHealth = -1;
			oldWeaponsOwned = new bool[DoomInfo.WeaponInfos.Length];
			Array.Copy(
				world.ConsolePlayer.WeaponOwned,
				oldWeaponsOwned,
				DoomInfo.WeaponInfos.Length);
			faceCount = 0;
			faceIndex = 0;
			randomNumber = 0;
			priority = 0;
			lastAttackDown = -1;
			lastPainOffset = 0;

			random = new DoomRandom();
		}

		public void Reset()
		{
			oldHealth = -1;
			Array.Copy(
				world.ConsolePlayer.WeaponOwned,
				oldWeaponsOwned,
				DoomInfo.WeaponInfos.Length);
			faceCount = 0;
			faceIndex = 0;
			randomNumber = 0;
			priority = 0;
			lastAttackDown = -1;
			lastPainOffset = 0;
		}

		public void Update()
		{
			randomNumber = random.Next();
			UpdateFace();
		}

		private void UpdateFace()
		{
			var player = world.ConsolePlayer;

			if (priority < 10)
			{
				// Dead.
				if (player.Health == 0)
				{
					priority = 9;
					faceIndex = Face.DeadIndex;
					faceCount = 1;
				}
			}

			if (priority < 9)
			{
				if (player.BonusCount != 0)
				{
					// Picking up bonus.
					var doEvilGrin = false;

					for (var i = 0; i < DoomInfo.WeaponInfos.Length; i++)
					{
						if (oldWeaponsOwned[i] != player.WeaponOwned[i])
						{
							doEvilGrin = true;
							oldWeaponsOwned[i] = player.WeaponOwned[i];
						}
					}

					if (doEvilGrin)
					{
						// Evil grin if just picked up weapon.
						priority = 8;
						faceCount = Face.EvilGrinDuration;
						faceIndex = CalcPainOffset() + Face.EvilGrinOffset;
					}
				}
			}

			if (priority < 8)
			{
				if (player.DamageCount != 0 &&
					player.Attacker != null &&
					player.Attacker != player.Mobj)
				{
					// Being attacked.
					priority = 7;

					if (player.Health - oldHealth > Face.MuchPain)
					{
						faceCount = Face.TurnDuration;
						faceIndex = CalcPainOffset() + Face.OuchOffset;
					}
					else
					{
						var attackerAngle = Geometry.PointToAngle(
							player.Mobj.X, player.Mobj.Y,
							player.Attacker.X, player.Attacker.Y);

						Angle diff;
						bool right;
						if (attackerAngle > player.Mobj.Angle)
						{
							// Whether right or left.
							diff = attackerAngle - player.Mobj.Angle;
							right = diff > Angle.Ang180;
						}
						else
						{
							// Whether left or right.
							diff = player.Mobj.Angle - attackerAngle;
							right = diff <= Angle.Ang180;
						}

						faceCount = Face.TurnDuration;
						faceIndex = CalcPainOffset();

						if (diff < Angle.Ang45)
						{
							// Head-on.
							faceIndex += Face.RampageOffset;
						}
						else if (right)
						{
							// Turn face right.
							faceIndex += Face.TurnOffset;
						}
						else
						{
							// Turn face left.
							faceIndex += Face.TurnOffset + 1;
						}
					}
				}
			}

			if (priority < 7)
			{
				// Getting hurt because of your own damn stupidity.
				if (player.DamageCount != 0)
				{
					if (player.Health - oldHealth > Face.MuchPain)
					{
						priority = 7;
						faceCount = Face.TurnDuration;
						faceIndex = CalcPainOffset() + Face.OuchOffset;
					}
					else
					{
						priority = 6;
						faceCount = Face.TurnDuration;
						faceIndex = CalcPainOffset() + Face.RampageOffset;
					}
				}
			}

			if (priority < 6)
			{
				// Rapid firing.
				if (player.AttackDown)
				{
					if (lastAttackDown == -1)
					{
						lastAttackDown = Face.RampageDelay;
					}
					else if (--lastAttackDown == 0)
					{
						priority = 5;
						faceIndex = CalcPainOffset() + Face.RampageOffset;
						faceCount = 1;
						lastAttackDown = 1;
					}
				}
				else
				{
					lastAttackDown = -1;
				}
			}

			if (priority < 5)
			{
				// Invulnerability.
				if ((player.Cheats & CheatFlags.GodMode) != 0 ||
					player.Powers[(int)PowerType.Invulnerability] != 0)
				{
					priority = 4;

					faceIndex = Face.GodIndex;
					faceCount = 1;
				}
			}

			// Look left or look right if the facecount has timed out.
			if (faceCount == 0)
			{
				faceIndex = CalcPainOffset() + (randomNumber % 3);
				faceCount = Face.StraightFaceDuration;
				priority = 0;
			}

			faceCount--;
		}

		private int CalcPainOffset()
		{
			var player = world.Options.Players[world.Options.ConsolePlayer];

			var health = player.Health > 100 ? 100 : player.Health;

			if (health != oldHealth)
			{
				lastPainOffset = Face.Stride *
					(((100 - health) * Face.PainFaceCount) / 101);
				oldHealth = health;
			}

			return lastPainOffset;
		}

		public int FaceIndex => faceIndex;



		public static class Face
		{
			public static readonly int PainFaceCount = 5;
			public static readonly int StraightFaceCount = 3;
			public static readonly int TurnFaceCount = 2;
			public static readonly int SpecialFaceCount = 3;

			public static readonly int Stride = StraightFaceCount + TurnFaceCount + SpecialFaceCount;
			public static readonly int ExtraFaceCount = 2;
			public static readonly int FaceCount = Stride * PainFaceCount + ExtraFaceCount;

			public static readonly int TurnOffset = StraightFaceCount;
			public static readonly int OuchOffset = TurnOffset + TurnFaceCount;
			public static readonly int EvilGrinOffset = OuchOffset + 1;
			public static readonly int RampageOffset = EvilGrinOffset + 1;
			public static readonly int GodIndex = PainFaceCount * Stride;
			public static readonly int DeadIndex = GodIndex + 1;

			public static readonly int EvilGrinDuration = (2 * GameConst.TicRate);
			public static readonly int StraightFaceDuration = (GameConst.TicRate / 2);
			public static readonly int TurnDuration = (1 * GameConst.TicRate);
			public static readonly int OuchDuration = (1 * GameConst.TicRate);
			public static readonly int RampageDelay = (2 * GameConst.TicRate);

			public static readonly int MuchPain = 20;
		}
	}
}
