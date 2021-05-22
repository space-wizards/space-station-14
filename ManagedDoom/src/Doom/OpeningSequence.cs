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
    public sealed class OpeningSequence
    {
        private CommonResource resource;
        private GameOptions options;

        private OpeningSequenceState state;

        private int currentStage;
        private int nextStage;

        private int count;
        private int timer;

        private TicCmd[] cmds;
        private Demo demo;
        private DoomGame game;

        private bool reset;

        public OpeningSequence(CommonResource resource, GameOptions options)
        {
            this.resource = resource;
            this.options = options;

            cmds = new TicCmd[Player.MaxPlayerCount];
            for (var i = 0; i < Player.MaxPlayerCount; i++)
            {
                cmds[i] = new TicCmd();
            }

            currentStage = 0;
            nextStage = 0;

            reset = false;

            StartTitleScreen();
        }

        public void Reset()
        {
            currentStage = 0;
            nextStage = 0;

            demo = null;
            game = null;

            reset = true;

            StartTitleScreen();
        }

        public UpdateResult Update()
        {
            var updateResult = UpdateResult.None;

            if (nextStage != currentStage)
            {
                switch (nextStage)
                {
                    case 0:
                        StartTitleScreen();
                        break;
                    case 1:
                        StartDemo("DEMO1");
                        break;
                    case 2:
                        StartCreditScreen();
                        break;
                    case 3:
                        StartDemo("DEMO2");
                        break;
                    case 4:
                        StartTitleScreen();
                        break;
                    case 5:
                        StartDemo("DEMO3");
                        break;
                    case 6:
                        StartCreditScreen();
                        break;
                    case 7:
                        StartDemo("DEMO4");
                        break;
                }

                currentStage = nextStage;
                updateResult = UpdateResult.NeedWipe;
            }

            switch (currentStage)
            {
                case 0:
                    count++;
                    if (count == timer)
                    {
                        nextStage = 1;
                    }
                    break;

                case 1:
                    if (!demo.ReadCmd(cmds))
                    {
                        nextStage = 2;
                    }
                    else
                    {
                        game.Update(cmds);
                    }
                    break;

                case 2:
                    count++;
                    if (count == timer)
                    {
                        nextStage = 3;
                    }
                    break;

                case 3:
                    if (!demo.ReadCmd(cmds))
                    {
                        nextStage = 4;
                    }
                    else
                    {
                        game.Update(cmds);
                    }
                    break;

                case 4:
                    count++;
                    if (count == timer)
                    {
                        nextStage = 5;
                    }
                    break;

                case 5:
                    if (!demo.ReadCmd(cmds))
                    {
                        if (resource.Wad.GetLumpNumber("DEMO4") == -1)
                        {
                            nextStage = 0;
                        }
                        else
                        {
                            nextStage = 6;
                        }
                    }
                    else
                    {
                        game.Update(cmds);
                    }
                    break;

                case 6:
                    count++;
                    if (count == timer)
                    {
                        nextStage = 7;
                    }
                    break;

                case 7:
                    if (!demo.ReadCmd(cmds))
                    {
                        nextStage = 0;
                    }
                    else
                    {
                        game.Update(cmds);
                    }
                    break;
            }

            if (state == OpeningSequenceState.Title && count == 1)
            {
                if (options.GameMode == GameMode.Commercial)
                {
                    options.Music.StartMusic(Bgm.DM2TTL, false);
                }
                else
                {
                    options.Music.StartMusic(Bgm.INTRO, false);
                }
            }

            if (reset)
            {
                reset = false;
                return UpdateResult.NeedWipe;
            }
            else
            {
                return updateResult;
            }
        }

        private void StartTitleScreen()
        {
            state = OpeningSequenceState.Title;

            count = 0;
            if (options.GameMode == GameMode.Commercial)
            {
                timer = 35 * 11;
            }
            else
            {
                timer = 170;
            }
        }

        private void StartCreditScreen()
        {
            state = OpeningSequenceState.Credit;

            count = 0;
            timer = 200;
        }

        private void StartDemo(string lump)
        {
            state = OpeningSequenceState.Demo;

            demo = new Demo(resource.Wad.ReadLump(lump));
            demo.Options.GameVersion = options.GameVersion;
            demo.Options.GameMode = options.GameMode;
            demo.Options.MissionPack = options.MissionPack;
            demo.Options.Renderer = options.Renderer;
            demo.Options.Sound = options.Sound;
            demo.Options.Music = options.Music;

            game = new DoomGame(resource, demo.Options);
            game.DeferedInitNew();
        }

        public OpeningSequenceState State => state;
        public DoomGame DemoGame => game;
    }
}
