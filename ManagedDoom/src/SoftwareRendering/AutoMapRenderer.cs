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
    public sealed class AutoMapRenderer
    {
        private static readonly float pr = 8 * DoomInfo.MobjInfos[(int)MobjType.Player].Radius.ToFloat() / 7;

        // The vector graphics for the automap.
        // A line drawing of the player pointing right, starting from the middle.
        private static readonly float[] playerArrow = new float[]
        {
            -pr + pr / 8, 0, pr, 0, // -----
            pr, 0, pr - pr / 2, pr / 4, // ----->
            pr, 0, pr - pr / 2, -pr / 4,
            -pr + pr / 8, 0, -pr - pr / 8, pr / 4, // >---->
            -pr + pr / 8, 0, -pr - pr / 8, -pr / 4,
            -pr + 3 * pr / 8, 0, -pr + pr / 8, pr / 4, // >>--->
            -pr + 3 * pr / 8, 0, -pr + pr / 8, -pr / 4
        };

        private static readonly float tr = 16;

        private static readonly float[] thingTriangle = new float[]
        {
            -0.5F * tr, -0.7F * tr, tr, 0F,
            tr, 0F, -0.5F * tr, 0.7F * tr,
            -0.5F * tr, 0.7F * tr, -0.5F * tr, -0.7F * tr
        };

        // For use if I do walls with outsides / insides.
        private static readonly int reds = (256 - 5 * 16);
        private static readonly int redRange = 16;
        private static readonly int greens = (7 * 16);
        private static readonly int greenRange = 16;
        private static readonly int grays = (6 * 16);
        private static readonly int grayRange = 16;
        private static readonly int browns = (4 * 16);
        private static readonly int brownRange = 16;
        private static readonly int yellows = (256 - 32 + 7);
        private static readonly int yellowRange = 1;
        private static readonly int black = 0;
        private static readonly int white = (256 - 47);

        // Automap colors.
        private static readonly int background = black;
        private static readonly int wallColors = reds;
        private static readonly int wallRange = redRange;
        private static readonly int tsWallColors = grays;
        private static readonly int tsWallRange = grayRange;
        private static readonly int fdWallColors = browns;
        private static readonly int fdWallRange = brownRange;
        private static readonly int cdWallColors = yellows;
        private static readonly int cdWallRange = yellowRange;
        private static readonly int thingColors = greens;
        private static readonly int thingRange = greenRange;
        private static readonly int secretWallColors = wallColors;
        private static readonly int secretWallRange = wallRange;

        private static readonly int[] playerColors = new int[]
        {
            greens,
            grays,
            browns,
            reds
        };

        private DrawScreen screen;

        private int scale;
        private int amWidth;
        private int amHeight;
        private float ppu;

        private float minX;
        private float maxX;
        private float width;
        private float minY;
        private float maxY;
        private float height;

        private float actualViewX;
        private float actualViewY;
        private float zoom;

        private float renderViewX;
        private float renderViewY;

        private Patch[] markNumbers;

        public AutoMapRenderer(Wad wad, DrawScreen screen)
        {
            this.screen = screen;

            scale = screen.Width / 320;
            amWidth = screen.Width;
            amHeight = screen.Height - scale * StatusBarRenderer.Height;
            ppu = (float)scale / 16;

            markNumbers = new Patch[10];
            for (var i = 0; i < markNumbers.Length; i++)
            {
                markNumbers[i] = Patch.FromWad(wad, "AMMNUM" + i);
            }
        }

        public void Render(Player player)
        {
            screen.FillRect(0, 0, amWidth, amHeight, background);

            var world = player.Mobj.World;
            var am = world.AutoMap;

            minX = am.MinX.ToFloat();
            maxX = am.MaxX.ToFloat();
            width = maxX - minX;
            minY = am.MinY.ToFloat();
            maxY = am.MaxY.ToFloat();
            height = maxY - minY;

            actualViewX = am.ViewX.ToFloat();
            actualViewY = am.ViewY.ToFloat();
            zoom = am.Zoom.ToFloat();

            // This hack aligns the view point to an integer coordinate
            // so that line shake is reduced when the view point moves.
            renderViewX = MathF.Round(zoom * ppu * actualViewX) / (zoom * ppu);
            renderViewY = MathF.Round(zoom * ppu * actualViewY) / (zoom * ppu);

            foreach (var line in world.Map.Lines)
            {
                var v1 = ToScreenPos(line.Vertex1);
                var v2 = ToScreenPos(line.Vertex2);

                var cheating = am.State != AutoMapState.None;

                if (cheating || (line.Flags & LineFlags.Mapped) != 0)
                {
                    if ((line.Flags & LineFlags.DontDraw) != 0 && !cheating)
                    {
                        continue;
                    }

                    if (line.BackSector == null)
                    {
                        screen.DrawLine(v1.X, v1.Y, v2.X, v2.Y, wallColors);
                    }
                    else
                    {
                        if (line.Special == (LineSpecial)39)
                        {
                            // Teleporters.
                            screen.DrawLine(v1.X, v1.Y, v2.X, v2.Y, wallColors + wallRange / 2);
                        }
                        else if ((line.Flags & LineFlags.Secret) != 0)
                        {
                            // Secret door.
                            if (cheating)
                            {
                                screen.DrawLine(v1.X, v1.Y, v2.X, v2.Y, secretWallColors);
                            }
                            else
                            {
                                screen.DrawLine(v1.X, v1.Y, v2.X, v2.Y, wallColors);
                            }
                        }
                        else if (line.BackSector.FloorHeight != line.FrontSector.FloorHeight)
                        {
                            // Floor level change.
                            screen.DrawLine(v1.X, v1.Y, v2.X, v2.Y, fdWallColors);
                        }
                        else if (line.BackSector.CeilingHeight != line.FrontSector.CeilingHeight)
                        {
                            // Ceiling level change.
                            screen.DrawLine(v1.X, v1.Y, v2.X, v2.Y, cdWallColors);
                        }
                        else if (cheating)
                        {
                            screen.DrawLine(v1.X, v1.Y, v2.X, v2.Y, tsWallColors);
                        }
                    }
                }
                else if (player.Powers[(int)PowerType.AllMap] > 0)
                {
                    if ((line.Flags & LineFlags.DontDraw) == 0)
                    {
                        screen.DrawLine(v1.X, v1.Y, v2.X, v2.Y, grays + 3);
                    }
                }
            }

            for (var i = 0; i < am.Marks.Count; i++)
            {
                var pos = ToScreenPos(am.Marks[i]);
                screen.DrawPatch(
                    markNumbers[i],
                    (int)MathF.Round(pos.X),
                    (int)MathF.Round(pos.Y),
                    scale);
            }

            if (am.State == AutoMapState.AllThings)
            {
                DrawThings(world);
            }

            DrawPlayers(world);

            if (!am.Follow)
            {
                screen.DrawLine(
                    amWidth / 2 - 2 * scale, amHeight / 2,
                    amWidth / 2 + 2 * scale, amHeight / 2,
                    grays);

                screen.DrawLine(
                    amWidth / 2, amHeight / 2 - 2 * scale,
                    amWidth / 2, amHeight / 2 + 2 * scale,
                    grays);
            }

            screen.DrawText(
                world.Map.Title,
                0,
                amHeight - scale,
                scale);
        }

        private void DrawPlayers(World world)
        {
            var options = world.Options;
            var players = options.Players;
            var consolePlayer = world.ConsolePlayer;
            var am = world.AutoMap;

            if (!options.NetGame)
            {
                DrawCharacter(consolePlayer.Mobj, playerArrow, white);
                return;
            }

            for (var i = 0; i < Player.MaxPlayerCount; i++)
            {
                var player = players[i];
                if (options.Deathmatch != 0 && !options.DemoPlayback && player != consolePlayer)
                {
                    continue;
                }

                if (!player.InGame)
                {
                    continue;
                }

                int color;
                if (player.Powers[(int)PowerType.Invisibility] > 0)
                {
                    // Close to black.
                    color = 246;
                }
                else
                {
                    color = playerColors[i];
                }

                DrawCharacter(player.Mobj, playerArrow, color);
            }
        }

        private void DrawThings(World world)
        {
            foreach (var thinker in world.Thinkers)
            {
                var mobj = thinker as Mobj;
                if (mobj != null)
                {
                    DrawCharacter(mobj, thingTriangle, greens);
                }
            }
        }

        private void DrawCharacter(Mobj mobj, float[] data, int color)
        {
            var pos = ToScreenPos(mobj.X, mobj.Y);
            var sin = (float)Math.Sin(mobj.Angle.ToRadian());
            var cos = (float)Math.Cos(mobj.Angle.ToRadian());
            for (var i = 0; i < data.Length; i += 4)
            {
                var x1 = pos.X + zoom * ppu * (cos * data[i + 0] - sin * data[i + 1]);
                var y1 = pos.Y - zoom * ppu * (sin * data[i + 0] + cos * data[i + 1]);
                var x2 = pos.X + zoom * ppu * (cos * data[i + 2] - sin * data[i + 3]);
                var y2 = pos.Y - zoom * ppu * (sin * data[i + 2] + cos * data[i + 3]);
                screen.DrawLine(x1, y1, x2, y2, color);
            }
        }

        private DrawPos ToScreenPos(Fixed x, Fixed y)
        {
            var posX = zoom * ppu * (x.ToFloat() - renderViewX) + amWidth / 2;
            var posY = -zoom * ppu * (y.ToFloat() - renderViewY) + amHeight / 2;
            return new DrawPos(posX, posY);
        }

        private DrawPos ToScreenPos(Vertex v)
        {
            var posX = zoom * ppu * (v.X.ToFloat() - renderViewX) + amWidth / 2;
            var posY = -zoom * ppu * (v.Y.ToFloat() - renderViewY) + amHeight / 2;
            return new DrawPos(posX, posY);
        }



        private struct DrawPos
        {
            public float X;
            public float Y;

            public DrawPos(float x, float y)
            {
                X = x;
                Y = y;
            }
        }
    }
}
