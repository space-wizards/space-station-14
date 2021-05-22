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
    public class IntermissionInfo
    {
        // Episode number (0-2).
        private int episode;

        // If true, splash the secret level.
        private bool didSecret;

        // Previous and next levels, origin 0.
        private int lastLevel;
        private int nextLevel;

        private int maxKillCount;
        private int maxItemCount;
        private int maxSecretCount;
        private int totalFrags;

        // The par time.
        private int parTime;

        private PlayerScores[] players;

        public IntermissionInfo()
        {
            players = new PlayerScores[Player.MaxPlayerCount];
            for (var i = 0; i < Player.MaxPlayerCount; i++)
            {
                players[i] = new PlayerScores();
            }
        }

        public int Episode
        {
            get => episode;
            set => episode = value;
        }

        public bool DidSecret
        {
            get => didSecret;
            set => didSecret = value;
        }

        public int LastLevel
        {
            get => lastLevel;
            set => lastLevel = value;
        }

        public int NextLevel
        {
            get => nextLevel;
            set => nextLevel = value;
        }

        public int MaxKillCount
        {
            get => Math.Max(maxKillCount, 1);
            set => maxKillCount = value;
        }

        public int MaxItemCount
        {
            get => Math.Max(maxItemCount, 1);
            set => maxItemCount = value;
        }

        public int MaxSecretCount
        {
            get => Math.Max(maxSecretCount, 1);
            set => maxSecretCount = value;
        }

        public int TotalFrags
        {
            get => Math.Max(totalFrags, 1);
            set => totalFrags = value;
        }

        public int ParTime
        {
            get => parTime;
            set => parTime = value;
        }

        public PlayerScores[] Players
        {
            get => players;
        }
    }
}
