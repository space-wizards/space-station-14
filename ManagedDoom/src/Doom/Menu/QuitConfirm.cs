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
    public sealed class QuitConfirm : MenuDef
    {
        private static readonly Sfx[] doomQuitSoundList = new Sfx[]
        {
            Sfx.PLDETH,
            Sfx.DMPAIN,
            Sfx.POPAIN,
            Sfx.SLOP,
            Sfx.TELEPT,
            Sfx.POSIT1,
            Sfx.POSIT3,
            Sfx.SGTATK
        };

        private static readonly Sfx[] doom2QuitSoundList = new Sfx[]
        {
            Sfx.VILACT,
            Sfx.GETPOW,
            Sfx.BOSCUB,
            Sfx.SLOP,
            Sfx.SKESWG,
            Sfx.KNTDTH,
            Sfx.BSPACT,
            Sfx.SGTATK
        };

        private DoomApplication app;
        private DoomRandom random;
        private string[] text;

        private int endCount;

        public QuitConfirm(DoomMenu menu, DoomApplication app) : base(menu)
        {
            this.app = app;
            random = new DoomRandom(DateTime.Now.Millisecond);
            endCount = -1;
        }

        public override void Open()
        {
            IReadOnlyList<DoomString> list;
            if (app.Options.GameMode == GameMode.Commercial)
            {
                if (app.Options.MissionPack == MissionPack.Doom2)
                {
                    list = DoomInfo.QuitMessages.Doom2;
                }
                else
                {
                    list = DoomInfo.QuitMessages.FinalDoom;
                }
            }
            else
            {
                list = DoomInfo.QuitMessages.Doom;
            }

            text = (list[random.Next() % list.Count] + "\n\n" + DoomInfo.Strings.PRESSYN).Split('\n');
        }

        public override bool DoEvent(DoomEvent e)
        {
            if (endCount != -1)
            {
                return true;
            }

            if (e.Type != EventType.KeyDown)
            {
                return true;
            }

            if (e.Key == DoomKey.Y ||
                e.Key == DoomKey.Enter ||
                e.Key == DoomKey.Space)
            {
                endCount = 0;

                Sfx sfx;
                if (Menu.Options.GameMode == GameMode.Commercial)
                {
                    sfx = doom2QuitSoundList[random.Next() % doom2QuitSoundList.Length];
                }
                else
                {
                    sfx = doomQuitSoundList[random.Next() % doomQuitSoundList.Length];
                }
                Menu.StartSound(sfx);
            }

            if (e.Key == DoomKey.N ||
                e.Key == DoomKey.Escape)
            {
                Menu.Close();
                Menu.StartSound(Sfx.SWTCHX);
            }

            return true;
        }

        public override void Update()
        {
            if (endCount != -1)
            {
                endCount++;
            }

            if (endCount == 50)
            {
                app.Quit();
            }
        }

        public IReadOnlyList<string> Text => text;
    }
}
