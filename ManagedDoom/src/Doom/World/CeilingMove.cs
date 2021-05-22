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
	public sealed class CeilingMove : Thinker
	{
		private World world;

		private CeilingMoveType type;
		private Sector sector;
		private Fixed bottomHeight;
		private Fixed topHeight;
		private Fixed speed;
		private bool crush;

		// 1 = up, 0 = waiting, -1 = down.
		private int direction;

		// Corresponding sector tag.
		private int tag;

		private int oldDirection;

		public CeilingMove(World world)
		{
			this.world = world;
		}

		public override void Run()
		{
			SectorActionResult result;

			var sa = world.SectorAction;

			switch (direction)
			{
				case 0:
					// In statis.
					break;

				case 1:
					// Up.
					result = sa.MovePlane(
						sector,
						speed,
						topHeight,
						false,
						1,
						direction);

					if ((world.LevelTime & 7) == 0)
					{
						switch (type)
						{
							case CeilingMoveType.SilentCrushAndRaise:
								break;

							default:
								world.StartSound(sector.SoundOrigin, Sfx.STNMOV, SfxType.Misc);
								break;
						}
					}

					if (result == SectorActionResult.PastDestination)
					{
						switch (type)
						{
							case CeilingMoveType.RaiseToHighest:
								sa.RemoveActiveCeiling(this);
								break;

							case CeilingMoveType.SilentCrushAndRaise:
							case CeilingMoveType.FastCrushAndRaise:
							case CeilingMoveType.CrushAndRaise:
								if (type == CeilingMoveType.SilentCrushAndRaise)
								{
									world.StartSound(sector.SoundOrigin, Sfx.PSTOP, SfxType.Misc);
								}
								direction = -1;
								break;

							default:
								break;
						}

					}
					break;

				case -1:
					// Down.
					result = sa.MovePlane(
						sector,
						speed,
						bottomHeight,
						crush,
						1,
						direction);

					if ((world.LevelTime & 7) == 0)
					{
						switch (type)
						{
							case CeilingMoveType.SilentCrushAndRaise:
								break;

							default:
								world.StartSound(sector.SoundOrigin, Sfx.STNMOV, SfxType.Misc);
								break;
						}
					}

					if (result == SectorActionResult.PastDestination)
					{
						switch (type)
						{
							case CeilingMoveType.SilentCrushAndRaise:
							case CeilingMoveType.CrushAndRaise:
							case CeilingMoveType.FastCrushAndRaise:
								if (type == CeilingMoveType.SilentCrushAndRaise)
								{
									world.StartSound(sector.SoundOrigin, Sfx.PSTOP, SfxType.Misc);
									speed = SectorAction.CeilingSpeed;
								}
								if (type == CeilingMoveType.CrushAndRaise)
								{
									speed = SectorAction.CeilingSpeed;
								}
								direction = 1;
								break;

							case CeilingMoveType.LowerAndCrush:
							case CeilingMoveType.LowerToFloor:
								sa.RemoveActiveCeiling(this);
								break;

							default:
								break;
						}
					}
					else
					{
						if (result == SectorActionResult.Crushed)
						{
							switch (type)
							{
								case CeilingMoveType.SilentCrushAndRaise:
								case CeilingMoveType.CrushAndRaise:
								case CeilingMoveType.LowerAndCrush:
									speed = SectorAction.CeilingSpeed / 8;
									break;

								default:
									break;
							}
						}
					}
					break;
			}
		}

		public CeilingMoveType Type
		{
			get => type;
			set => type = value;
		}

		public Sector Sector
		{
			get => sector;
			set => sector = value;
		}

		public Fixed BottomHeight
		{
			get => bottomHeight;
			set => bottomHeight = value;
		}

		public Fixed TopHeight
		{
			get => topHeight;
			set => topHeight = value;
		}

		public Fixed Speed
		{
			get => speed;
			set => speed = value;
		}

		public bool Crush
		{
			get => crush;
			set => crush = value;
		}

		public int Direction
		{
			get => direction;
			set => direction = value;
		}

		public int Tag
		{
			get => tag;
			set => tag = value;
		}

		public int OldDirection
		{
			get => oldDirection;
			set => oldDirection = value;
		}
	}
}
