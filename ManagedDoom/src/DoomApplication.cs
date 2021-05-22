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
using System.Runtime.ExceptionServices;
using Content.Shared.Input;
using ManagedDoom.SoftwareRendering;
using ManagedDoom.Audio;
using ManagedDoom.UserInput;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Input;

namespace ManagedDoom
{
    public sealed class DoomApplication : IDisposable
    {
        private readonly IRenderTarget _renderTarget;
        private Config config;

        private CommonResource resource;
        private SfmlRenderer renderer;
        private SfmlSound sound;
        private RobustMusic music;
        private SfmlUserInput userInput;

        private List<DoomEvent> events;

        private GameOptions options;

        private DoomMenu menu;

        private OpeningSequence opening;

        private DemoPlayback demoPlayback;

        private TicCmd[] cmds;
        private DoomGame game;

        private WipeEffect wipe;
        private bool wiping;

        private ApplicationState currentState;
        private ApplicationState nextState;
        private bool needWipe;

        private bool sendPause;

        private bool quit;
        private string quitMessage;

        private bool mouseGrabbed;

        public DoomApplication(CommandLineArgs args, IRenderTarget renderTarget)
        {
            _renderTarget = renderTarget;
            config = new Config(ConfigUtilities.GetConfigPath());

            try
            {
                config.video_screenwidth = renderTarget.Size.X;
                config.video_screenheight = renderTarget.Size.Y;

                if (args.deh.Present)
                {
                    DeHackEd.ReadFiles(args.deh.Value);
                }

                resource = new CommonResource(GetWadPaths(args), !args.nodeh.Present);

                renderer = new SfmlRenderer(config, renderTarget, resource);

                if (!args.nosound.Present && !args.nosfx.Present)
                {
                    sound = new SfmlSound(config, resource.Wad);
                }

                if (!args.nosound.Present && !args.nomusic.Present)
                {
                    music = new RobustMusic(config, resource.Wad);
                }

                userInput = new SfmlUserInput(config, null, !args.nomouse.Present);

                events = new List<DoomEvent>();

                options = new GameOptions();
                options.GameVersion = resource.Wad.GameVersion;
                options.GameMode = resource.Wad.GameMode;
                options.MissionPack = resource.Wad.MissionPack;
                options.Renderer = renderer;
                options.Sound = sound;
                options.Music = music;
                options.UserInput = userInput;

                menu = new DoomMenu(this);

                opening = new OpeningSequence(resource, options);

                cmds = new TicCmd[Player.MaxPlayerCount];
                for (var i = 0; i < Player.MaxPlayerCount; i++)
                {
                    cmds[i] = new TicCmd();
                }

                game = new DoomGame(resource, options);

                wipe = new WipeEffect(renderer.WipeBandCount, renderer.WipeHeight);
                wiping = false;

                currentState = ApplicationState.None;
                nextState = ApplicationState.Opening;
                needWipe = false;

                sendPause = false;

                quit = false;
                quitMessage = null;

                CheckGameArgs(args);

                /*
                window.Closed += (sender, e) => window.Close();
                window.KeyPressed += KeyPressed;
                window.KeyReleased += KeyReleased;

                if (!args.timedemo.Present)
                {
                    window.SetFramerateLimit(35);
                }
                */

                mouseGrabbed = false;
            }
            catch (Exception e)
            {
                Dispose();
                ExceptionDispatchInfo.Throw(e);
            }
        }

        private string[] GetWadPaths(CommandLineArgs args)
        {
            var wadPaths = new List<string>();

            if (args.iwad.Present)
            {
                wadPaths.Add(args.iwad.Value);
            }
            else
            {
                wadPaths.Add(ConfigUtilities.GetDefaultIwadPath());
            }

            if (args.file.Present)
            {
                foreach (var path in args.file.Value)
                {
                    wadPaths.Add(path);
                }
            }

            return wadPaths.ToArray();
        }

