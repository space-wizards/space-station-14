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
using System.Linq;

namespace ManagedDoom
{
    public sealed class Cheat
    {
        private static Tuple<string, Action<Cheat, string>>[] list = new Tuple<string, Action<Cheat, string>>[]
        {
            Tuple.Create("idfa", (Action<Cheat, string>)((cheat, typed) => cheat.FullAmmo())),
            Tuple.Create("idkfa", (Action<Cheat, string>)((cheat, typed) => cheat.FullAmmoAndKeys())),
            Tuple.Create("iddqd", (Action<Cheat, string>)((cheat, typed) => cheat.GodMode())),
            Tuple.Create("idclip", (Action<Cheat, string>)((cheat, typed) => cheat.NoClip())),
            Tuple.Create("idspispopd", (Action<Cheat, string>)((cheat, typed) => cheat.NoClip())),
            Tuple.Create("iddt", (Action<Cheat, string>)((cheat, typed) => cheat.FullMap())),
            Tuple.Create("idbehold", (Action<Cheat, string>)((cheat, typed) => cheat.ShowPowerUpList())),
            Tuple.Create("idbehold?", (Action<Cheat, string>)((cheat, typed) => cheat.DoPowerUp(typed))),
            Tuple.Create("idchoppers", (Action<Cheat, string>)((cheat, typed) => cheat.GiveChainsaw())),
            Tuple.Create("tntem", (Action<Cheat, string>)((cheat, typed) => cheat.KillMonsters())),
            Tuple.Create("killem", (Action<Cheat, string>)((cheat, typed) => cheat.KillMonsters())),
            Tuple.Create("fhhall", (Action<Cheat, string>)((cheat, typed) => cheat.KillMonsters())),
            Tuple.Create("idclev??", (Action<Cheat, string>)((cheat, typed) => cheat.ChangeLevel(typed))),
            Tuple.Create("idmus??", (Action<Cheat, string>)((cheat, typed) => cheat.ChangeMusic(typed)))
        };

        private static readonly int maxLength = list.Max(tuple => tuple.Item1.Length);

        private World world;

        private char[] buffer;
        private int p;

        public Cheat(World world)
        {
            this.world = world;

            buffer = new char[maxLength];
            p = 0;
        }

        public bool DoEvent(DoomEvent e)
        {
            if (e.Type == EventType.KeyDown)
            {
                buffer[p] = e.Key.GetChar();

                p = (p + 1) % buffer.Length;

                CheckBuffer();
            }

            return true;
        }

        private void CheckBuffer()
        {
            for (var i = 0; i < list.Length; i++)
            {
                var code = list[i].Item1;
                var q = p;
                int j;
                for (j = 0; j < code.Length; j++)
                {
                    q--;
                    if (q == -1)
                    {
                        q = buffer.Length - 1;
                    }
                    var ch = code[code.Length - j - 1];
                    if (buffer[q] != ch && ch != '?')
                    {
                        break;
                    }
                }

                if (j == code.Length)
                {
                    var typed = new char[code.Length];
                    var k = code.Length;
                    q = p;
                    for (j = 0; j < code.Length; j++)
                    {
                        k--;
                        q--;
                        if (q == -1)
                        {
                            q = buffer.Length - 1;
                        }
                        typed[k] = buffer[q];
                    }
                    list[i].Item2(this, new string(typed));
                }
            }
        }

        private void GiveWeapons()
        {
            var player = world.ConsolePlayer;
            if (world.Options.GameMode == GameMode.Commercial)
            {
                for (var i = 0; i < (int)WeaponType.Count; i++)
                {
                    player.WeaponOwned[i] = true;
                }
            }
            else
            {
                for (var i = 0; i <= (int)WeaponType.Missile; i++)
                {
                    player.WeaponOwned[i] = true;
                }
                player.WeaponOwned[(int)WeaponType.Chainsaw] = true;
                if (world.Options.GameMode != GameMode.Shareware)
                {
                    player.WeaponOwned[(int)WeaponType.Plasma] = true;
                    player.WeaponOwned[(int)WeaponType.Bfg] = true;
                }
            }

            player.Backpack = true;
            for (var i = 0; i < (int)AmmoType.Count; i++)
            {
                player.MaxAmmo[i] = 2 * DoomInfo.AmmoInfos.Max[i];
                player.Ammo[i] = 2 * DoomInfo.AmmoInfos.Max[i];
            }
        }

