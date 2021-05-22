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
    public sealed class AutoMap
    {
        private World world;

        private Fixed minX;
        private Fixed maxX;
        private Fixed minY;
        private Fixed maxY;

        private Fixed viewX;
        private Fixed viewY;

        private bool visible;
        private AutoMapState state;

        private Fixed zoom;
        private bool follow;

        private bool zoomIn;
        private bool zoomOut;

        private bool left;
        private bool right;
        private bool up;
        private bool down;

        private List<Vertex> marks;
        private int nextMarkNumber;

        public AutoMap(World world)
        {
            this.world = world;

            minX = Fixed.MaxValue;
            maxX = Fixed.MinValue;
            minY = Fixed.MaxValue;
            maxY = Fixed.MinValue;
            foreach (var vertex in world.Map.Vertices)
            {
                if (vertex.X < minX)
                {
                    minX = vertex.X;
                }

                if (vertex.X > maxX)
                {
                    maxX = vertex.X;
                }

                if (vertex.Y < minY)
                {
                    minY = vertex.Y;
                }

                if (vertex.Y > maxY)
                {
                    maxY = vertex.Y;
                }
            }

            viewX = minX + (maxX - minX) / 2;
            viewY = minY + (maxY - minY) / 2;

            visible = false;
            state = AutoMapState.None;

            zoom = Fixed.One;
            follow = true;

            zoomIn = false;
            zoomOut = false;
            left = false;
            right = false;
            up = false;
            down = false;

            marks = new List<Vertex>();
            nextMarkNumber = 0;
        }

        public void Update()
        {
            if (zoomIn)
            {
                zoom += zoom / 16;
            }

            if (zoomOut)
            {
                zoom -= zoom / 16;
            }

            if (zoom < Fixed.One / 2)
            {
                zoom = Fixed.One / 2;
            }
            else if (zoom > Fixed.One * 32)
            {
                zoom = Fixed.One * 32;
            }

            if (left)
            {
                viewX -= 64 / zoom;
            }

            if (right)
            {
                viewX += 64 / zoom;
            }

            if (up)
            {
                viewY += 64 / zoom;
            }

            if (down)
            {
                viewY -= 64 / zoom;
            }

            if (viewX < minX)
            {
                viewX = minX;
            }
            else if (viewX > maxX)
            {
                viewX = maxX;
            }

            if (viewY < minY)
            {
                viewY = minY;
            }
            else if (viewY > maxY)
            {
                viewY = maxY;
            }

            if (follow)
            {
                var player = world.ConsolePlayer.Mobj;
                viewX = player.X;
                viewY = player.Y;
            }
        }

        public bool DoEvent(DoomEvent e)
        {
            if (e.Key == DoomKey.Add || e.Key == DoomKey.Quote)
            {
                if (e.Type == EventType.KeyDown)
                {
                    zoomIn = true;
                }
                else if (e.Type == EventType.KeyUp)
                {
                    zoomIn = false;
                }

                return true;
            }
            else if (e.Key == DoomKey.Subtract || e.Key == DoomKey.Hyphen)
            {
                if (e.Type == EventType.KeyDown)
                {
                    zoomOut = true;
                }
                else if (e.Type == EventType.KeyUp)
                {
                    zoomOut = false;
                }

                return true;
            }
            else if (e.Key == DoomKey.Left)
            {
                if (e.Type == EventType.KeyDown)
                {
                    left = true;
                }
                else if (e.Type == EventType.KeyUp)
                {
                    left = false;
                }

                return true;
            }
            else if (e.Key == DoomKey.Right)
            {
                if (e.Type == EventType.KeyDown)
                {
                    right = true;
                }
                else if (e.Type == EventType.KeyUp)
                {
                    right = false;
                }

                return true;
            }
            else if (e.Key == DoomKey.Up)
            {
                if (e.Type == EventType.KeyDown)
                {
                    up = true;
                }
                else if (e.Type == EventType.KeyUp)
                {
                    up = false;
                }

                return true;
            }
            else if (e.Key == DoomKey.Down)
            {
                if (e.Type == EventType.KeyDown)
                {
                    down = true;
                }
                else if (e.Type == EventType.KeyUp)
                {
                    down = false;
                }

                return true;
            }
            else if (e.Key == DoomKey.F)
            {
                if (e.Type == EventType.KeyDown)
                {
                    follow = !follow;
                    if (follow)
                    {
                        world.ConsolePlayer.SendMessage(DoomInfo.Strings.AMSTR_FOLLOWON);
                    }
                    else
                    {
                        world.ConsolePlayer.SendMessage(DoomInfo.Strings.AMSTR_FOLLOWOFF);
                    }
                    return true;
                }
            }
            else if (e.Key == DoomKey.M)
            {
                if (e.Type == EventType.KeyDown)
                {
                    if (marks.Count < 10)
                    {
                        marks.Add(new Vertex(viewX, viewY));
                    }
                    else
                    {
                        marks[nextMarkNumber] = new Vertex(viewX, viewY);
                    }
                    nextMarkNumber++;
                    if (nextMarkNumber == 10)
                    {
                        nextMarkNumber = 0;
                    }
                    world.ConsolePlayer.SendMessage(DoomInfo.Strings.AMSTR_MARKEDSPOT);
                    return true;
                }
            }
            else if (e.Key == DoomKey.C)
            {
                if (e.Type == EventType.KeyDown)
                {
                    marks.Clear();
                    nextMarkNumber = 0;
                    world.ConsolePlayer.SendMessage(DoomInfo.Strings.AMSTR_MARKSCLEARED);
                    return true;
                }
            }

            return false;
        }

        public void Open()
        {
            visible = true;
        }

        public void Close()
        {
            visible = false;
            zoomIn = false;
            zoomOut = false;
            left = false;
            right = false;
            up = false;
            down = false;
        }

        public void ToggleCheat()
        {
            state++;
            if ((int)state == 3)
            {
                state = AutoMapState.None;
            }
        }

        public Fixed MinX => minX;
        public Fixed MaxX => maxX;
        public Fixed MinY => minY;
        public Fixed MaxY => maxY;
        public Fixed ViewX => viewX;
        public Fixed ViewY => viewY;
        public Fixed Zoom => zoom;
        public bool Follow => follow;
        public bool Visible => visible;
        public AutoMapState State => state;
        public IReadOnlyList<Vertex> Marks => marks;
    }
}
