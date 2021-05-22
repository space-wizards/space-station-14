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

namespace ManagedDoom.Audio
{
    public interface ISound
    {
        public void SetListener(Mobj listener);
        public void Update();
        public void StartSound(Sfx sfx);
        public void StartSound(Mobj mobj, Sfx sfx, SfxType type);
        public void StartSound(Mobj mobj, Sfx sfx, SfxType type, int volume);
        public void StopSound(Mobj mobj);
        public void Reset();
        public void Pause();
        public void Resume();

        public int MaxVolume { get; }
        public int Volume { get; set; }
    }
}
