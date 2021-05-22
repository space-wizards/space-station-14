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
	public sealed class Finale
	{
		public static readonly int TextSpeed = 3;
		public static readonly int TextWait = 250;

		private GameOptions options;

		// Stage of animation:
		// 0 = text, 1 = art screen, 2 = character cast.
		private int stage;
		private int count;

		private string flat;
		private string text;

		// For bunny scroll.
		private int scrolled;
		private bool showTheEnd;
		private int theEndIndex;

		private UpdateResult updateResult;

		public Finale(GameOptions options)
		{
			this.options = options;

			string c1Text;
			string c2Text;
			string c3Text;
			string c4Text;
			string c5Text;
			string c6Text;
			switch (options.MissionPack)
			{
				case MissionPack.Plutonia:
					c1Text = DoomInfo.Strings.P1TEXT;
					c2Text = DoomInfo.Strings.P2TEXT;
					c3Text = DoomInfo.Strings.P3TEXT;
					c4Text = DoomInfo.Strings.P4TEXT;
					c5Text = DoomInfo.Strings.P5TEXT;
					c6Text = DoomInfo.Strings.P6TEXT;
					break;

				case MissionPack.Tnt:
					c1Text = DoomInfo.Strings.T1TEXT;
					c2Text = DoomInfo.Strings.T2TEXT;
					c3Text = DoomInfo.Strings.T3TEXT;
					c4Text = DoomInfo.Strings.T4TEXT;
					c5Text = DoomInfo.Strings.T5TEXT;
					c6Text = DoomInfo.Strings.T6TEXT;
					break;

				default:
					c1Text = DoomInfo.Strings.C1TEXT;
					c2Text = DoomInfo.Strings.C2TEXT;
					c3Text = DoomInfo.Strings.C3TEXT;
					c4Text = DoomInfo.Strings.C4TEXT;
					c5Text = DoomInfo.Strings.C5TEXT;
					c6Text = DoomInfo.Strings.C6TEXT;
					break;
			}

			switch (options.GameMode)
			{
				case GameMode.Shareware:
				case GameMode.Registered:
				case GameMode.Retail:
					options.Music.StartMusic(Bgm.VICTOR, true);
					switch (options.Episode)
					{
						case 1:
							flat = "FLOOR4_8";
							text = DoomInfo.Strings.E1TEXT;
							break;

						case 2:
							flat = "SFLR6_1";
							text = DoomInfo.Strings.E2TEXT;
							break;

						case 3:
							flat = "MFLR8_4";
							text = DoomInfo.Strings.E3TEXT;
							break;

						case 4:
							flat = "MFLR8_3";
							text = DoomInfo.Strings.E4TEXT;
							break;

						default:
							break;
					}
					break;

				case GameMode.Commercial:
					options.Music.StartMusic(Bgm.READ_M, true);
					switch (options.Map)
					{
						case 6:
							flat = "SLIME16";
							text = c1Text;
							break;

						case 11:
							flat = "RROCK14";
							text = c2Text;
							break;

						case 20:
							flat = "RROCK07";
							text = c3Text;
							break;

						case 30:
							flat = "RROCK17";
							text = c4Text;
							break;

						case 15:
							flat = "RROCK13";
							text = c5Text;
							break;

						case 31:
							flat = "RROCK19";
							text = c6Text;
							break;

						default:
							break;
					}
					break;

				default:
					options.Music.StartMusic(Bgm.READ_M, true);
					flat = "F_SKY1";
					text = DoomInfo.Strings.C1TEXT;
					break;
			}

			stage = 0;
			count = 0;

			scrolled = 0;
			showTheEnd = false;
			theEndIndex = 0;
		}

		public UpdateResult Update()
		{
			updateResult = UpdateResult.None;

			// Check for skipping.
			if (options.GameMode == GameMode.Commercial && count > 50)
			{
				int i;

				// Go on to the next level.
				for (i = 0; i < Player.MaxPlayerCount; i++)
				{
					if (options.Players[i].Cmd.Buttons != 0)
					{
						break;
					}
				}

				if (i < Player.MaxPlayerCount && stage != 2)
				{
					if (options.Map == 30)
					{
						StartCast();
					}
					else
					{
						return UpdateResult.Completed;
					}
				}
			}

			// Advance animation.
			count++;

			if (stage == 2)
			{
				UpdateCast();
				return updateResult;
			}

			if (options.GameMode == GameMode.Commercial)
			{
				return updateResult;
			}

			if (stage == 0 && count > text.Length * TextSpeed + TextWait)
			{
				count = 0;
				stage = 1;
				updateResult = UpdateResult.NeedWipe;
				if (options.Episode == 3)
				{
					options.Music.StartMusic(Bgm.BUNNY, true);
				}
			}

			if (stage == 1 && options.Episode == 3)
			{
				BunnyScroll();
			}

			return updateResult;
		}

		private void BunnyScroll()
		{
			scrolled = 320 - (count - 230) / 2;
			if (scrolled > 320)
			{
				scrolled = 320;
			}
			if (scrolled < 0)
			{
				scrolled = 0;
			}

			if (count < 1130)
			{
				return;
			}

			showTheEnd = true;

			if (count < 1180)
			{
				theEndIndex = 0;
				return;
			}

			var stage = (count - 1180) / 5;
			if (stage > 6)
			{
				stage = 6;
			}
			if (stage > theEndIndex)
			{
				StartSound(Sfx.PISTOL);
				theEndIndex = stage;
			}
		}



		private static readonly CastInfo[] castorder = new CastInfo[]
		{
			new CastInfo(DoomInfo.Strings.CC_ZOMBIE, MobjType.Possessed),
			new CastInfo(DoomInfo.Strings.CC_SHOTGUN, MobjType.Shotguy),
			new CastInfo(DoomInfo.Strings.CC_HEAVY, MobjType.Chainguy),
			new CastInfo(DoomInfo.Strings.CC_IMP, MobjType.Troop),
			new CastInfo(DoomInfo.Strings.CC_DEMON, MobjType.Sergeant),
			new CastInfo(DoomInfo.Strings.CC_LOST, MobjType.Skull),
			new CastInfo(DoomInfo.Strings.CC_CACO, MobjType.Head),
			new CastInfo(DoomInfo.Strings.CC_HELL, MobjType.Knight),
			new CastInfo(DoomInfo.Strings.CC_BARON, MobjType.Bruiser),
			new CastInfo(DoomInfo.Strings.CC_ARACH, MobjType.Baby),
			new CastInfo(DoomInfo.Strings.CC_PAIN, MobjType.Pain),
			new CastInfo(DoomInfo.Strings.CC_REVEN, MobjType.Undead),
			new CastInfo(DoomInfo.Strings.CC_MANCU, MobjType.Fatso),
			new CastInfo(DoomInfo.Strings.CC_ARCH, MobjType.Vile),
			new CastInfo(DoomInfo.Strings.CC_SPIDER, MobjType.Spider),
			new CastInfo(DoomInfo.Strings.CC_CYBER, MobjType.Cyborg),
			new CastInfo(DoomInfo.Strings.CC_HERO, MobjType.Player)
		};

		private int castNumber;
		private MobjStateDef castState;
		private int castTics;
		private int castFrames;
		private bool castDeath;
		private bool castOnMelee;
		private bool castAttacking;

		private void StartCast()
		{
			stage = 2;

			castNumber = 0;
			castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)castorder[castNumber].Type].SeeState];
			castTics = castState.Tics;
			castFrames = 0;
			castDeath = false;
			castOnMelee = false;
			castAttacking = false;

			updateResult = UpdateResult.NeedWipe;

			options.Music.StartMusic(Bgm.EVIL, true);
		}

		private void UpdateCast()
		{
			if (--castTics > 0)
			{
				// Not time to change state yet.
				return;
			}

			if (castState.Tics == -1 || castState.Next == MobjState.Null)
			{
				// Switch from deathstate to next monster.
				castNumber++;
				castDeath = false;
				if (castNumber == castorder.Length)
				{
					castNumber = 0;
				}
				if (DoomInfo.MobjInfos[(int)castorder[castNumber].Type].SeeSound != 0)
				{
					StartSound(DoomInfo.MobjInfos[(int)castorder[castNumber].Type].SeeSound);
				}
				castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)castorder[castNumber].Type].SeeState];
				castFrames = 0;
			}
			else
			{
				// Just advance to next state in animation.
				if (castState == DoomInfo.States[(int)MobjState.PlayAtk1])
				{
					// Oh, gross hack!
					castAttacking = false;
					castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)castorder[castNumber].Type].SeeState];
					castFrames = 0;
					goto stopAttack;
				}
				var st = castState.Next;
				castState = DoomInfo.States[(int)st];
				castFrames++;

				// Sound hacks....
				Sfx sfx;
				switch (st)
				{
					case MobjState.PlayAtk1:
						sfx = Sfx.DSHTGN;
						break;

					case MobjState.PossAtk2:
						sfx = Sfx.PISTOL;
						break;

					case MobjState.SposAtk2:
						sfx = Sfx.SHOTGN;
						break;

					case MobjState.VileAtk2:
						sfx = Sfx.VILATK;
						break;

					case MobjState.SkelFist2:
						sfx = Sfx.SKESWG;
						break;

					case MobjState.SkelFist4:
						sfx = Sfx.SKEPCH;
						break;

					case MobjState.SkelMiss2:
						sfx = Sfx.SKEATK;
						break;

					case MobjState.FattAtk8:
					case MobjState.FattAtk5:
					case MobjState.FattAtk2:
						sfx = Sfx.FIRSHT;
						break;

					case MobjState.CposAtk2:
					case MobjState.CposAtk3:
					case MobjState.CposAtk4:
						sfx = Sfx.SHOTGN;
						break;

					case MobjState.TrooAtk3:
						sfx = Sfx.CLAW;
						break;

					case MobjState.SargAtk2:
						sfx = Sfx.SGTATK;
						break;

					case MobjState.BossAtk2:
					case MobjState.Bos2Atk2:
					case MobjState.HeadAtk2:
						sfx = Sfx.FIRSHT;
						break;

					case MobjState.SkullAtk2:
						sfx = Sfx.SKLATK;
						break;

					case MobjState.SpidAtk2:
					case MobjState.SpidAtk3:
						sfx = Sfx.SHOTGN;
						break;

					case MobjState.BspiAtk2:
						sfx = Sfx.PLASMA;
						break;

					case MobjState.CyberAtk2:
					case MobjState.CyberAtk4:
					case MobjState.CyberAtk6:
						sfx = Sfx.RLAUNC;
						break;

					case MobjState.PainAtk3:
						sfx = Sfx.SKLATK;
						break;

					default:
						sfx = 0;
						break;
				}

				if (sfx != 0)
				{
					StartSound(sfx);
				}
			}

			if (castFrames == 12)
			{
				// Go into attack frame.
				castAttacking = true;
				if (castOnMelee)
				{
					castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)castorder[castNumber].Type].MeleeState];
				}
				else
				{
					castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)castorder[castNumber].Type].MissileState];
				}

				castOnMelee = !castOnMelee;
				if (castState == DoomInfo.States[(int)MobjState.Null])
				{
					if (castOnMelee)
					{
						castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)castorder[castNumber].Type].MeleeState];
					}
					else
					{
						castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)castorder[castNumber].Type].MissileState];
					}
				}
			}

			if (castAttacking)
			{
				if (castFrames == 24 ||
					castState == DoomInfo.States[(int)DoomInfo.MobjInfos[(int)castorder[castNumber].Type].SeeState])
				{
					castAttacking = false;
					castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)castorder[castNumber].Type].SeeState];
					castFrames = 0;
				}
			}

			stopAttack:

			castTics = castState.Tics;
			if (castTics == -1)
			{
				castTics = 15;
			}
		}

		public bool DoEvent(DoomEvent e)
		{
			if (stage != 2)
			{
				return false;
			}

			if (e.Type == EventType.KeyDown)
			{
				if (castDeath)
				{
					// Already in dying frames.
					return true;
				}

				// Go into death frame.
				castDeath = true;
				castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)castorder[castNumber].Type].DeathState];
				castTics = castState.Tics;
				castFrames = 0;
				castAttacking = false;
				if (DoomInfo.MobjInfos[(int)castorder[castNumber].Type].DeathSound != 0)
				{
					StartSound(DoomInfo.MobjInfos[(int)castorder[castNumber].Type].DeathSound);
				}

				return true;
			}

			return false;
		}

		private void StartSound(Sfx sfx)
		{
			options.Sound.StartSound(sfx);
		}



		public GameOptions Options => options;
		public string Flat => flat;
		public string Text => text;
		public int Count => count;
		public int Stage => stage;

		// For cast.
		public string CastName => castorder[castNumber].Name;
		public MobjStateDef CastState => castState;

		// For bunny scroll.
		public int Scrolled => scrolled;
		public int TheEndIndex => theEndIndex;
		public bool ShowTheEnd => showTheEnd;



		private class CastInfo
		{
			public string Name;
			public MobjType Type;

			public CastInfo(string name, MobjType type)
			{
				Name = name;
				Type = type;
			}
		}
	}
}
