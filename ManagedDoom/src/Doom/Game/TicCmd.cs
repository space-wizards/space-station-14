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
    public sealed class TicCmd
    {
        private sbyte forwardMove;
        private sbyte sideMove;
        private short angleTurn;
        private byte buttons;

        public void Clear()
        {
            forwardMove = 0;
            sideMove = 0;
            angleTurn = 0;
            buttons = 0;
        }

        public void CopyFrom(TicCmd cmd)
        {
            forwardMove = cmd.forwardMove;
            sideMove = cmd.sideMove;
            angleTurn = cmd.angleTurn;
            buttons = cmd.buttons;
        }

        public sbyte ForwardMove
        {
            get => forwardMove;
            set => forwardMove = value;
        }

        public sbyte SideMove
        {
            get => sideMove;
            set => sideMove = value;
        }

        public short AngleTurn
        {
            get => angleTurn;
            set => angleTurn = value;
        }

        public byte Buttons
        {
            get => buttons;
            set => buttons = value;
        }
    }
}
