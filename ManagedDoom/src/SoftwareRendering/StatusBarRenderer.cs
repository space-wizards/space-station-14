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

namespace ManagedDoom.SoftwareRendering
{
    public sealed class StatusBarRenderer
    {
        public static readonly int Height = 32;

        // Ammo number pos.
        private static readonly int ammoWidth = 3;
        private static readonly int ammoX = 44;
        private static readonly int ammoY = 171;

        // Health number pos.
        private static readonly int healthX = 90;
        private static readonly int healthY = 171;

        // Weapon pos.
        private static readonly int armsX = 111;
        private static readonly int armsY = 172;
        private static readonly int armsBackgroundX = 104;
        private static readonly int armsBackgroundY = 168;
        private static readonly int armsSpaceX = 12;
        private static readonly int armsSpaceY = 10;

        // Frags pos.
        private static readonly int fragsWidth = 2;
        private static readonly int fragsX = 138;
        private static readonly int fragsY = 171;

        // Armor number pos.
        private static readonly int armorX = 221;
        private static readonly int armorY = 171;

        // Key icon positions.
        private static readonly int key0Width = 8;
        private static readonly int key0X = 239;
        private static readonly int key0Y = 171;
        private static readonly int key1Width = key0Width;
        private static readonly int key1X = 239;
        private static readonly int key1Y = 181;
        private static readonly int key2Width = key0Width;
        private static readonly int key2X = 239;
        private static readonly int key2Y = 191;

        // Ammunition counter.
        private static readonly int ammo0Width = 3;
        private static readonly int ammo0X = 288;
        private static readonly int ammo0Y = 173;
        private static readonly int ammo1Width = ammo0Width;
        private static readonly int ammo1X = 288;
        private static readonly int ammo1Y = 179;
        private static readonly int ammo2Width = ammo0Width;
        private static readonly int ammo2X = 288;
        private static readonly int ammo2Y = 191;
        private static readonly int ammo3Wdth = ammo0Width;
        private static readonly int ammo3X = 288;
        private static readonly int ammo3Y = 185;

        // Indicate maximum ammunition.
        // Only needed because backpack exists.
        private static readonly int maxAmmo0Width = 3;
        private static readonly int maxAmmo0X = 314;
        private static readonly int maxAmmo0Y = 173;
        private static readonly int maxAmmo1Width = maxAmmo0Width;
        private static readonly int maxAmmo1X = 314;
        private static readonly int maxAmmo1Y = 179;
        private static readonly int maxAmmo2Width = maxAmmo0Width;
        private static readonly int maxAmmo2X = 314;
        private static readonly int maxAmmo2Y = 191;
        private static readonly int maxAmmo3Width = maxAmmo0Width;
        private static readonly int maxAmmo3X = 314;
        private static readonly int maxAmmo3Y = 185;

        private static readonly int faceX = 143;
        private static readonly int faceY = 168;
        private static readonly int faceBackgroundX = 143;
        private static readonly int faceBackgroundY = 169;

        private DrawScreen screen;

        private Patches patches;

        private int scale;

        private NumberWidget ready;
        private PercentWidget health;
        private PercentWidget armor;

        private NumberWidget[] ammo;
        private NumberWidget[] maxAmmo;

        private MultIconWidget[] weapons;

        private NumberWidget frags;

        private MultIconWidget[] keys;

