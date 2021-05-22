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
using System.Collections.Generic;

namespace ManagedDoom
{
    public sealed class Intermission
    {
        private GameOptions options;

        // Contains information passed into intermission.
        private IntermissionInfo info;
        private PlayerScores[] scores;

        // Used to accelerate or skip a stage.
        private bool accelerateStage;

        // Specifies current state.
        private IntermissionState state;

        private int[] killCount;
        private int[] itemCount;
        private int[] secretCount;
        private int[] fragCount;
        private int timeCount;
        private int parCount;
        private int pauseCount;

        private int spState;

        private int ngState;
        private bool doFrags;

        private int dmState;
        private int[][] dmFragCount;
        private int[] dmTotalCount;

        private DoomRandom random;
        private Animation[] animations;
        private bool showYouAreHere;

        // Used for general timing.
        private int count;

        // Used for timing of background animation.
        private int bgCount;

        private bool completed;

        public Intermission(GameOptions options, IntermissionInfo info)
        {
            this.options = options;
            this.info = info;

            scores = info.Players;

            killCount = new int[Player.MaxPlayerCount];
            itemCount = new int[Player.MaxPlayerCount];
            secretCount = new int[Player.MaxPlayerCount];
            fragCount = new int[Player.MaxPlayerCount];

            dmFragCount = new int[Player.MaxPlayerCount][];
            for (var i = 0; i < Player.MaxPlayerCount; i++)
            {
                dmFragCount[i] = new int[Player.MaxPlayerCount];
            }
            dmTotalCount = new int[Player.MaxPlayerCount];

            if (options.Deathmatch != 0)
            {
                InitDeathmatchStats();
            }
            else if (options.NetGame)
            {
                InitNetGameStats();
            }
            else
            {
                InitSinglePLayerStats();
            }

            completed = false;
        }



        ////////////////////////////////////////////////////////////
        // Initialization
        ////////////////////////////////////////////////////////////
        
        private void InitSinglePLayerStats()
        {
            state = IntermissionState.StatCount;
            accelerateStage = false;
            spState = 1;
            killCount[0] = itemCount[0] = secretCount[0] = -1;
            timeCount = parCount = -1;
            pauseCount = GameConst.TicRate;

            InitAnimatedBack();
        }


        private void InitNetGameStats()
        {
            state = IntermissionState.StatCount;
            accelerateStage = false;
            ngState = 1;
            pauseCount = GameConst.TicRate;

            var frags = 0;
            for (var i = 0; i < Player.MaxPlayerCount; i++)
            {
                if (!options.Players[i].InGame)
                {
                    continue;
                }

                killCount[i] = itemCount[i] = secretCount[i] = fragCount[i] = 0;

                frags += GetFragSum(i);
            }
            doFrags = frags > 0;

            InitAnimatedBack();
        }


        private void InitDeathmatchStats()
        {
            state = IntermissionState.StatCount;
            accelerateStage = false;
            dmState = 1;
            pauseCount = GameConst.TicRate;

            for (var i = 0; i < Player.MaxPlayerCount; i++)
            {
                if (options.Players[i].InGame)
                {
                    for (var j = 0; j < Player.MaxPlayerCount; j++)
                    {
                        if (options.Players[j].InGame)
                        {
                            dmFragCount[i][j] = 0;
                        }
                    }
                    dmTotalCount[i] = 0;
                }
            }

            InitAnimatedBack();
        }


        private void InitNoState()
        {
            state = IntermissionState.NoState;
            accelerateStage = false;
            count = 10;
        }


        private static readonly int showNextLocDelay = 4;

        private void InitShowNextLoc()
        {
            state = IntermissionState.ShowNextLoc;
            accelerateStage = false;
            count = showNextLocDelay * GameConst.TicRate;

            InitAnimatedBack();
        }


        private void InitAnimatedBack()
        {
            if (options.GameMode == GameMode.Commercial)
            {
                return;
            }

            if (info.Episode > 2)
            {
                return;
            }

            if (animations == null)
            {
                animations = new Animation[AnimationInfo.Episodes[info.Episode].Count];
                for (var i = 0; i < animations.Length; i++)
                {
                    animations[i] = new Animation(this, AnimationInfo.Episodes[info.Episode][i], i);
                }

                random = new DoomRandom();
            }

            foreach (var animation in animations)
            {
                animation.Reset(bgCount);
            }
        }



        ////////////////////////////////////////////////////////////
        // Update
        ////////////////////////////////////////////////////////////

