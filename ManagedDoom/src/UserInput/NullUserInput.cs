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

namespace ManagedDoom.UserInput
{
    public sealed class NullUserInput : IUserInput
    {
        private static NullUserInput instance;

        public static NullUserInput GetInstance()
        {
            if (instance == null)
            {
                instance = new NullUserInput();
            }

            return instance;
        }

        public void BuildTicCmd(TicCmd cmd)
        {
        }

        public void Reset()
        {
        }

        public void GrabMouse()
        {
        }

        public void ReleaseMouse()
        {
        }

        public int MaxMouseSensitivity
        {
            get
            {
                return 9;
            }
        }

        public int MouseSensitivity
        {
            get
            {
                return 3;
            }

            set
            {
            }
        }
    }
}
