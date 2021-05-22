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
using System.IO;

namespace ManagedDoom
{
	public sealed class DoomGame
	{
		private CommonResource resource;
		private GameOptions options;

		private GameAction gameAction;
		private GameState gameState;

		private int gameTic;
		private DoomRandom random;

		private World world;
		private Intermission intermission;
		private Finale finale;

		private bool paused;

		private int loadGameSlotNumber;
		private int saveGameSlotNumber;
		private string saveGameDescription;

		public DoomGame(CommonResource resource, GameOptions options)
		{
			this.resource = resource;
			this.options = options;

			gameAction = GameAction.Nothing;

			gameTic = 0;
			random = new DoomRandom();
		}


		////////////////////////////////////////////////////////////
		// Public methods to control the game state
		////////////////////////////////////////////////////////////

		/// <summary>
		/// Start a new game.
		/// Can be called by the startup code or the menu task.
		/// </summary>
		public void DeferedInitNew()
		{
			gameAction = GameAction.NewGame;
		}

		/// <summary>
		/// Start a new game.
		/// Can be called by the startup code or the menu task.
		/// </summary>
		public void DeferedInitNew(GameSkill skill, int episode, int map)
		{
			options.Skill = skill;
			options.Episode = episode;
			options.Map = map;
			gameAction = GameAction.NewGame;
		}

		/// <summary>
		/// Load the saved game at the given slot number.
		/// Can be called by the startup code or the menu task.
		/// </summary>
		public void LoadGame(int slotNumber)
		{
			loadGameSlotNumber = slotNumber;
			gameAction = GameAction.LoadGame;
		}

		/// <summary>
		/// Save the game at the given slot number.
		/// Can be called by the startup code or the menu task.
		/// </summary>
		public void SaveGame(int slotNumber, string description)
		{
			saveGameSlotNumber = slotNumber;
			saveGameDescription = description;
			gameAction = GameAction.SaveGame;
		}

		/// <summary>
		/// Advance the game one frame.
		/// </summary>
		public UpdateResult Update(TicCmd[] cmds)
		{
			// Do player reborns if needed.
			var players = options.Players;
			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (players[i].InGame && players[i].PlayerState == PlayerState.Reborn)
				{
					DoReborn(i);
				}
			}

			// Do things to change the game state.
			while (gameAction != GameAction.Nothing)
			{
				switch (gameAction)
				{
					case GameAction.LoadLevel:
						DoLoadLevel();
						break;
					case GameAction.NewGame:
						DoNewGame();
						break;
					case GameAction.LoadGame:
						DoLoadGame();
						break;
					case GameAction.SaveGame:
						DoSaveGame();
						break;
					case GameAction.Completed:
						DoCompleted();
						break;
					case GameAction.Victory:
						DoFinale();
						break;
					case GameAction.WorldDone:
						DoWorldDone();
						break;
					case GameAction.Nothing:
						break;
				}
			}

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (players[i].InGame)
				{
					var cmd = players[i].Cmd;
					cmd.CopyFrom(cmds[i]);

					/*
					if (demorecording)
					{
						G_WriteDemoTiccmd(cmd);
					}
					*/

					// Check for turbo cheats.
					if (cmd.ForwardMove > GameConst.TurboThreshold &&
						(world.LevelTime & 31) == 0 &&
						((world.LevelTime >> 5) & 3) == i)
					{
						var player = players[options.ConsolePlayer];
						player.SendMessage(players[i].Name + " is turbo!");
					}
				}
			}

