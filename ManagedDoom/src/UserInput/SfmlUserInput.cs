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
using System.Linq;
using System.Runtime.ExceptionServices;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.IoC;

namespace ManagedDoom.UserInput
{
    public sealed class SfmlUserInput : IUserInput, IDisposable
    {
        private Config config;

        private object window;

        private bool useMouse;

        private bool[] weaponKeys;
        private int turnHeld;

        private bool mouseGrabbed;
        private int windowCenterX;
        private int windowCenterY;
        private int mouseX;
        private int mouseY;
        private bool cursorCentered;

        public SfmlUserInput(Config config, object window, bool useMouse)
        {
            try
            {
                Console.Write("Initialize user input: ");

                this.config = config;

                config.mouse_sensitivity = Math.Max(config.mouse_sensitivity, 0);

                this.window = window;

                this.useMouse = useMouse;

                weaponKeys = new bool[7];
                turnHeld = 0;

                mouseGrabbed = false;
                /*
                windowCenterX = (int)window.Size.X / 2;
                windowCenterY = (int)window.Size.Y / 2;
                */
                mouseX = 0;
                mouseY = 0;
                cursorCentered = false;

                Console.WriteLine("OK");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed");
                Dispose();
                ExceptionDispatchInfo.Throw(e);
            }
        }

        public void BuildTicCmd(TicCmd cmd)
        {
            var inputMgr = IoCManager.Resolve<IInputManager>();
            var downFunctions = inputMgr.DownKeyFunctions.ToList();
            var keyForward = IsPressed(downFunctions, config.key_forward);
            var keyBackward = IsPressed(downFunctions, config.key_backward);
            var keyStrafeLeft = IsPressed(downFunctions, config.key_strafeleft);
            var keyStrafeRight = IsPressed(downFunctions, config.key_straferight);
            var keyTurnLeft = IsPressed(downFunctions, config.key_turnleft);
            var keyTurnRight = IsPressed(downFunctions, config.key_turnright);
            var keyFire = IsPressed(downFunctions, config.key_fire);
            var keyUse = IsPressed(downFunctions, config.key_use);
            var keyRun = IsPressed(downFunctions, config.key_run);
            var keyStrafe = IsPressed(downFunctions, config.key_strafe);

            weaponKeys[0] = downFunctions.Contains(ContentKeyFunctions.DoomNum1);
            weaponKeys[1] = downFunctions.Contains(ContentKeyFunctions.DoomNum2);
            weaponKeys[2] = downFunctions.Contains(ContentKeyFunctions.DoomNum3);
            weaponKeys[3] = downFunctions.Contains(ContentKeyFunctions.DoomNum4);
            weaponKeys[4] = downFunctions.Contains(ContentKeyFunctions.DoomNum5);
            weaponKeys[5] = downFunctions.Contains(ContentKeyFunctions.DoomNum6);
            weaponKeys[6] = downFunctions.Contains(ContentKeyFunctions.DoomNum7);

            cmd.Clear();

            var strafe = keyStrafe;
            var speed = keyRun ? 1 : 0;
            var forward = 0;
            var side = 0;

            if (config.game_alwaysrun)
            {
                speed = 1 - speed;
            }

            if (keyTurnLeft || keyTurnRight)
            {
                turnHeld++;
            }
            else
            {
                turnHeld = 0;
            }

            int turnSpeed;
            if (turnHeld < PlayerBehavior.SlowTurnTics)
            {
                turnSpeed = 2;
            }
            else
            {
                turnSpeed = speed;
            }

            if (strafe)
            {
                if (keyTurnRight)
                {
                    side += PlayerBehavior.SideMove[speed];
                }
                if (keyTurnLeft)
                {
                    side -= PlayerBehavior.SideMove[speed];
                }
            }
            else
            {
                if (keyTurnRight)
                {
                    cmd.AngleTurn -= (short)PlayerBehavior.AngleTurn[turnSpeed];
                }
                if (keyTurnLeft)
                {
                    cmd.AngleTurn += (short)PlayerBehavior.AngleTurn[turnSpeed];
                }
            }

            if (keyForward)
            {
                forward += PlayerBehavior.ForwardMove[speed];
            }
            if (keyBackward)
            {
                forward -= PlayerBehavior.ForwardMove[speed];
            }

            if (keyStrafeLeft)
            {
                side -= PlayerBehavior.SideMove[speed];
            }
            if (keyStrafeRight)
            {
                side += PlayerBehavior.SideMove[speed];
            }

            if (keyFire)
            {
                cmd.Buttons |= TicCmdButtons.Attack;
            }

            if (keyUse)
            {
                cmd.Buttons |= TicCmdButtons.Use;
            }

            // Check weapon keys.
            for (var i = 0; i < weaponKeys.Length; i++)
            {
                if (weaponKeys[i])
                {
                    cmd.Buttons |= TicCmdButtons.Change;
                    cmd.Buttons |= (byte)(i << TicCmdButtons.WeaponShift);
                    break;
                }
            }

            UpdateMouse();
            var ms = 0.5F * config.mouse_sensitivity;
            var mx = (int)MathF.Round(ms * mouseX);
            var my = (int)MathF.Round(ms * mouseY);
            forward += my;
            if (strafe)
            {
                side += mx * 2;
            }
            else
            {
                cmd.AngleTurn -= (short)(mx * 0x8);
            }

            if (forward > PlayerBehavior.MaxMove)
            {
                forward = PlayerBehavior.MaxMove;
            }
            else if (forward < -PlayerBehavior.MaxMove)
            {
                forward = -PlayerBehavior.MaxMove;
            }
            if (side > PlayerBehavior.MaxMove)
            {
                side = PlayerBehavior.MaxMove;
            }
            else if (side < -PlayerBehavior.MaxMove)
            {
                side = -PlayerBehavior.MaxMove;
            }

            cmd.ForwardMove += (sbyte)forward;
            cmd.SideMove += (sbyte)side;
        }

