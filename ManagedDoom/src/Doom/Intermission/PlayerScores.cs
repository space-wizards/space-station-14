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
    public class PlayerScores
    {
        // Whether the player is in game.
        private bool inGame;
    
        // Player stats, kills, collected items etc.
        private int killCount;
        private int itemCount;
        private int secretCount;
        private int time;
        private int[] frags;

        public PlayerScores()
        {
            frags = new int[Player.MaxPlayerCount];
        }

        public bool InGame
        {
            get => inGame;
            set => inGame = value;
        }

        public int KillCount
        {
            get => killCount;
            set => killCount = value;
        }

        public int ItemCount
        {
            get => itemCount;
            set => itemCount = value;
        }

        public int SecretCount
        {
            get => secretCount;
            set => secretCount = value;
        }

        public int Time
        {
            get => time;
            set => time = value;
        }

        public int[] Frags
        {
            get => frags;
        }
    }
}