        private void FullAmmo()
        {
            GiveWeapons();
            var player = world.ConsolePlayer;
            player.ArmorType = DoomInfo.DeHackEdConst.IdfaArmorClass;
            player.ArmorPoints = DoomInfo.DeHackEdConst.IdfaArmor;
            player.SendMessage(DoomInfo.Strings.STSTR_FAADDED);
        }

        private void FullAmmoAndKeys()
        {
            GiveWeapons();
            var player = world.ConsolePlayer;
            player.ArmorType = DoomInfo.DeHackEdConst.IdkfaArmorClass;
            player.ArmorPoints = DoomInfo.DeHackEdConst.IdkfaArmor;
            for (var i = 0; i < (int)CardType.Count; i++)
            {
                player.Cards[i] = true;
            }
            player.SendMessage(DoomInfo.Strings.STSTR_KFAADDED);
        }

        private void GodMode()
        {
            var player = world.ConsolePlayer;
            if ((player.Cheats & CheatFlags.GodMode) != 0)
            {
                player.Cheats &= ~CheatFlags.GodMode;
                player.SendMessage(DoomInfo.Strings.STSTR_DQDOFF);
            }
            else
            {
                player.Cheats |= CheatFlags.GodMode;
                player.Health = Math.Max(DoomInfo.DeHackEdConst.GodModeHealth, player.Health);
                player.Mobj.Health = player.Health;
                player.SendMessage(DoomInfo.Strings.STSTR_DQDON);
            }
        }

        private void NoClip()
        {
            var player = world.ConsolePlayer;
            if ((player.Cheats & CheatFlags.NoClip) != 0)
            {
                player.Cheats &= ~CheatFlags.NoClip;
                player.SendMessage(DoomInfo.Strings.STSTR_NCOFF);
            }
            else
            {
                player.Cheats |= CheatFlags.NoClip;
                player.SendMessage(DoomInfo.Strings.STSTR_NCON);
            }
        }

        private void FullMap()
        {
            world.AutoMap.ToggleCheat();
        }

        private void ShowPowerUpList()
        {
            var player = world.ConsolePlayer;
            player.SendMessage(DoomInfo.Strings.STSTR_BEHOLD);
        }

        private void DoPowerUp(string typed)
        {
            switch (typed.Last())
            {
                case 'v':
                    ToggleInvulnerability();
                    break;
                case 's':
                    ToggleStrength();
                    break;
                case 'i':
                    ToggleInvisibility();
                    break;
                case 'r':
                    ToggleIronFeet();
                    break;
                case 'a':
                    ToggleAllMap();
                    break;
                case 'l':
                    ToggleInfrared();
                    break;
            }
        }

        private void ToggleInvulnerability()
        {
            var player = world.ConsolePlayer;
            if (player.Powers[(int)PowerType.Invulnerability] > 0)
            {
                player.Powers[(int)PowerType.Invulnerability] = 0;
            }
            else
            {
                player.Powers[(int)PowerType.Invulnerability] = DoomInfo.PowerDuration.Invulnerability;
            }
            player.SendMessage(DoomInfo.Strings.STSTR_BEHOLDX);
        }

        private void ToggleStrength()
        {
            var player = world.ConsolePlayer;
            if (player.Powers[(int)PowerType.Strength] != 0)
            {
                player.Powers[(int)PowerType.Strength] = 0;
            }
            else
            {
                player.Powers[(int)PowerType.Strength] = 1;
            }
            player.SendMessage(DoomInfo.Strings.STSTR_BEHOLDX);
        }