        private bool IsPressed(IReadOnlyList<BoundKeyFunction> downBinds, KeyBinding keyBinding)
        {
            foreach (var key in keyBinding.Keys)
            {
                var keyFunc = key switch
                {
                    DoomKey.A => ContentKeyFunctions.DoomA,
                    DoomKey.D => ContentKeyFunctions.DoomD,
                    DoomKey.S => ContentKeyFunctions.DoomS,
                    DoomKey.W => ContentKeyFunctions.DoomW,
                    DoomKey.Num1 => ContentKeyFunctions.DoomNum1,
                    DoomKey.Num2 => ContentKeyFunctions.DoomNum2,
                    DoomKey.Num3 => ContentKeyFunctions.DoomNum3,
                    DoomKey.Num4 => ContentKeyFunctions.DoomNum4,
                    DoomKey.Num5 => ContentKeyFunctions.DoomNum5,
                    DoomKey.Num6 => ContentKeyFunctions.DoomNum6,
                    DoomKey.Num7 => ContentKeyFunctions.DoomNum7,
                    DoomKey.Escape => ContentKeyFunctions.DoomEsc,
                    DoomKey.LControl => ContentKeyFunctions.DoomLControl,
                    DoomKey.LShift => ContentKeyFunctions.DoomLShift,
                    DoomKey.LAlt => ContentKeyFunctions.DoomLAlt,
                    DoomKey.RControl => ContentKeyFunctions.DoomRControl,
                    DoomKey.RShift => ContentKeyFunctions.DoomRShift,
                    DoomKey.RAlt => ContentKeyFunctions.DoomRAlt,
                    DoomKey.Space => ContentKeyFunctions.DoomSpace,
                    DoomKey.Enter => ContentKeyFunctions.DoomEnter,
                    DoomKey.Left => ContentKeyFunctions.DoomLeft,
                    DoomKey.Right => ContentKeyFunctions.DoomRight,
                    DoomKey.Up => ContentKeyFunctions.DoomUp,
                    DoomKey.Down => ContentKeyFunctions.DoomDown,
                    _ => default
                };

                if (keyFunc == default)
                    continue;

                if (downBinds.Contains(keyFunc))
                    return true;
            }

            /*
            if (mouseGrabbed)
            {
                foreach (var mouseButton in keyBinding.MouseButtons)
                {
                    if (Mouse.IsButtonPressed((Mouse.Button)mouseButton))
                    {
                        return true;
                    }
                }
            }*/

            return false;
        }

        public void Reset()
        {
            mouseX = 0;
            mouseY = 0;
            cursorCentered = false;
        }

        public void GrabMouse()
        {
            /*
            if (useMouse && !mouseGrabbed)
            {
                window.SetMouseCursorGrabbed(true);
                window.SetMouseCursorVisible(false);
                mouseGrabbed = true;
                mouseX = 0;
                mouseY = 0;
                cursorCentered = false;
            }
        */
        }

        public void ReleaseMouse()
        {
            /*
            if (useMouse && mouseGrabbed)
            {
                var posX = (int)(0.9 * window.Size.X);
                var posY = (int)(0.9 * window.Size.Y);
                Mouse.SetPosition(new Vector2i(posX, posY), window);
                window.SetMouseCursorGrabbed(false);
                window.SetMouseCursorVisible(true);
                mouseGrabbed = false;
            }
        */
        }

        private void UpdateMouse()
        {
            /*
            if (mouseGrabbed)
            {
                if (cursorCentered)
                {
                    var current = Mouse.GetPosition(window);

                    mouseX = current.X - windowCenterX;

                    if (config.mouse_disableyaxis)
                    {
                        mouseY = 0;
                    }
                    else
                    {
                        mouseY = -(current.Y - windowCenterY);
                    }
                }
                else
                {
                    mouseX = 0;
                    mouseY = 0;
                }

                Mouse.SetPosition(new Vector2i(windowCenterX, windowCenterY), window);
                var pos = Mouse.GetPosition(window);
                cursorCentered = (pos.X == windowCenterX && pos.Y == windowCenterY);
            }
            else
            {
                mouseX = 0;
                mouseY = 0;
            }
        */
        }

        public void Dispose()
        {
            Console.WriteLine("Shutdown user input.");

            ReleaseMouse();
        }

        public int MaxMouseSensitivity
        {
            get
            {
                return 15;
            }
        }

        public int MouseSensitivity
        {
            get
            {
                return config.mouse_sensitivity;
            }

            set
            {
                config.mouse_sensitivity = value;
            }
        }
    }
}
