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
	public sealed class MapInteraction
	{
		private static readonly Fixed useRange = Fixed.FromInt(64);

		private World world;

		public MapInteraction(World world)
		{
			this.world = world;

			InitUse();
		}



		////////////////////////////////////////////////////////////
		// Line use
		////////////////////////////////////////////////////////////

		private Mobj useThing;
		private Func<Intercept, bool> useTraverseFunc;

		private void InitUse()
		{
			useTraverseFunc = UseTraverse;
		}

		private bool UseTraverse(Intercept intercept)
		{
			var mc = world.MapCollision;

			if (intercept.Line.Special == 0)
			{
				mc.LineOpening(intercept.Line);
				if (mc.OpenRange <= Fixed.Zero)
				{
					world.StartSound(useThing, Sfx.NOWAY, SfxType.Voice);

					// Can't use through a wall.
					return false;
				}

				// Not a special line, but keep checking.
				return true;
			}

			var side = 0;
			if (Geometry.PointOnLineSide(useThing.X, useThing.Y, intercept.Line) == 1)
			{
				side = 1;
			}

			UseSpecialLine(useThing, intercept.Line, side);

			// Can't use for than one special line in a row.
			return false;
		}

		/// <summary>
		/// Looks for special lines in front of the player to activate.
		/// </summary>
		public void UseLines(Player player)
		{
			var pt = world.PathTraversal;

			useThing = player.Mobj;

			var angle = player.Mobj.Angle;

			var x1 = player.Mobj.X;
			var y1 = player.Mobj.Y;
			var x2 = x1 + useRange.ToIntFloor() * Trig.Cos(angle);
			var y2 = y1 + useRange.ToIntFloor() * Trig.Sin(angle);

			pt.PathTraverse(x1, y1, x2, y2, PathTraverseFlags.AddLines, useTraverseFunc);
		}

		/// <summary>
		/// Called when a thing uses a special line.
		/// Only the front sides of lines are usable.
		/// </summary>
		public bool UseSpecialLine(Mobj thing, LineDef line, int side)
		{
			var specials = world.Specials;
			var sa = world.SectorAction;

			// Err...
			// Use the back sides of VERY SPECIAL lines...
			if (side != 0)
			{
				switch ((int)line.Special)
				{
					case 124:
						// Sliding door open and close (unused).
						break;

					default:
						return false;
				}
			}

			// Switches that other things can activate.
			if (thing.Player == null)
			{
				// Never open secret doors.
				if ((line.Flags & LineFlags.Secret) != 0)
				{
					return false;
				}

				switch ((int)line.Special)
				{
					case 1:  // Manual door raise.
					case 32: // Manual blue.
					case 33: // Manual red.
					case 34: // Manual yellow.
						break;

					default:
						return false;
				}
			}

			// Do something.
			switch ((int)line.Special)
			{
				// MANUALS
				case 1:   // Vertical door.
				case 26:  // Blue door (locked).
				case 27:  // Yellow door (locked).
				case 28:  // Red door (locked).

				case 31:  // Manual door open.
				case 32:  // Blue locked door open.
				case 33:  // Red locked door open.
				case 34:  // Yellow locked door open.

				case 117: // Blazing door raise.
				case 118: // Blazing door open.
					sa.DoLocalDoor(line, thing);
					break;

				// SWITCHES
				case 7:
					// Build stairs.
					if (sa.BuildStairs(line, StairType.Build8))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 9:
					// Change donut.
					if (sa.DoDonut(line))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 11:
					// Exit level.
					specials.ChangeSwitchTexture(line, false);
					world.ExitLevel();
					break;

				case 14:
					// Raise floor 32 and change texture.
					if (sa.DoPlatform(line, PlatformType.RaiseAndChange, 32))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 15:
					// Raise floor 24 and change texture.
					if (sa.DoPlatform(line, PlatformType.RaiseAndChange, 24))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 18:
					// Raise floor to next highest floor.
					if (sa.DoFloor(line, FloorMoveType.RaiseFloorToNearest))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 20:
					// Raise platform next highest floor and change texture.
					if (sa.DoPlatform(line, PlatformType.RaiseToNearestAndChange, 0))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 21:
					// Platform down, wait, up and stay.
					if (sa.DoPlatform(line, PlatformType.DownWaitUpStay, 0))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 23:
					// Lower floor to Lowest.
					if (sa.DoFloor(line, FloorMoveType.LowerFloorToLowest))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 29:
					// Raise door.
					if (sa.DoDoor(line, VerticalDoorType.Normal))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 41:
					// Lower ceiling to floor.
					if (sa.DoCeiling(line, CeilingMoveType.LowerToFloor))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 71:
					// Turbo lower floor.
					if (sa.DoFloor(line, FloorMoveType.TurboLower))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 49:
					// Ceiling crush and raise.
					if (sa.DoCeiling(line, CeilingMoveType.CrushAndRaise))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 50:
					// Close door.
					if (sa.DoDoor(line, VerticalDoorType.Close))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 51:
					// Secret exit.
					specials.ChangeSwitchTexture(line, false);
					world.SecretExitLevel();
					break;

				case 55:
					// Raise floor crush.
					if (sa.DoFloor(line, FloorMoveType.RaiseFloorCrush))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 101:
					// Raise floor.
					if (sa.DoFloor(line, FloorMoveType.RaiseFloor))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 102:
					// Lower floor to surrounding floor height.
					if (sa.DoFloor(line, FloorMoveType.LowerFloor))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 103:
					// Open door.
					if (sa.DoDoor(line, VerticalDoorType.Open))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 111:
					// Blazing door raise (faster than turbo).
					if (sa.DoDoor(line, VerticalDoorType.BlazeRaise))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 112:
					// Blazing door open (faster than turbo).
					if (sa.DoDoor(line, VerticalDoorType.BlazeOpen))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 113:
					// Blazing door close (faster than turbo).
					if (sa.DoDoor(line, VerticalDoorType.BlazeClose))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 122:
					// Blazing platform down, wait, up and stay.
					if (sa.DoPlatform(line, PlatformType.BlazeDwus, 0))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 127:
					// Build stairs turbo 16.
					if (sa.BuildStairs(line, StairType.Turbo16))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 131:
					// Raise floor turbo.
					if (sa.DoFloor(line, FloorMoveType.RaiseFloorTurbo))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 133:
				// Blazing open door (blue).
				case 135:
				// Blazing open door (red).
				case 137:
					// Blazing open door (yellow).
					if (sa.DoLockedDoor(line, VerticalDoorType.BlazeOpen, thing))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				case 140:
					// Raise floor 512.
					if (sa.DoFloor(line, FloorMoveType.RaiseFloor512))
					{
						specials.ChangeSwitchTexture(line, false);
					}
					break;

				// BUTTONS
				case 42:
					// Close door.
					if (sa.DoDoor(line, VerticalDoorType.Close))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 43:
					// Lower ceiling to floor.
					if (sa.DoCeiling(line, CeilingMoveType.LowerToFloor))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 45:
					// lower floor to surrounding floor height.
					if (sa.DoFloor(line, FloorMoveType.LowerFloor))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 60:
					// Lower floor to Lowest.
					if (sa.DoFloor(line, FloorMoveType.LowerFloorToLowest))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 61:
					// Open door.
					if (sa.DoDoor(line, VerticalDoorType.Open))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 62:
					// Platform down, wait, up and stay.
					if (sa.DoPlatform(line, PlatformType.DownWaitUpStay, 1))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 63:
					// Raise door.
					if (sa.DoDoor(line, VerticalDoorType.Normal))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 64:
					// Raise floor to ceiling.
					if (sa.DoFloor(line, FloorMoveType.RaiseFloor))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 66:
					// Raise floor 24 and change texture.
					if (sa.DoPlatform(line, PlatformType.RaiseAndChange, 24))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 67:
					// Raise floor 32 and change texture.
					if (sa.DoPlatform(line, PlatformType.RaiseAndChange, 32))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 65:
					// Raise floor crush.
					if (sa.DoFloor(line, FloorMoveType.RaiseFloorCrush))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 68:
					// Raise platform to next highest floor and change texture.
					if (sa.DoPlatform(line, PlatformType.RaiseToNearestAndChange, 0))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 69:
					// Raise floor to next highest floor.
					if (sa.DoFloor(line, FloorMoveType.RaiseFloorToNearest))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 70:
					// Turbo lower floor.
					if (sa.DoFloor(line, FloorMoveType.TurboLower))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 114:
					// Blazing door raise (faster than turbo).
					if (sa.DoDoor(line, VerticalDoorType.BlazeRaise))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 115:
					// Blazing door open (faster than turbo).
					if (sa.DoDoor(line, VerticalDoorType.BlazeOpen))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 116:
					// Blazing door close (faster than turbo).
					if (sa.DoDoor(line, VerticalDoorType.BlazeClose))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 123:
					// Blazing platform down, wait, up and stay.
					if (sa.DoPlatform(line, PlatformType.BlazeDwus, 0))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 132:
					// Raise floor turbo.
					if (sa.DoFloor(line, FloorMoveType.RaiseFloorTurbo))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 99:
				// Blazing open door (blue).
				case 134:
				// Blazing open door (red).
				case 136:
					// Blazing open door (yellow).
					if (sa.DoLockedDoor(line, VerticalDoorType.BlazeOpen, thing))
					{
						specials.ChangeSwitchTexture(line, true);
					}
					break;

				case 138:
					// Light turn on.
					sa.LightTurnOn(line, 255);
					specials.ChangeSwitchTexture(line, true);
					break;

				case 139:
					// Light turn Off.
					sa.LightTurnOn(line, 35);
					specials.ChangeSwitchTexture(line, true);
					break;
			}

			return true;
		}



		////////////////////////////////////////////////////////////
		// Line crossing
		////////////////////////////////////////////////////////////

		/// <summary>
		/// Called every time a thing origin is about to cross a line
		/// with a non zero special.
		/// </summary>
		public void CrossSpecialLine(LineDef line, int side, Mobj thing)
		{
			//	Triggers that other things can activate.
			if (thing.Player == null)
			{
				// Things that should NOT trigger specials...
				switch (thing.Type)
				{
					case MobjType.Rocket:
					case MobjType.Plasma:
					case MobjType.Bfg:
					case MobjType.Troopshot:
					case MobjType.Headshot:
					case MobjType.Bruisershot:
						return;
					default:
						break;
				}

				var ok = false;
				switch ((int)line.Special)
				{
					case 39:  // TELEPORT TRIGGER
					case 97:  // TELEPORT RETRIGGER
					case 125: // TELEPORT MONSTERONLY TRIGGER
					case 126: // TELEPORT MONSTERONLY RETRIGGER
					case 4:   // RAISE DOOR
					case 10:  // PLAT DOWN-WAIT-UP-STAY TRIGGER
					case 88:  // PLAT DOWN-WAIT-UP-STAY RETRIGGER
						ok = true;
						break;
				}
				if (!ok)
				{
					return;
				}
			}

			var sa = world.SectorAction;

			// Note: could use some const's here.
			switch ((int)line.Special)
			{
				// TRIGGERS.
				// All from here to RETRIGGERS.
				case 2:
					// Open door.
					sa.DoDoor(line, VerticalDoorType.Open);
					line.Special = 0;
					break;

				case 3:
					// Close door.
					sa.DoDoor(line, VerticalDoorType.Close);
					line.Special = 0;
					break;

				case 4:
					// Raise door.
					sa.DoDoor(line, VerticalDoorType.Normal);
					line.Special = 0;
					break;

				case 5:
					// Raise floor.
					sa.DoFloor(line, FloorMoveType.RaiseFloor);
					line.Special = 0;
					break;

				case 6:
					// Fast ceiling crush and raise.
					sa.DoCeiling(line, CeilingMoveType.FastCrushAndRaise);
					line.Special = 0;
					break;

				case 8:
					// Build stairs.
					sa.BuildStairs(line, StairType.Build8);
					line.Special = 0;
					break;

				case 10:
					// Platform down, wait, up and stay.
					sa.DoPlatform(line, PlatformType.DownWaitUpStay, 0);
					line.Special = 0;
					break;

				case 12:
					// Light turn on - brightest near.
					sa.LightTurnOn(line, 0);
					line.Special = 0;
					break;

				case 13:
					// Light turn on 255.
					sa.LightTurnOn(line, 255);
					line.Special = 0;
					break;

				case 16:
					// Close door 30.
					sa.DoDoor(line, VerticalDoorType.Close30ThenOpen);
					line.Special = 0;
					break;

				case 17:
					// Start light strobing.
					sa.StartLightStrobing(line);
					line.Special = 0;
					break;

				case 19:
					// Lower floor.
					sa.DoFloor(line, FloorMoveType.LowerFloor);
					line.Special = 0;
					break;

				case 22:
					// Raise floor to nearest height and change texture.
					sa.DoPlatform(line, PlatformType.RaiseToNearestAndChange, 0);
					line.Special = 0;
					break;

				case 25:
					// Ceiling crush and raise.
					sa.DoCeiling(line, CeilingMoveType.CrushAndRaise);
					line.Special = 0;
					break;

				case 30:
					// Raise floor to shortest texture height on either side of lines.
					sa.DoFloor(line, FloorMoveType.RaiseToTexture);
					line.Special = 0;
					break;

				case 35:
					// Lights very dark.
					sa.LightTurnOn(line, 35);
					line.Special = 0;
					break;

				case 36:
					// Lower floor (turbo).
					sa.DoFloor(line, FloorMoveType.TurboLower);
					line.Special = 0;
					break;

				case 37:
					// Lower and change.
					sa.DoFloor(line, FloorMoveType.LowerAndChange);
					line.Special = 0;
					break;

				case 38:
					// Lower floor to lowest.
					sa.DoFloor(line, FloorMoveType.LowerFloorToLowest);
					line.Special = 0;
					break;

				case 39:
					// Do teleport.
					sa.Teleport(line, side, thing);
					line.Special = 0;
					break;

				case 40:
					// Raise ceiling and lower floor.
					sa.DoCeiling(line, CeilingMoveType.RaiseToHighest);
					sa.DoFloor(line, FloorMoveType.LowerFloorToLowest);
					line.Special = 0;
					break;

				case 44:
					// Ceiling crush.
					sa.DoCeiling(line, CeilingMoveType.LowerAndCrush);
					line.Special = 0;
					break;

				case 52:
					// Do exit.
					world.ExitLevel();
					break;

				case 53:
					// Perpetual platform raise.
					sa.DoPlatform(line, PlatformType.PerpetualRaise, 0);
					line.Special = 0;
					break;

				case 54:
					// Platform stop.
					sa.StopPlatform(line);
					line.Special = 0;
					break;

				case 56:
					// Raise floor crush.
					sa.DoFloor(line, FloorMoveType.RaiseFloorCrush);
					line.Special = 0;
					break;

				case 57:
					// Ceiling crush stop.
					sa.CeilingCrushStop(line);
					line.Special = 0;
					break;

				case 58:
					// Raise floor 24.
					sa.DoFloor(line, FloorMoveType.RaiseFloor24);
					line.Special = 0;
					break;

				case 59:
					// Raise floor 24 and change.
					sa.DoFloor(line, FloorMoveType.RaiseFloor24AndChange);
					line.Special = 0;
					break;

				case 104:
					// Turn lights off in sector (tag).
					sa.TurnTagLightsOff(line);
					line.Special = 0;
					break;

				case 108:
					// Blazing door raise (faster than turbo).
					sa.DoDoor(line, VerticalDoorType.BlazeRaise);
					line.Special = 0;
					break;

				case 109:
					// Blazing door open (faster than turbo).
					sa.DoDoor(line, VerticalDoorType.BlazeOpen);
					line.Special = 0;
					break;

				case 100:
					// Build stairs turbo 16.
					sa.BuildStairs(line, StairType.Turbo16);
					line.Special = 0;
					break;

				case 110:
					// Blazing door close (faster than turbo).
					sa.DoDoor(line, VerticalDoorType.BlazeClose);
					line.Special = 0;
					break;

				case 119:
					// Raise floor to nearest surrounding floor.
					sa.DoFloor(line, FloorMoveType.RaiseFloorToNearest);
					line.Special = 0;
					break;

				case 121:
					// Blazing platform down, wait, up and stay.
					sa.DoPlatform(line, PlatformType.BlazeDwus, 0);
					line.Special = 0;
					break;

				case 124:
					// Secret exit.
					world.SecretExitLevel();
					break;

				case 125:
					// Teleport monster only.
					if (thing.Player == null)
					{
						sa.Teleport(line, side, thing);
						line.Special = 0;
					}
					break;

				case 130:
					// Raise floor turbo.
					sa.DoFloor(line, FloorMoveType.RaiseFloorTurbo);
					line.Special = 0;
					break;

				case 141:
					// Silent ceiling crush and raise.
					sa.DoCeiling(line, CeilingMoveType.SilentCrushAndRaise);
					line.Special = 0;
					break;

				// RETRIGGERS. All from here till end.
				case 72:
					// Ceiling crush.
					sa.DoCeiling(line, CeilingMoveType.LowerAndCrush);
					break;

				case 73:
					// Ceiling crush and raise.
					sa.DoCeiling(line, CeilingMoveType.CrushAndRaise);
					break;

				case 74:
					// Ceiling crush stop.
					sa.CeilingCrushStop(line);
					break;

				case 75:
					// Close door.
					sa.DoDoor(line, VerticalDoorType.Close);
					break;

				case 76:
					// Close door 30.
					sa.DoDoor(line, VerticalDoorType.Close30ThenOpen);
					break;

				case 77:
					// Fast ceiling crush and raise.
					sa.DoCeiling(line, CeilingMoveType.FastCrushAndRaise);
					break;

				case 79:
					// Lights very dark.
					sa.LightTurnOn(line, 35);
					break;

				case 80:
					// Light turn on - brightest near.
					sa.LightTurnOn(line, 0);
					break;

				case 81:
					// Light turn on 255.
					sa.LightTurnOn(line, 255);
					break;

				case 82:
					// Lower floor to lowest.
					sa.DoFloor(line, FloorMoveType.LowerFloorToLowest);
					break;

				case 83:
					// Lower floor.
					sa.DoFloor(line, FloorMoveType.LowerFloor);
					break;

				case 84:
					// Lower and change.
					sa.DoFloor(line, FloorMoveType.LowerAndChange);
					break;

				case 86:
					// Open door.
					sa.DoDoor(line, VerticalDoorType.Open);
					break;

				case 87:
					// Perpetual platform raise.
					sa.DoPlatform(line, PlatformType.PerpetualRaise, 0);
					break;

				case 88:
					// Platform down, wait, up and stay.
					sa.DoPlatform(line, PlatformType.DownWaitUpStay, 0);
					break;

				case 89:
					// Platform stop.
					sa.StopPlatform(line);
					break;

				case 90:
					// Raise door.
					sa.DoDoor(line, VerticalDoorType.Normal);
					break;

				case 91:
					// Raise floor.
					sa.DoFloor(line, FloorMoveType.RaiseFloor);
					break;

				case 92:
					// Raise floor 24.
					sa.DoFloor(line, FloorMoveType.RaiseFloor24);
					break;

				case 93:
					// Raise floor 24 and change.
					sa.DoFloor(line, FloorMoveType.RaiseFloor24AndChange);
					break;

				case 94:
					// Raise Floor Crush
					sa.DoFloor(line, FloorMoveType.RaiseFloorCrush);
					break;

				case 95:
					// Raise floor to nearest height and change texture.
					sa.DoPlatform(line, PlatformType.RaiseToNearestAndChange, 0);
					break;

				case 96:
					// Raise floor to shortest texture height on either side of lines.
					sa.DoFloor(line, FloorMoveType.RaiseToTexture);
					break;

				case 97:
					// Do Teleport.
					sa.Teleport(line, side, thing);
					break;

				case 98:
					// Lower floor (turbo).
					sa.DoFloor(line, FloorMoveType.TurboLower);
					break;

				case 105:
					// Blazing door raise (faster than turbo).
					sa.DoDoor(line, VerticalDoorType.BlazeRaise);
					break;

				case 106:
					// Blazing door open (faster than turbo).
					sa.DoDoor(line, VerticalDoorType.BlazeOpen);
					break;

				case 107:
					// Blazing door close (faster than turbo).
					sa.DoDoor(line, VerticalDoorType.BlazeClose);
					break;

				case 120:
					// Blazing platform down, wait, up and stay.
					sa.DoPlatform(line, PlatformType.BlazeDwus, 0);
					break;

				case 126:
					// Teleport monster only.
					if (thing.Player == null)
					{
						sa.Teleport(line, side, thing);
					}
					break;

				case 128:
					// Raise to nearest floor.
					sa.DoFloor(line, FloorMoveType.RaiseFloorToNearest);
					break;

				case 129:
					// Raise floor turbo.
					sa.DoFloor(line, FloorMoveType.RaiseFloorTurbo);
					break;
			}
		}



		////////////////////////////////////////////////////////////
		// Line shoot
		////////////////////////////////////////////////////////////

		/// <summary>
		/// Called when a thing shoots a special line.
		/// </summary>
		public void ShootSpecialLine(Mobj thing, LineDef line)
		{
			bool ok;

			//	Impacts that other things can activate.
			if (thing.Player == null)
			{
				ok = false;
				switch ((int)line.Special)
				{
					case 46:
						// Open door impact.
						ok = true;
						break;
				}
				if (!ok)
				{
					return;
				}
			}

			var sa = world.SectorAction;
			var specials = world.Specials;

			switch ((int)line.Special)
			{
				case 24:
					// Raise floor.
					sa.DoFloor(line, FloorMoveType.RaiseFloor);
					specials.ChangeSwitchTexture(line, false);
					break;

				case 46:
					// Open door.
					sa.DoDoor(line, VerticalDoorType.Open);
					specials.ChangeSwitchTexture(line, true);
					break;

				case 47:
					// Raise floor near and change.
					sa.DoPlatform(line, PlatformType.RaiseToNearestAndChange, 0);
					specials.ChangeSwitchTexture(line, false);
					break;
			}
		}
	}
}
