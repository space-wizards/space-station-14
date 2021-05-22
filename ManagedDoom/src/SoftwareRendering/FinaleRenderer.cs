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
    public sealed class FinaleRenderer
    {
        private Wad wad;
        private FlatLookup flats;
        private SpriteLookup sprites;

        private DrawScreen screen;
        private int scale;

        private PatchCache cache;

        public FinaleRenderer(CommonResource resource, DrawScreen screen)
        {
            wad = resource.Wad;
            flats = resource.Flats;
            sprites = resource.Sprites;

            this.screen = screen;
            scale = screen.Width / 320;

            cache = new PatchCache(wad);
        }

        public void Render(Finale finale)
        {
            if (finale.Stage == 2)
            {
                RenderCast(finale);
                return;
            }

            if (finale.Stage == 0)
            {
                RenderTextScreen(finale);
            }
            else
            {
                switch (finale.Options.Episode)
                {
                    case 1:
                        DrawPatch("CREDIT", 0, 0);
                        break;

                    case 2:
                        DrawPatch("VICTORY2", 0, 0);
                        break;

                    case 3:
                        BunnyScroll(finale);
                        break;

                    case 4:
                        DrawPatch("ENDPIC", 0, 0);
                        break;
                }
            }
        }

        private void RenderTextScreen(Finale finale)
        {
            FillFlat(flats[finale.Flat]);

            // Draw some of the text onto the screen.
            var cx = 10 * scale;
            var cy = 17 * scale;
            var ch = 0;

            var count = (finale.Count - 10) / Finale.TextSpeed;
            if (count < 0)
            {
                count = 0;
            }

            for (; count > 0; count--)
            {
                if (ch == finale.Text.Length)
                {
                    break;
                }

                var c = finale.Text[ch++];

                if (c == '\n')
                {
                    cx = 10 * scale;
                    cy += 11 * scale;
                    continue;
                }

                screen.DrawChar(c, cx, cy, scale);

                cx += screen.MeasureChar(c, scale);
            }
        }

        private void BunnyScroll(Finale finale)
        {
            var scroll = 320 - finale.Scrolled;
            DrawPatch("PFUB2", scroll - 320, 0);
            DrawPatch("PFUB1", scroll, 0);

            if (finale.ShowTheEnd)
            {
                string patch = "END0";
                switch (finale.TheEndIndex)
                {
                    case 1:
                        patch = "END1";
                        break;
                    case 2:
                        patch = "END2";
                        break;
                    case 3:
                        patch = "END3";
                        break;
                    case 4:
                        patch = "END4";
                        break;
                    case 5:
                        patch = "END5";
                        break;
                    case 6:
                        patch = "END6";
                        break;
                }

                DrawPatch(
                    patch,
                    (320 - 13 * 8) / 2,
                    (240 - 8 * 8) / 2);
            }
        }

        private void FillFlat(Flat flat)
        {
            var src = flat.Data;
            var dst = screen.Data;
            var scale = screen.Width / 320;
            var xFrac = Fixed.One / scale - Fixed.Epsilon;
            var step = Fixed.One / scale;
            for (var x = 0; x < screen.Width; x++)
            {
                var yFrac = Fixed.One / scale - Fixed.Epsilon;
                var p = screen.Height * x;
                for (var y = 0; y < screen.Height; y++)
                {
                    var spotX = xFrac.ToIntFloor() & 0x3F;
                    var spotY = yFrac.ToIntFloor() & 0x3F;
                    dst[p] = src[(spotY << 6) + spotX];
                    yFrac += step;
                    p++;
                }
                xFrac += step;
            }
        }

        private void DrawPatch(string name, int x, int y)
        {
            var scale = screen.Width / 320;
            screen.DrawPatch(cache[name], scale * x, scale * y, scale);
        }

        private void RenderCast(Finale finale)
        {
            DrawPatch("BOSSBACK", 0, 0);

            var frame = finale.CastState.Frame & 0x7fff;
            var patch = sprites[finale.CastState.Sprite].Frames[frame].Patches[0];
            if (sprites[finale.CastState.Sprite].Frames[frame].Flip[0])
            {
                screen.DrawPatchFlip(
                    patch,
                    screen.Width / 2,
                    screen.Height - scale * 30,
                    scale);
            }
            else
            {
                screen.DrawPatch(
                    patch,
                    screen.Width / 2,
                    screen.Height - scale * 30,
                    scale);
            }

            var width = screen.MeasureText(finale.CastName, scale);
            screen.DrawText(
                finale.CastName,
                (screen.Width - width) / 2,
                screen.Height - scale * 13,
                scale);
        }
    }
}