        public UpdateResult Update()
        {
            // Counter for general background animation.
            bgCount++;

            CheckForAccelerate();

            if (bgCount == 1)
            {
                // intermission music
                if (options.GameMode == GameMode.Commercial)
                {
                    options.Music.StartMusic(Bgm.DM2INT, true);
                }
                else
                {
                    options.Music.StartMusic(Bgm.INTER, true);
                }
            }

            switch (state)
            {
                case IntermissionState.StatCount:
                    if (options.Deathmatch != 0)
                    {
                        UpdateDeathmatchStats();
                    }
                    else if (options.NetGame)
                    {
                        UpdateNetGameStats();
                    }
                    else
                    {
                        UpdateSinglePlayerStats();
                    }
                    break;

                case IntermissionState.ShowNextLoc:
                    UpdateShowNextLoc();
                    break;

                case IntermissionState.NoState:
                    UpdateNoState();
                    break;
            }

            if (completed)
            {
                return UpdateResult.Completed;
            }
            else
            {
                if (bgCount == 1)
                {
                    return UpdateResult.NeedWipe;
                }
                else
                {
                    return UpdateResult.None;
                }
            }
        }


        private void UpdateSinglePlayerStats()
        {
            UpdateAnimatedBack();

            if (accelerateStage && spState != 10)
            {
                accelerateStage = false;
                killCount[0] = (scores[0].KillCount * 100) / info.MaxKillCount;
                itemCount[0] = (scores[0].ItemCount * 100) / info.MaxItemCount;
                secretCount[0] = (scores[0].SecretCount * 100) / info.MaxSecretCount;
                timeCount = scores[0].Time / GameConst.TicRate;
                parCount = info.ParTime / GameConst.TicRate;
                StartSound(Sfx.BAREXP);
                spState = 10;
            }

            if (spState == 2)
            {
                killCount[0] += 2;

                if ((bgCount & 3) == 0)
                {
                    StartSound(Sfx.PISTOL);
                }

                if (killCount[0] >= (scores[0].KillCount * 100) / info.MaxKillCount)
                {
                    killCount[0] = (scores[0].KillCount * 100) / info.MaxKillCount;
                    StartSound(Sfx.BAREXP);
                    spState++;
                }
            }
            else if (spState == 4)
            {
                itemCount[0] += 2;

                if ((bgCount & 3) == 0)
                {
                    StartSound(Sfx.PISTOL);
                }

                if (itemCount[0] >= (scores[0].ItemCount * 100) / info.MaxItemCount)
                {
                    itemCount[0] = (scores[0].ItemCount * 100) / info.MaxItemCount;
                    StartSound(Sfx.BAREXP);
                    spState++;
                }
            }
            else if (spState == 6)
            {
                secretCount[0] += 2;

                if ((bgCount & 3) == 0)
                {
                    StartSound(Sfx.PISTOL);
                }

                if (secretCount[0] >= (scores[0].SecretCount * 100) / info.MaxSecretCount)
                {
                    secretCount[0] = (scores[0].SecretCount * 100) / info.MaxSecretCount;
                    StartSound(Sfx.BAREXP);
                    spState++;
                }
            }

            else if (spState == 8)
            {
                if ((bgCount & 3) == 0)
                {
                    StartSound(Sfx.PISTOL);
                }

                timeCount += 3;

                if (timeCount >= scores[0].Time / GameConst.TicRate)
                {
                    timeCount = scores[0].Time / GameConst.TicRate;
                }

                parCount += 3;

                if (parCount >= info.ParTime / GameConst.TicRate)
                {
                    parCount = info.ParTime / GameConst.TicRate;

                    if (timeCount >= scores[0].Time / GameConst.TicRate)
                    {
                        StartSound(Sfx.BAREXP);
                        spState++;
                    }
                }
            }
            else if (spState == 10)
            {
                if (accelerateStage)
                {
                    StartSound(Sfx.SGCOCK);

                    if (options.GameMode == GameMode.Commercial)
                    {
                        InitNoState();
                    }
                    else
                    {
                        InitShowNextLoc();
                    }
                }
            }
            else if ((spState & 1) != 0)
            {
                if (--pauseCount == 0)
                {
                    spState++;
                    pauseCount = GameConst.TicRate;
                }
            }
        }


