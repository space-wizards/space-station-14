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
	public sealed class Platform : Thinker
	{
		private World world;

		private Sector sector;
		private Fixed speed;
		private Fixed low;
		private Fixed high;
		private int wait;
		private int count;
		private PlatformState status;
		private PlatformState oldStatus;
		private bool crush;
		private int tag;
		private PlatformType type;

		public Platform(World world)
		{
			this.world = world;
		}

		public override void Run()
		{
			var sa = world.SectorAction;

			SectorActionResult result;

			switch (status)
			{
				case PlatformState.Up:
					result = sa.MovePlane(sector, speed, high, crush, 0, 1);

					if (type == PlatformType.RaiseAndChange ||
						type == PlatformType.RaiseToNearestAndChange)
					{
						if (((world.LevelTime + sector.Number) & 7) == 0)
						{
							world.StartSound(sector.SoundOrigin, Sfx.STNMOV, SfxType.Misc);
						}
					}

					if (result == SectorActionResult.Crushed && !crush)
					{
						count = wait;
						status = PlatformState.Down;
						world.StartSound(sector.SoundOrigin, Sfx.PSTART, SfxType.Misc);
					}
					else
					{
						if (result == SectorActionResult.PastDestination)
						{
							count = wait;
							status = PlatformState.Waiting;
							world.StartSound(sector.SoundOrigin, Sfx.PSTOP, SfxType.Misc);

							switch (type)
							{
								case PlatformType.BlazeDwus:
								case PlatformType.DownWaitUpStay:
									sa.RemoveActivePlatform(this);
									break;

								case PlatformType.RaiseAndChange:
								case PlatformType.RaiseToNearestAndChange:
									sa.RemoveActivePlatform(this);
									break;

								default:
									break;
							}
						}
					}

					break;

				case PlatformState.Down:
					result = sa.MovePlane(sector, speed, low, false, 0, -1);

					if (result == SectorActionResult.PastDestination)
					{
						count = wait;
						status = PlatformState.Waiting;
						world.StartSound(sector.SoundOrigin, Sfx.PSTOP, SfxType.Misc);
					}

					break;

				case PlatformState.Waiting:
					if (--count == 0)
					{
						if (sector.FloorHeight == low)
						{
							status = PlatformState.Up;
						}
						else
						{
							status = PlatformState.Down;
						}
						world.StartSound(sector.SoundOrigin, Sfx.PSTART, SfxType.Misc);
					}

					break;

				case PlatformState.InStasis:
					break;
			}
		}

		public Sector Sector
		{
			get => sector;
			set => sector = value;
		}

		public Fixed Speed
		{
			get => speed;
			set => speed = value;
		}

		public Fixed Low
		{
			get => low;
			set => low = value;
		}

		public Fixed High
		{
			get => high;
			set => high = value;
		}

		public int Wait
		{
			get => wait;
			set => wait = value;
		}

		public int Count
		{
			get => count;
			set => count = value;
		}

		public PlatformState Status
		{
			get => status;
			set => status = value;
		}

		public PlatformState OldStatus
		{
			get => oldStatus;
			set => oldStatus = value;
		}

		public bool Crush
		{
			get => crush;
			set => crush = value;
		}

		public int Tag
		{
			get => tag;
			set => tag = value;
		}

		public PlatformType Type
		{
			get => type;
			set => type = value;
		}
	}
}