        private void CheckGameArgs(CommandLineArgs args)
        {
            if (args.warp.Present)
            {
                nextState = ApplicationState.Game;
                options.Episode = args.warp.Value.Item1;
                options.Map = args.warp.Value.Item2;
                game.DeferedInitNew();
            }
            else if (args.episode.Present)
            {
                nextState = ApplicationState.Game;
                options.Episode = args.episode.Value;
                options.Map = 1;
                game.DeferedInitNew();
            }

            if (args.skill.Present)
            {
                options.Skill = (GameSkill) (args.skill.Value - 1);
            }

            if (args.deathmatch.Present)
            {
                options.Deathmatch = 1;
            }

            if (args.altdeath.Present)
            {
                options.Deathmatch = 2;
            }

            if (args.fast.Present)
            {
                options.FastMonsters = true;
            }

            if (args.respawn.Present)
            {
                options.RespawnMonsters = true;
            }

            if (args.nomonsters.Present)
            {
                options.NoMonsters = true;
            }

            if (args.loadgame.Present)
            {
                nextState = ApplicationState.Game;
                game.LoadGame(args.loadgame.Value);
            }

            if (args.playdemo.Present)
            {
                nextState = ApplicationState.DemoPlayback;
                demoPlayback = new DemoPlayback(resource, options, args.playdemo.Value);
            }

            if (args.timedemo.Present)
            {
                nextState = ApplicationState.DemoPlayback;
                demoPlayback = new DemoPlayback(resource, options, args.timedemo.Value);
            }
        }

        /*
        public void Run()
        {
            while (window.IsOpen)
            {
                window.DispatchEvents();
                DoEvents();
                if (Update() == UpdateResult.Completed)
                {
                    break;
                }
            }

            config.Save(ConfigUtilities.GetConfigPath());
        }
        */

        public void NewGame(GameSkill skill, int episode, int map)
        {
            game.DeferedInitNew(skill, episode, map);
            nextState = ApplicationState.Game;
        }

        public void EndGame()
        {
            nextState = ApplicationState.Opening;
        }

        public void DoEvents()
        {
            if (wiping)
            {
                return;
            }

            foreach (var e in events)
            {
                if (menu.DoEvent(e))
                {
                    continue;
                }

                if (e.Type == EventType.KeyDown)
                {
                    if (CheckFunctionKey(e.Key))
                    {
                        continue;
                    }
                }

                if (currentState == ApplicationState.Game)
                {
                    if (e.Key == DoomKey.Pause && e.Type == EventType.KeyDown)
                    {
                        sendPause = true;
                        continue;
                    }

                    if (game.DoEvent(e))
                    {
                        continue;
                    }
                }
                else if (currentState == ApplicationState.DemoPlayback)
                {
                    demoPlayback.DoEvent(e);
                }
            }

            events.Clear();
        }

        private bool CheckFunctionKey(DoomKey key)
        {
            switch (key)
            {
                case DoomKey.F1:
                    menu.ShowHelpScreen();
                    return true;

                case DoomKey.F2:
                    menu.ShowSaveScreen();
                    return true;

                case DoomKey.F3:
                    menu.ShowLoadScreen();
                    return true;

                case DoomKey.F4:
                    menu.ShowVolumeControl();
                    return true;

                case DoomKey.F6:
                    menu.QuickSave();
                    return true;

                case DoomKey.F7:
                    if (currentState == ApplicationState.Game)
                    {
                        menu.EndGame();
                    }
                    else
                    {
                        options.Sound.StartSound(Sfx.OOF);
                    }

                    return true;

                case DoomKey.F8:
                    renderer.DisplayMessage = !renderer.DisplayMessage;
                    if (currentState == ApplicationState.Game && game.State == GameState.Level)
                    {
                        string msg;
                        if (renderer.DisplayMessage)
                        {
                            msg = DoomInfo.Strings.MSGON;
                        }
                        else
                        {
                            msg = DoomInfo.Strings.MSGOFF;
                        }

                        game.World.ConsolePlayer.SendMessage(msg);
                    }

                    menu.StartSound(Sfx.SWTCHN);
                    return true;

                case DoomKey.F9:
                    menu.QuickLoad();
                    return true;

                case DoomKey.F10:
                    menu.Quit();
                    return true;

                case DoomKey.F11:
                    var gcl = renderer.GammaCorrectionLevel;
                    gcl++;
                    if (gcl > renderer.MaxGammaCorrectionLevel)
                    {
                        gcl = 0;
                    }

                    renderer.GammaCorrectionLevel = gcl;
                    if (currentState == ApplicationState.Game && game.State == GameState.Level)
                    {
                        string msg;
                        if (gcl == 0)
                        {
                            msg = DoomInfo.Strings.GAMMALVL0;
                        }
                        else
                        {
                            msg = "Gamma correction level " + gcl;
                        }

                        game.World.ConsolePlayer.SendMessage(msg);
                    }

                    return true;

                case DoomKey.Add:
                case DoomKey.Quote:
                    if (currentState == ApplicationState.Game &&
                        game.State == GameState.Level &&
                        game.World.AutoMap.Visible)
                    {
                        return false;
                    }
                    else
                    {
                        renderer.WindowSize = Math.Min(renderer.WindowSize + 1, renderer.MaxWindowSize);
                        options.Sound.StartSound(Sfx.STNMOV);
                        return true;
                    }

                case DoomKey.Subtract:
                case DoomKey.Hyphen:
                    if (currentState == ApplicationState.Game &&
                        game.State == GameState.Level &&
                        game.World.AutoMap.Visible)
                    {
                        return false;
                    }
                    else
                    {
                        renderer.WindowSize = Math.Max(renderer.WindowSize - 1, 0);
                        options.Sound.StartSound(Sfx.STNMOV);
                        return true;
                    }

                default:
                    return false;
            }
        }