        private void ToggleInvisibility()
        {
            var player = world.ConsolePlayer;
            if (player.Powers[(int)PowerType.Invisibility] > 0)
            {
                player.Powers[(int)PowerType.Invisibility] = 0;
                player.Mobj.Flags &= ~MobjFlags.Shadow;
            }
            else
            {
                player.Powers[(int)PowerType.Invisibility] = DoomInfo.PowerDuration.Invisibility;
                player.Mobj.Flags |= MobjFlags.Shadow;
            }
            player.SendMessage(DoomInfo.Strings.STSTR_BEHOLDX);
        }

        private void ToggleIronFeet()
        {
            var player = world.ConsolePlayer;
            if (player.Powers[(int)PowerType.IronFeet] > 0)
            {
                player.Powers[(int)PowerType.IronFeet] = 0;
            }
            else
            {
                player.Powers[(int)PowerType.IronFeet] = DoomInfo.PowerDuration.IronFeet;
            }
            player.SendMessage(DoomInfo.Strings.STSTR_BEHOLDX);
        }

        private void ToggleAllMap()
        {
            var player = world.ConsolePlayer;
            if (player.Powers[(int)PowerType.AllMap] != 0)
            {
                player.Powers[(int)PowerType.AllMap] = 0;
            }
            else
            {
                player.Powers[(int)PowerType.AllMap] = 1;
            }
            player.SendMessage(DoomInfo.Strings.STSTR_BEHOLDX);
        }

        private void ToggleInfrared()
        {
            var player = world.ConsolePlayer;
            if (player.Powers[(int)PowerType.Infrared] > 0)
            {
                player.Powers[(int)PowerType.Infrared] = 0;
            }
            else
            {
                player.Powers[(int)PowerType.Infrared] = DoomInfo.PowerDuration.Infrared;
            }
            player.SendMessage(DoomInfo.Strings.STSTR_BEHOLDX);
        }

        private void GiveChainsaw()
        {
            var player = world.ConsolePlayer;
            player.WeaponOwned[(int)WeaponType.Chainsaw] = true;
            player.SendMessage(DoomInfo.Strings.STSTR_CHOPPERS);
        }

        private void KillMonsters()
        {
            var player = world.ConsolePlayer;
            var count = 0;
            foreach (var thinker in world.Thinkers)
            {
                var mobj = thinker as Mobj;
                if (mobj != null &&
                    mobj.Player == null &&
                    ((mobj.Flags & MobjFlags.CountKill) != 0 || mobj.Type == MobjType.Skull) &&
                    mobj.Health > 0)
                {
                    world.ThingInteraction.DamageMobj(mobj, null, player.Mobj, 10000);
                    count++;
                }
            }
            player.SendMessage(count + " monsters killed");
        }

        private void ChangeLevel(string typed)
        {
            if (world.Options.GameMode == GameMode.Commercial)
            {
                int map;
                if (!int.TryParse(typed.Substring(typed.Length - 2, 2), out map))
                {
                    return;
                }
                var skill = world.Options.Skill;
                world.Game.DeferedInitNew(skill, 1, map);
            }
            else
            {
                int episode;
                if (!int.TryParse(typed.Substring(typed.Length - 2, 1), out episode))
                {
                    return;
                }
                int map;
                if (!int.TryParse(typed.Substring(typed.Length - 1, 1), out map))
                {
                    return;
                }
                var skill = world.Options.Skill;
                world.Game.DeferedInitNew(skill, episode, map);
            }
        }

        private void ChangeMusic(string typed)
        {
            var options = new GameOptions();
            options.GameMode = world.Options.GameMode;
            if (world.Options.GameMode == GameMode.Commercial)
            {
                int map;
                if (!int.TryParse(typed.Substring(typed.Length - 2, 2), out map))
                {
                    return;
                }
                options.Map = map;
            }
            else
            {
                int episode;
                if (!int.TryParse(typed.Substring(typed.Length - 2, 1), out episode))
                {
                    return;
                }
                int map;
                if (!int.TryParse(typed.Substring(typed.Length - 1, 1), out map))
                {
                    return;
                }
                options.Episode = episode;
                options.Map = map;
            }
            world.Options.Music.StartMusic(Map.GetMapBgm(options), true);
            world.ConsolePlayer.SendMessage(DoomInfo.Strings.STSTR_MUS);
        }
    }
}