			// Check for special buttons.
			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (players[i].InGame)
				{
					if ((players[i].Cmd.Buttons & TicCmdButtons.Special) != 0)
					{
						if ((players[i].Cmd.Buttons & TicCmdButtons.SpecialMask) == TicCmdButtons.Pause)
						{
							paused = !paused;
							if (paused)
							{
								options.Sound.Pause();
							}
							else
							{
								options.Sound.Resume();
							}
						}
					}
				}
			}

			// Do main actions.
			var result = UpdateResult.None;
			switch (gameState)
			{
				case GameState.Level:
					if (!paused || world.FirstTicIsNotYetDone)
					{
						result = world.Update();
						if (result == UpdateResult.Completed)
						{
							gameAction = GameAction.Completed;
						}
					}
					break;

				case GameState.Intermission:
					result = intermission.Update();
					if (result == UpdateResult.Completed)
					{
						gameAction = GameAction.WorldDone;

						if (world.SecretExit)
						{
							players[options.ConsolePlayer].DidSecret = true;
						}

						if (options.GameMode == GameMode.Commercial)
						{
							switch (options.Map)
							{
								case 6:
								case 11:
								case 20:
								case 30:
									DoFinale();
									result = UpdateResult.NeedWipe;
									break;

								case 15:
								case 31:
									if (world.SecretExit)
									{
										DoFinale();
										result = UpdateResult.NeedWipe;
									}
									break;
							}
						}
					}
					break;

				case GameState.Finale:
					result = finale.Update();
					if (result == UpdateResult.Completed)
					{
						gameAction = GameAction.WorldDone;
					}
					break;
			}

			gameTic++;

			if (result == UpdateResult.NeedWipe)
			{
				return UpdateResult.NeedWipe;
			}
			else
			{
				return UpdateResult.None;
			}
		}


		////////////////////////////////////////////////////////////
		// Actual game actions
		////////////////////////////////////////////////////////////

		// It seems that these methods should not be called directly
		// from outside for some reason.
		// So if you want to start a new game or do load / save, use
		// the following public methods.
		//
		//     - DeferedInitNew
		//     - LoadGame
		//     - SaveGame

		private void DoLoadLevel()
		{
			gameAction = GameAction.Nothing;

			gameState = GameState.Level;

			var players = options.Players;
			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (players[i].InGame && players[i].PlayerState == PlayerState.Dead)
				{
					players[i].PlayerState = PlayerState.Reborn;
				}
				Array.Clear(players[i].Frags, 0, players[i].Frags.Length);
			}

			intermission = null;

			options.Sound.Reset();

			world = new World(resource, options, this);

			options.UserInput.Reset();
		}

		private void DoNewGame()
		{
			gameAction = GameAction.Nothing;

			InitNew(options.Skill, options.Episode, options.Map);
		}

		private void DoLoadGame()
		{
			gameAction = GameAction.Nothing;

			var directory = ConfigUtilities.GetExeDirectory();
			var path = Path.Combine(directory, "doomsav" + loadGameSlotNumber + ".dsg");
			SaveAndLoad.Load(this, path);
		}

		private void DoSaveGame()
		{
			gameAction = GameAction.Nothing;

			var directory = ConfigUtilities.GetExeDirectory();
			var path = Path.Combine(directory, "doomsav" + saveGameSlotNumber + ".dsg");
			SaveAndLoad.Save(this, saveGameDescription, path);
			world.ConsolePlayer.SendMessage(DoomInfo.Strings.GGSAVED);
		}

		private void DoCompleted()
		{
			gameAction = GameAction.Nothing;

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (options.Players[i].InGame)
				{
					// Take away cards and stuff.
					options.Players[i].FinishLevel();
				}
			}

			if (options.GameMode != GameMode.Commercial)
			{
				switch (options.Map)
				{
					case 8:
						gameAction = GameAction.Victory;
						return;
					case 9:
						for (var i = 0; i < Player.MaxPlayerCount; i++)
						{
							options.Players[i].DidSecret = true;
						}
						break;
				}
			}

			if ((options.Map == 8) && (options.GameMode != GameMode.Commercial))
			{
				// Victory.
				gameAction = GameAction.Victory;
				return;
			}

			if ((options.Map == 9) && (options.GameMode != GameMode.Commercial))
			{
				// Exit secret level.
				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					options.Players[i].DidSecret = true;
				}
			}

			var imInfo = options.IntermissionInfo;

			imInfo.DidSecret = options.Players[options.ConsolePlayer].DidSecret;
			imInfo.Episode = options.Episode - 1;
			imInfo.LastLevel = options.Map - 1;

			// IntermissionInfo.Next is 0 biased, unlike GameOptions.Map.
			if (options.GameMode == GameMode.Commercial)
			{
				if (world.SecretExit)
				{
					switch (options.Map)
					{
						case 15:
							imInfo.NextLevel = 30;
							break;
						case 31:
							imInfo.NextLevel = 31;
							break;
					}
				}
				else
				{
					switch (options.Map)
					{
						case 31:
						case 32:
							imInfo.NextLevel = 15;
							break;
						default:
							imInfo.NextLevel = options.Map;
							break;
					}
				}
			}
			else
			{
				if (world.SecretExit)
				{
					// Go to secret level.
					imInfo.NextLevel = 8;
				}
				else if (options.Map == 9)
				{
					// Returning from secret level.
					switch (options.Episode)
					{
						case 1:
							imInfo.NextLevel = 3;
							break;
						case 2:
							imInfo.NextLevel = 5;
							break;
						case 3:
							imInfo.NextLevel = 6;
							break;
						case 4:
							imInfo.NextLevel = 2;
							break;
					}
				}
				else
				{
					// Go to next level.
					imInfo.NextLevel = options.Map;
				}
			}

			imInfo.MaxKillCount = world.TotalKills;
			imInfo.MaxItemCount = world.TotalItems;
			imInfo.MaxSecretCount = world.TotalSecrets;
			imInfo.TotalFrags = 0;
			if (options.GameMode == GameMode.Commercial)
			{
				imInfo.ParTime = 35 * DoomInfo.ParTimes.Doom2[options.Map - 1];
			}
			else
			{
				imInfo.ParTime = 35 * DoomInfo.ParTimes.Doom1[options.Episode - 1][options.Map - 1];
			}

			var players = options.Players;
			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				imInfo.Players[i].InGame = players[i].InGame;
				imInfo.Players[i].KillCount = players[i].KillCount;
				imInfo.Players[i].ItemCount = players[i].ItemCount;
				imInfo.Players[i].SecretCount = players[i].SecretCount;
				imInfo.Players[i].Time = world.LevelTime;
				Array.Copy(players[i].Frags, imInfo.Players[i].Frags, Player.MaxPlayerCount);
			}

			gameState = GameState.Intermission;
			intermission = new Intermission(options, imInfo);
		}

		private void DoWorldDone()
		{
			gameAction = GameAction.Nothing;

			gameState = GameState.Level;
			options.Map = options.IntermissionInfo.NextLevel + 1;
			DoLoadLevel();
		}

		private void DoFinale()
		{
			gameAction = GameAction.Nothing;

			gameState = GameState.Finale;
			finale = new Finale(options);
		}


		////////////////////////////////////////////////////////////
		// Miscellaneous things
		////////////////////////////////////////////////////////////

		public void InitNew(GameSkill skill, int episode, int map)
		{
			skill = (GameSkill)Math.Clamp((int)skill, (int)GameSkill.Baby, (int)GameSkill.Nightmare);

			if (options.GameMode == GameMode.Retail)
			{
				episode = Math.Clamp(episode, 1, 4);
			}
			else if (options.GameMode == GameMode.Shareware)
			{
				episode = 1;
			}
			else
			{
				episode = Math.Clamp(episode, 1, 3);
			}

			if (options.GameMode == GameMode.Commercial)
			{
				map = Math.Clamp(map, 1, 32);
			}
			else
			{
				map = Math.Clamp(map, 1, 9);
			}

			random.Clear();

			// Force players to be initialized upon first level load.
			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				options.Players[i].PlayerState = PlayerState.Reborn;
			}

			DoLoadLevel();
		}

		public bool DoEvent(DoomEvent e)
		{
			if (gameState == GameState.Level)
			{
				return world.DoEvent(e);
			}
			else if (gameState == GameState.Finale)
			{
				return finale.DoEvent(e);
			}

			return false;
		}

		private void DoReborn(int playerNumber)
		{
			if (!options.NetGame)
			{
				// Reload the level from scratch.
				gameAction = GameAction.LoadLevel;
			}
			else
			{
				// Respawn at the start.

				// First dissasociate the corpse.
				options.Players[playerNumber].Mobj.Player = null;

				var ta = world.ThingAllocation;

				// Spawn at random spot if in death match.
				if (options.Deathmatch != 0)
				{
					ta.DeathMatchSpawnPlayer(playerNumber);
					return;
				}

				if (ta.CheckSpot(playerNumber, ta.PlayerStarts[playerNumber]))
				{
					ta.SpawnPlayer(ta.PlayerStarts[playerNumber]);
					return;
				}

				// Try to spawn at one of the other players spots.
				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					if (ta.CheckSpot(playerNumber, ta.PlayerStarts[i]))
					{
						// Fake as other player.
						ta.PlayerStarts[i].Type = playerNumber + 1;

						world.ThingAllocation.SpawnPlayer(ta.PlayerStarts[i]);

						// Restore.
						ta.PlayerStarts[i].Type = i + 1;

						return;
					}
				}

				// He's going to be inside something.
				// Too bad.
				world.ThingAllocation.SpawnPlayer(ta.PlayerStarts[playerNumber]);
			}
		}


		public GameOptions Options => options;
		public Player[] Players => options.Players;
		public GameState State => gameState;
		public int GameTic => gameTic;
		public DoomRandom Random => random;
		public World World => world;
		public Intermission Intermission => intermission;
		public Finale Finale => finale;
		public bool Paused => paused;



		private enum GameAction
		{
			Nothing,
			LoadLevel,
			NewGame,
			LoadGame,
			SaveGame,
			Completed,
			Victory,
			WorldDone
		}
	}
}