        public UpdateResult UpdateAndRender(IRenderHandle rh)
        {
            if (!wiping)
            {
                menu.Update();

                if (nextState != currentState)
                {
                    if (nextState != ApplicationState.Opening)
                    {
                        opening.Reset();
                    }

                    if (nextState != ApplicationState.DemoPlayback)
                    {
                        demoPlayback = null;
                    }

                    currentState = nextState;
                }

                if (quit)
                {
                    return UpdateResult.Completed;
                }

                if (needWipe)
                {
                    needWipe = false;
                    StartWipe();
                }
            }

            if (!wiping)
            {
                switch (currentState)
                {
                    case ApplicationState.Opening:
                        if (opening.Update() == UpdateResult.NeedWipe)
                        {
                            StartWipe();
                        }

                        break;

                    case ApplicationState.DemoPlayback:
                        var result = demoPlayback.Update();
                        if (result == UpdateResult.NeedWipe)
                        {
                            StartWipe();
                        }
                        else if (result == UpdateResult.Completed)
                        {
                            Quit("FPS: " + demoPlayback.Fps.ToString("0.0"));
                        }

                        break;

                    case ApplicationState.Game:
                        userInput.BuildTicCmd(cmds[options.ConsolePlayer]);
                        if (sendPause)
                        {
                            sendPause = false;
                            cmds[options.ConsolePlayer].Buttons |= (byte) (TicCmdButtons.Special | TicCmdButtons.Pause);
                        }

                        if (game.Update(cmds) == UpdateResult.NeedWipe)
                        {
                            StartWipe();
                        }

                        break;

                    default:
                        throw new Exception("Invalid application state!");
                }
            }

            rh.RenderInRenderTarget(_renderTarget, () =>
            {
                if (wiping)
                {
                    var result = wipe.Update();
                    renderer.RenderWipe(this, wipe, rh.DrawingHandleScreen);
                    if (result == UpdateResult.Completed)
                    {
                        wiping = false;
                    }
                }
                else
                {
                    renderer.Render(this, rh.DrawingHandleScreen);
                }
            });

            options.Sound.Update();

            CheckMouseState();

            return UpdateResult.None;
        }