        private void UpdateNetGameStats()
        {
            UpdateAnimatedBack();

            bool stillTicking;

            if (accelerateStage && ngState != 10)
            {
                accelerateStage = false;

                for (var i = 0; i < Player.MaxPlayerCount; i++)
                {
                    if (!options.Players[i].InGame)
                    {
                        continue;
                    }

                    killCount[i] = (scores[i].KillCount * 100) / info.MaxKillCount;
                    itemCount[i] = (scores[i].ItemCount * 100) / info.MaxItemCount;
                    secretCount[i] = (scores[i].SecretCount * 100) / info.MaxSecretCount;
                }

                StartSound(Sfx.BAREXP);

                ngState = 10;
            }

            if (ngState == 2)
            {
                if ((bgCount & 3) == 0)
                {
                    StartSound(Sfx.PISTOL);
                }

                stillTicking = false;

                for (var i = 0; i < Player.MaxPlayerCount; i++)
                {
                    if (!options.Players[i].InGame)
                    {
                        continue;
                    }

                    killCount[i] += 2;
                    if (killCount[i] >= (scores[i].KillCount * 100) / info.MaxKillCount)
                    {
                        killCount[i] = (scores[i].KillCount * 100) / info.MaxKillCount;
                    }
                    else
                    {
                        stillTicking = true;
                    }
                }

                if (!stillTicking)
                {
                    StartSound(Sfx.BAREXP);
                    ngState++;
                }
            }
            else if (ngState == 4)
            {
                if ((bgCount & 3) == 0)
                {
                    StartSound(Sfx.PISTOL);
                }

                stillTicking = false;

                for (var i = 0; i < Player.MaxPlayerCount; i++)
                {
                    if (!options.Players[i].InGame)
                    {
                        continue;
                    }

                    itemCount[i] += 2;
                    if (itemCount[i] >= (scores[i].ItemCount * 100) / info.MaxItemCount)
                    {
                        itemCount[i] = (scores[i].ItemCount * 100) / info.MaxItemCount;
                    }
                    else
                    {
                        stillTicking = true;
                    }
                }

                if (!stillTicking)
                {
                    StartSound(Sfx.BAREXP);
                    ngState++;
                }
            }
            else if (ngState == 6)
            {
                if ((bgCount & 3) == 0)
                {
                    StartSound(Sfx.PISTOL);
                }

                stillTicking = false;

                for (var i = 0; i < Player.MaxPlayerCount; i++)
                {
                    if (!options.Players[i].InGame)
                    {
                        continue;
                    }

                    secretCount[i] += 2;
                    if (secretCount[i] >= (scores[i].SecretCount * 100) / info.MaxSecretCount)
                    {
                        secretCount[i] = (scores[i].SecretCount * 100) / info.MaxSecretCount;
                    }
                    else
                    {
                        stillTicking = true;
                    }
                }

                if (!stillTicking)
                {
                    StartSound(Sfx.BAREXP);
                    if (doFrags)
                    {
                        ngState++;
                    }
                    else
                    {
                        ngState += 3;
                    }
                }
            }
            else if (ngState == 8)
            {
                if ((bgCount & 3) == 0)
                {
                    StartSound(Sfx.PISTOL);
                }

                stillTicking = false;

                for (var i = 0; i < Player.MaxPlayerCount; i++)
                {
                    if (!options.Players[i].InGame)
                    {
                        continue;
                    }

                    fragCount[i] += 1;
                    var sum = GetFragSum(i);
                    if (fragCount[i] >= sum)
                    {
                        fragCount[i] = sum;
                    }
                    else
                    {
                        stillTicking = true;
                    }
                }

                if (!stillTicking)
                {
                    StartSound(Sfx.PLDETH);
                    ngState++;
                }
            }
            else if (ngState == 10)
            {
                if (accelerateStage)
                {
                    StartSound(Sfx.SGCOCK);

                    if (options.GameMode == GameMode.Commercial)
                    {
                        InitNoState();
                    }
                    else
                    {
                        InitShowNextLoc();
                    }
                }
            }
            else if ((ngState & 1) != 0)
            {
                if (--pauseCount == 0)
                {
                    ngState++;
                    pauseCount = GameConst.TicRate;
                }
            }
        }