        public StatusBarRenderer(Wad wad, DrawScreen screen)
        {
            this.screen = screen;

            patches = new Patches(wad);

            scale = screen.Width / 320;

            ready = new NumberWidget();
            ready.Patches = patches.TallNumbers;
            ready.Width = ammoWidth;
            ready.X = ammoX;
            ready.Y = ammoY;

            health = new PercentWidget();
            health.NumberWidget.Patches = patches.TallNumbers;
            health.NumberWidget.Width = 3;
            health.NumberWidget.X = healthX;
            health.NumberWidget.Y = healthY;
            health.Patch = patches.TallPercent;

            armor = new PercentWidget();
            armor.NumberWidget.Patches = patches.TallNumbers;
            armor.NumberWidget.Width = 3;
            armor.NumberWidget.X = armorX;
            armor.NumberWidget.Y = armorY;
            armor.Patch = patches.TallPercent;

            ammo = new NumberWidget[(int)AmmoType.Count];
            ammo[0] = new NumberWidget();
            ammo[0].Patches = patches.ShortNumbers;
            ammo[0].Width = ammo0Width;
            ammo[0].X = ammo0X;
            ammo[0].Y = ammo0Y;
            ammo[1] = new NumberWidget();
            ammo[1].Patches = patches.ShortNumbers;
            ammo[1].Width = ammo1Width;
            ammo[1].X = ammo1X;
            ammo[1].Y = ammo1Y;
            ammo[2] = new NumberWidget();
            ammo[2].Patches = patches.ShortNumbers;
            ammo[2].Width = ammo2Width;
            ammo[2].X = ammo2X;
            ammo[2].Y = ammo2Y;
            ammo[3] = new NumberWidget();
            ammo[3].Patches = patches.ShortNumbers;
            ammo[3].Width = ammo3Wdth;
            ammo[3].X = ammo3X;
            ammo[3].Y = ammo3Y;

            maxAmmo = new NumberWidget[(int)AmmoType.Count];
            maxAmmo[0] = new NumberWidget();
            maxAmmo[0].Patches = patches.ShortNumbers;
            maxAmmo[0].Width = maxAmmo0Width;
            maxAmmo[0].X = maxAmmo0X;
            maxAmmo[0].Y = maxAmmo0Y;
            maxAmmo[1] = new NumberWidget();
            maxAmmo[1].Patches = patches.ShortNumbers;
            maxAmmo[1].Width = maxAmmo1Width;
            maxAmmo[1].X = maxAmmo1X;
            maxAmmo[1].Y = maxAmmo1Y;
            maxAmmo[2] = new NumberWidget();
            maxAmmo[2].Patches = patches.ShortNumbers;
            maxAmmo[2].Width = maxAmmo2Width;
            maxAmmo[2].X = maxAmmo2X;
            maxAmmo[2].Y = maxAmmo2Y;
            maxAmmo[3] = new NumberWidget();
            maxAmmo[3].Patches = patches.ShortNumbers;
            maxAmmo[3].Width = maxAmmo3Width;
            maxAmmo[3].X = maxAmmo3X;
            maxAmmo[3].Y = maxAmmo3Y;

            weapons = new MultIconWidget[6];
            for (var i = 0; i < weapons.Length; i++)
            {
                weapons[i] = new MultIconWidget();
                weapons[i].X = armsX + (i % 3) * armsSpaceX;
                weapons[i].Y = armsY + (i / 3) * armsSpaceY;
                weapons[i].Patches = patches.Arms[i];
            }

            frags = new NumberWidget();
            frags.Patches = patches.TallNumbers;
            frags.Width = fragsWidth;
            frags.X = fragsX;
            frags.Y = fragsY;

            keys = new MultIconWidget[3];
            keys[0] = new MultIconWidget();
            keys[0].X = key0X;
            keys[0].Y = key0Y;
            keys[0].Patches = patches.Keys;
            keys[1] = new MultIconWidget();
            keys[1].X = key1X;
            keys[1].Y = key1Y;
            keys[1].Patches = patches.Keys;
            keys[2] = new MultIconWidget();
            keys[2].X = key2X;
            keys[2].Y = key2Y;
            keys[2].Patches = patches.Keys;
        }

        public void Render(Player player, bool drawBackground)
        {
            if (drawBackground)
            {
                screen.DrawPatch(
                    patches.Background,
                    0,
                    scale * (200 - Height),
                    scale);
            }

            if (DoomInfo.WeaponInfos[(int)player.ReadyWeapon].Ammo != AmmoType.NoAmmo)
            {
                var num = player.Ammo[(int)DoomInfo.WeaponInfos[(int)player.ReadyWeapon].Ammo];
                DrawNumber(ready, num);
            }

            DrawPercent(health, player.Health);
            DrawPercent(armor, player.ArmorPoints);

            for (var i = 0; i < (int)AmmoType.Count; i++)
            {
                DrawNumber(ammo[i], player.Ammo[i]);
                DrawNumber(maxAmmo[i], player.MaxAmmo[i]);
            }

            if (player.Mobj.World.Options.Deathmatch == 0)
            {
                if (drawBackground)
                {
                    screen.DrawPatch(
                        patches.ArmsBackground,
                        scale * armsBackgroundX,
                        scale * armsBackgroundY,
                        scale);
                }

                for (var i = 0; i < weapons.Length; i++)
                {
                    DrawMultIcon(weapons[i], player.WeaponOwned[i + 1] ? 1 : 0);
                }
            }
            else
            {
                var sum = 0;
                for (var i = 0; i < player.Frags.Length; i++)
                {
                    sum += player.Frags[i];
                }
                DrawNumber(frags, sum);
            }

            if (drawBackground)
            {
                if (player.Mobj.World.Options.NetGame)
                {
                    screen.DrawPatch(
                        patches.FaceBackground[player.Number],
                        scale * faceBackgroundX,
                        scale * faceBackgroundY,
                        scale);
                }

                screen.DrawPatch(
                    patches.Faces[player.Mobj.World.StatusBar.FaceIndex],
                    scale * faceX,
                    scale * faceY,
                    scale);
            }

            for (var i = 0; i < 3; i++)
            {
                if (player.Cards[i + 3])
                {
                    DrawMultIcon(keys[i], i + 3);
                }
                else if (player.Cards[i])
                {
                    DrawMultIcon(keys[i], i);
                }
            }
        }