        private static readonly Dictionary<BoundKeyFunction, DoomKey> DoomKeyMap = new()
        {
            {ContentKeyFunctions.DoomEsc, DoomKey.Escape},
            {ContentKeyFunctions.DoomEnter, DoomKey.Enter},
            {ContentKeyFunctions.DoomW, DoomKey.W},
            {ContentKeyFunctions.DoomS, DoomKey.S},
            {ContentKeyFunctions.DoomA, DoomKey.A},
            {ContentKeyFunctions.DoomD, DoomKey.D},
            {ContentKeyFunctions.DoomUp, DoomKey.Up},
            {ContentKeyFunctions.DoomDown, DoomKey.Down},
            {ContentKeyFunctions.DoomLeft, DoomKey.Left},
            {ContentKeyFunctions.DoomRight, DoomKey.Right},
            {ContentKeyFunctions.DoomLControl, DoomKey.LControl},
            {ContentKeyFunctions.DoomRControl, DoomKey.RControl},
            {ContentKeyFunctions.DoomSpace, DoomKey.Space},
            {ContentKeyFunctions.DoomLShift, DoomKey.LShift},
            {ContentKeyFunctions.DoomRShift, DoomKey.RShift},
            {ContentKeyFunctions.DoomLAlt, DoomKey.LAlt},
            {ContentKeyFunctions.DoomRAlt, DoomKey.RAlt},
            {ContentKeyFunctions.DoomNum1, DoomKey.Num1},
            {ContentKeyFunctions.DoomNum2, DoomKey.Num2},
            {ContentKeyFunctions.DoomNum3, DoomKey.Num3},
            {ContentKeyFunctions.DoomNum4, DoomKey.Num4},
            {ContentKeyFunctions.DoomNum5, DoomKey.Num5},
            {ContentKeyFunctions.DoomNum6, DoomKey.Num6},
            {ContentKeyFunctions.DoomNum7, DoomKey.Num7},
        };

        public void KeyPressed(object sender, BoundKeyEventArgs e)
        {
            if (events.Count < 64)
            {
                if (!DoomKeyMap.TryGetValue(e.Function, out var key))
                    return;

                events.Add(new DoomEvent(EventType.KeyDown, key));
            }
        }

        public void KeyReleased(object sender, BoundKeyEventArgs e)
        {
            if (events.Count < 64)
            {
                if (!DoomKeyMap.TryGetValue(e.Function, out var key))
                    return;

                events.Add(new DoomEvent(EventType.KeyUp, key));
            }
        }

        private void CheckMouseState()
        {
            /*bool mouseShouldBeGrabbed;
            if (!window.HasFocus())
            {
                mouseShouldBeGrabbed = false;
            }
            else if (config.video_fullscreen)
            {
                mouseShouldBeGrabbed = true;
            }
            else
            {
                mouseShouldBeGrabbed = currentState == ApplicationState.Game && !menu.Active;
            }

            if (mouseGrabbed)
            {
                if (!mouseShouldBeGrabbed)
                {
                    userInput.ReleaseMouse();
                    mouseGrabbed = false;
                }
            }
            else
            {
                if (mouseShouldBeGrabbed)
                {
                    userInput.GrabMouse();
                    mouseGrabbed = true;
                }
            }*/
        }

        private void StartWipe()
        {
            wipe.Start();
            renderer.InitializeWipe();
            wiping = true;
        }

        public void PauseGame()
        {
            if (currentState == ApplicationState.Game &&
                game.State == GameState.Level &&
                !game.Paused && !sendPause)
            {
                sendPause = true;
            }
        }

        public void ResumeGame()
        {
            if (currentState == ApplicationState.Game &&
                game.State == GameState.Level &&
                game.Paused && !sendPause)
            {
                sendPause = true;
            }
        }

        public bool SaveGame(int slotNumber, string description)
        {
            if (currentState == ApplicationState.Game && game.State == GameState.Level)
            {
                game.SaveGame(slotNumber, description);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void LoadGame(int slotNumber)
        {
            game.LoadGame(slotNumber);
            nextState = ApplicationState.Game;
        }

        public void Quit()
        {
            quit = true;
        }

        public void Quit(string message)
        {
            quit = true;
            quitMessage = message;
        }

        public void Dispose()
        {
            if (userInput != null)
            {
                userInput.Dispose();
                userInput = null;
            }

            if (music != null)
            {
                music.Dispose();
                music = null;
            }

            if (sound != null)
            {
                sound.Dispose();
                sound = null;
            }

            if (renderer != null)
            {
                renderer.Dispose();
                renderer = null;
            }

            if (resource != null)
            {
                resource.Dispose();
                resource = null;
            }

            /*if (window != null)
            {
                window.Dispose();
                window = null;
            }*/
        }

        public ApplicationState State => currentState;
        public OpeningSequence Opening => opening;
        public DemoPlayback DemoPlayback => demoPlayback;
        public GameOptions Options => options;
        public DoomGame Game => game;
        public DoomMenu Menu => menu;
        public string QuitMessage => quitMessage;
    }
}
