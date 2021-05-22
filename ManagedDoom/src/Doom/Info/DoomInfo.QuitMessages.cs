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
    public static partial class DoomInfo
    {
        public static class QuitMessages
        {
            public static readonly IReadOnlyList<DoomString> Doom = new DoomString[]
            {
                Strings.QUITMSG,
                new DoomString("please don't leave, there's more\ndemons to toast!"),
                new DoomString("let's beat it -- this is turning\ninto a bloodbath!"),
                new DoomString("i wouldn't leave if i were you.\ndos is much worse."),
                new DoomString("you're trying to say you like dos\nbetter than me, right?"),
                new DoomString("don't leave yet -- there's a\ndemon around that corner!"),
                new DoomString("ya know, next time you come in here\ni'm gonna toast ya."),
                new DoomString("go ahead and leave. see if i care.")
            };

            public static readonly IReadOnlyList<DoomString> Doom2 = new DoomString[]
            {
                new DoomString("you want to quit?\nthen, thou hast lost an eighth!"),
                new DoomString("don't go now, there's a \ndimensional shambler waiting\nat the dos prompt!"),
                new DoomString("get outta here and go back\nto your boring programs."),
                new DoomString("if i were your boss, i'd \n deathmatch ya in a minute!"),
                new DoomString("look, bud. you leave now\nand you forfeit your body count!"),
                new DoomString("just leave. when you come\nback, i'll be waiting with a bat."),
                new DoomString("you're lucky i don't smack\nyou for thinking about leaving.")
            };

            public static readonly IReadOnlyList<DoomString> FinalDoom = new DoomString[]
            {
                new DoomString("fuck you, pussy!\nget the fuck out!"),
                new DoomString("you quit and i'll jizz\nin your cystholes!"),
                new DoomString("if you leave, i'll make\nthe lord drink my jizz."),
                new DoomString("hey, ron! can we say\n'fuck' in the game?"),
                new DoomString("i'd leave: this is just\nmore monsters and levels.\nwhat a load."),
                new DoomString("suck it down, asshole!\nyou're a fucking wimp!"),
                new DoomString("don't quit now! we're \nstill spending your money!")
            };
        }
    }
}