        private void DrawNumber(NumberWidget widget, int num)
        {
            var digits = widget.Width;

            var w = widget.Patches[0].Width;
            var h = widget.Patches[0].Height;
            var x = widget.X;

            var neg = num < 0;

            if (neg)
            {
                if (digits == 2 && num < -9)
                {
                    num = -9;
                }
                else if (digits == 3 && num < -99)
                {
                    num = -99;
                }

                num = -num;
            }

            x = widget.X - digits * w;

            if (num == 1994)
            {
                return;
            }

            x = widget.X;

            // In the special case of 0, you draw 0.
            if (num == 0)
            {
                screen.DrawPatch(
                    widget.Patches[0],
                    scale * (x - w),
                    scale * widget.Y,
                    scale);
            }

            // Draw the new number.
            while (num != 0 && digits-- != 0)
            {
                x -= w;

                screen.DrawPatch(
                    widget.Patches[num % 10],
                    scale * x,
                    scale * widget.Y,
                    scale);

                num /= 10;
            }

            // Draw a minus sign if necessary.
            if (neg)
            {
                screen.DrawPatch(
                    patches.TallMinus,
                    scale * (x - 8),
                    scale * widget.Y,
                    scale);
            }
        }

        private void DrawPercent(PercentWidget per, int value)
        {
            screen.DrawPatch(
                per.Patch,
                scale * per.NumberWidget.X,
                scale * per.NumberWidget.Y,
                scale);

            DrawNumber(per.NumberWidget, value);
        }

        private void DrawMultIcon(MultIconWidget mi, int value)
        {
            screen.DrawPatch(
                mi.Patches[value],
                scale * mi.X,
                scale * mi.Y,
                scale);
        }



        private class NumberWidget
        {
            public int X;
            public int Y;
            public int Width;
            public Patch[] Patches;
        }

        private class PercentWidget
        {
            public NumberWidget NumberWidget = new NumberWidget();
            public Patch Patch;
        }

        private class MultIconWidget
        {
            public int X;
            public int Y;
            public Patch[] Patches;
        }

        private class Patches
        {
            public Patch Background;
            public Patch[] TallNumbers;
            public Patch[] ShortNumbers;
            public Patch TallMinus;
            public Patch TallPercent;
            public Patch[] Keys;
            public Patch ArmsBackground;
            public Patch[][] Arms;
            public Patch[] FaceBackground;
            public Patch[] Faces;

            public Patches(Wad wad)
            {
                Background = Patch.FromWad(wad, "STBAR");

                TallNumbers = new Patch[10];
                ShortNumbers = new Patch[10];
                for (var i = 0; i < 10; i++)
                {
                    TallNumbers[i] = Patch.FromWad(wad, "STTNUM" + i);
                    ShortNumbers[i] = Patch.FromWad(wad, "STYSNUM" + i);
                }
                TallMinus = Patch.FromWad(wad, "STTMINUS");
                TallPercent = Patch.FromWad(wad, "STTPRCNT");

                Keys = new Patch[(int)CardType.Count];
                for (var i = 0; i < Keys.Length; i++)
                {
                    Keys[i] = Patch.FromWad(wad, "STKEYS" + i);
                }

                ArmsBackground = Patch.FromWad(wad, "STARMS");
                Arms = new Patch[6][];
                for (var i = 0; i < 6; i++)
                {
                    var num = i + 2;
                    Arms[i] = new Patch[2];
                    Arms[i][0] = Patch.FromWad(wad, "STGNUM" + num);
                    Arms[i][1] = ShortNumbers[num];
                }

                FaceBackground = new Patch[Player.MaxPlayerCount];
                for (var i = 0; i < FaceBackground.Length; i++)
                {
                    FaceBackground[i] = Patch.FromWad(wad, "STFB" + i);
                }
                Faces = new Patch[StatusBar.Face.FaceCount];
                var faceCount = 0;
                for (var i = 0; i < StatusBar.Face.PainFaceCount; i++)
                {
                    for (var j = 0; j < StatusBar.Face.StraightFaceCount; j++)
                    {
                        Faces[faceCount++] = Patch.FromWad(wad, "STFST" + i + j);
                    }
                    Faces[faceCount++] = Patch.FromWad(wad, "STFTR" + i + "0");
                    Faces[faceCount++] = Patch.FromWad(wad, "STFTL" + i + "0");
                    Faces[faceCount++] = Patch.FromWad(wad, "STFOUCH" + i);
                    Faces[faceCount++] = Patch.FromWad(wad, "STFEVL" + i);
                    Faces[faceCount++] = Patch.FromWad(wad, "STFKILL" + i);
                }
                Faces[faceCount++] = Patch.FromWad(wad, "STFGOD0");
                Faces[faceCount++] = Patch.FromWad(wad, "STFDEAD0");
            }
        }
    }
}