        private void UpdateDeathmatchStats()
        {
            UpdateAnimatedBack();

            bool stillticking;

            if (accelerateStage && dmState != 4)
            {
                accelerateStage = false;

                for (var i = 0; i < Player.MaxPlayerCount; i++)
                {
                    if (options.Players[i].InGame)
                    {
                        for (var j = 0; j < Player.MaxPlayerCount; j++)
                        {
                            if (options.Players[j].InGame)
                            {
                                dmFragCount[i][j] = scores[i].Frags[j];
                            }
                        }

                        dmTotalCount[i] = GetFragSum(i);
                    }
                }

                StartSound(Sfx.BAREXP);

                dmState = 4;
            }

            if (dmState == 2)
            {
                if ((bgCount & 3) == 0)
                {
                    StartSound(Sfx.PISTOL);
                }

                stillticking = false;

                for (var i = 0; i < Player.MaxPlayerCount; i++)
                {
                    if (options.Players[i].InGame)
                    {
                        for (var j = 0; j < Player.MaxPlayerCount; j++)
                        {
                            if (options.Players[j].InGame && dmFragCount[i][j] != scores[i].Frags[j])
                            {
                                if (scores[i].Frags[j] < 0)
                                {
                                    dmFragCount[i][j]--;
                                }
                                else
                                {
                                    dmFragCount[i][j]++;
                                }

                                if (dmFragCount[i][j] > 99)
                                {
                                    dmFragCount[i][j] = 99;
                                }

                                if (dmFragCount[i][j] < -99)
                                {
                                    dmFragCount[i][j] = -99;
                                }

                                stillticking = true;
                            }
                        }

                        dmTotalCount[i] = GetFragSum(i);

                        if (dmTotalCount[i] > 99)
                        {
                            dmTotalCount[i] = 99;
                        }

                        if (dmTotalCount[i] < -99)
                        {
                            dmTotalCount[i] = -99;
                        }
                    }

                }

                if (!stillticking)
                {
                    StartSound(Sfx.BAREXP);
                    dmState++;
                }

            }
            else if (dmState == 4)
            {
                if (accelerateStage)
                {
                    StartSound(Sfx.SLOP);

                    if (options.GameMode == GameMode.Commercial)
                    {
                        InitNoState();
                    }
                    else
                    {
                        InitShowNextLoc();
                    }
                }
            }
            else if ((dmState & 1) != 0)
            {
                if (--pauseCount == 0)
                {
                    dmState++;
                    pauseCount = GameConst.TicRate;
                }
            }
        }


        private void UpdateShowNextLoc()
        {
            UpdateAnimatedBack();

            if (--count == 0 || accelerateStage)
            {
                InitNoState();
            }
            else
            {
                showYouAreHere = (count & 31) < 20;
            }
        }


        private void UpdateNoState()
        {

            UpdateAnimatedBack();

            if (--count == 0)
            {
                completed = true;
            }
        }


        private void UpdateAnimatedBack()
        {
            if (options.GameMode == GameMode.Commercial)
            {
                return;
            }

            if (info.Episode > 2)
            {
                return;
            }

            foreach (var a in animations)
            {
                a.Update(bgCount);
            }
        }



        ////////////////////////////////////////////////////////////
        // Check for button press
        ////////////////////////////////////////////////////////////

        private void CheckForAccelerate()
        {
            // Check for button presses to skip delays.
            for (var i = 0; i < Player.MaxPlayerCount; i++)
            {
                var player = options.Players[i];
                if (player.InGame)
                {
                    if ((player.Cmd.Buttons & TicCmdButtons.Attack) != 0)
                    {
                        if (!player.AttackDown)
                        {
                            accelerateStage = true;
                        }
                        player.AttackDown = true;
                    }
                    else
                    {
                        player.AttackDown = false;
                    }

                    if ((player.Cmd.Buttons & TicCmdButtons.Use) != 0)
                    {
                        if (!player.UseDown)
                        {
                            accelerateStage = true;
                        }
                        player.UseDown = true;
                    }
                    else
                    {
                        player.UseDown = false;
                    }
                }
            }
        }



        ////////////////////////////////////////////////////////////
        // Miscellaneous functions
        ////////////////////////////////////////////////////////////

        private int GetFragSum(int playerNumber)
        {
            var frags = 0;

            for (var i = 0; i < Player.MaxPlayerCount; i++)
            {
                if (options.Players[i].InGame && i != playerNumber)
                {
                    frags += scores[playerNumber].Frags[i];
                }
            }

            frags -= scores[playerNumber].Frags[playerNumber];

            return frags;
        }


        private void StartSound(Sfx sfx)
        {
            options.Sound.StartSound(sfx);
        }


        
        public GameOptions Options => options;
        public IntermissionInfo Info => info;
        public IntermissionState State => state;
        public IReadOnlyList<int> KillCount => killCount;
        public IReadOnlyList<int> ItemCount => itemCount;
        public IReadOnlyList<int> SecretCount => secretCount;
        public IReadOnlyList<int> FragCount => fragCount;
        public int TimeCount => timeCount;
        public int ParCount => parCount;
        public int[][] DeathmatchFrags => dmFragCount;
        public int[] DeathmatchTotals => dmTotalCount;
        public bool DoFrags => doFrags;
        public DoomRandom Random => random;
        public Animation[] Animations => animations;
        public bool ShowYouAreHere => showYouAreHere;
    }
}
