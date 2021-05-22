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

namespace ManagedDoom.SoftwareRendering
{
    public sealed class ThreeDRenderer
    {
        public static readonly int MaxScreenSize = 9;

        private ColorMap colorMap;
        private TextureLookup textures;
        private FlatLookup flats;
        private SpriteLookup sprites;

        private DrawScreen screen;
        private int screenWidth;
        private int screenHeight;
        private byte[] screenData;
        private int drawScale;

        private int windowSize;

        public ThreeDRenderer(CommonResource resource, DrawScreen screen, int windowSize)
        {
            colorMap = resource.ColorMap;
            textures = resource.Textures;
            flats = resource.Flats;
            sprites = resource.Sprites;

            this.screen = screen;
            screenWidth = screen.Width;
            screenHeight = screen.Height;
            screenData = screen.Data;
            drawScale = screenWidth / 320;

            this.windowSize = windowSize;

            InitWallRendering();
            InitPlaneRendering();
            InitSkyRendering();
            InitLighting();
            InitRenderingHistory();
            InitSpriteRendering();
            InitWeaponRendering();
            InitFuzzEffect();
            InitColorTranslation();
            InitWindowBorder(resource.Wad);

            SetWindowSize(windowSize);
        }

        private void SetWindowSize(int size)
        {
            var scale = screenWidth / 320;
            if (size < 7)
            {
                var width = scale * (96 + 32 * size);
                var height = scale * (48 + 16 * size);
                var x = (screenWidth - width) / 2;
                var y = (screenHeight - StatusBarRenderer.Height * scale - height) / 2;
                ResetWindow(x, y, width, height);
            }
            else if (size == 7)
            {
                var width = screenWidth;
                var height = screenHeight - StatusBarRenderer.Height * scale;
                ResetWindow(0, 0, width, height);
            }
            else
            {
                var width = screenWidth;
                var height = screenHeight;
                ResetWindow(0, 0, width, height);
            }

            ResetWallRendering();
            ResetPlaneRendering();
            ResetSkyRendering();
            ResetLighting();
            ResetRenderingHistory();
            ResetWeaponRendering();
        }



        ////////////////////////////////////////////////////////////
        // Window settings
        ////////////////////////////////////////////////////////////

        private int windowX;
        private int windowY;
        private int windowWidth;
        private int windowHeight;
        private int centerX;
        private int centerY;
        private Fixed centerXFrac;
        private Fixed centerYFrac;
        private Fixed projection;

        private void ResetWindow(int x, int y, int width, int height)
        {
            windowX = x;
            windowY = y;
            windowWidth = width;
            windowHeight = height;
            centerX = windowWidth / 2;
            centerY = windowHeight / 2;
            centerXFrac = Fixed.FromInt(centerX);
            centerYFrac = Fixed.FromInt(centerY);
            projection = centerXFrac;
        }



        ////////////////////////////////////////////////////////////
        // Wall rendering
        ////////////////////////////////////////////////////////////

        private const int FineFov = 2048;

        private int[] angleToX;
        private Angle[] xToAngle;
        private Angle clipAngle;
        private Angle clipAngle2;

        private void InitWallRendering()
        {
            angleToX = new int[Trig.FineAngleCount / 2];
            xToAngle = new Angle[screenWidth];
        }

        private void ResetWallRendering()
        {
            var focalLength = centerXFrac / Trig.Tan(Trig.FineAngleCount / 4 + FineFov / 2);

            for (var i = 0; i < Trig.FineAngleCount / 2; i++)
            {
                int t;

                if (Trig.Tan(i) > Fixed.FromInt(2))
                {
                    t = -1;
                }
                else if (Trig.Tan(i) < Fixed.FromInt(-2))
                {
                    t = windowWidth + 1;
                }
                else
                {
                    t = (centerXFrac - Trig.Tan(i) * focalLength).ToIntCeiling();

                    if (t < -1)
                    {
                        t = -1;
                    }
                    else if (t > windowWidth + 1)
                    {
                        t = windowWidth + 1;
                    }
                }

                angleToX[i] = t;
            }

            for (var x = 0; x < windowWidth; x++)
            {
                var i = 0;
                while (angleToX[i] > x)
                {
                    i++;
                }
                xToAngle[x] = new Angle((uint)(i << Trig.AngleToFineShift)) - Angle.Ang90;
            }

            for (var i = 0; i < Trig.FineAngleCount / 2; i++)
            {
                if (angleToX[i] == -1)
                {
                    angleToX[i] = 0;
                }
                else if (angleToX[i] == windowWidth + 1)
                {
                    angleToX[i] = windowWidth;
                }
            }

            clipAngle = xToAngle[0];
            clipAngle2 = new Angle(2 * clipAngle.Data);
        }



        ////////////////////////////////////////////////////////////
        // Plane rendering
        ////////////////////////////////////////////////////////////

        private Fixed[] planeYSlope;
        private Fixed[] planeDistScale;
        private Fixed planeBaseXScale;
        private Fixed planeBaseYScale;

        private Sector ceilingPrevSector;
        private int ceilingPrevX;
        private int ceilingPrevY1;
        private int ceilingPrevY2;
        private Fixed[] ceilingXFrac;
        private Fixed[] ceilingYFrac;
        private Fixed[] ceilingXStep;
        private Fixed[] ceilingYStep;
        private byte[][] ceilingLights;

        private Sector floorPrevSector;
        private int floorPrevX;
        private int floorPrevY1;
        private int floorPrevY2;
        private Fixed[] floorXFrac;
        private Fixed[] floorYFrac;
        private Fixed[] floorXStep;
        private Fixed[] floorYStep;
        private byte[][] floorLights;

        private void InitPlaneRendering()
        {
            planeYSlope = new Fixed[screenHeight];
            planeDistScale = new Fixed[screenWidth];
            ceilingXFrac = new Fixed[screenHeight];
            ceilingYFrac = new Fixed[screenHeight];
            ceilingXStep = new Fixed[screenHeight];
            ceilingYStep = new Fixed[screenHeight];
            ceilingLights = new byte[screenHeight][];
            floorXFrac = new Fixed[screenHeight];
            floorYFrac = new Fixed[screenHeight];
            floorXStep = new Fixed[screenHeight];
            floorYStep = new Fixed[screenHeight];
            floorLights = new byte[screenHeight][];
        }

        private void ResetPlaneRendering()
        {
            for (int i = 0; i < windowHeight; i++)
            {
                var dy = Fixed.FromInt(i - windowHeight / 2) + Fixed.One / 2;
                dy = Fixed.Abs(dy);
                planeYSlope[i] = Fixed.FromInt(windowWidth / 2) / dy;
            }

            for (var i = 0; i < windowWidth; i++)
            {
                var cos = Fixed.Abs(Trig.Cos(xToAngle[i]));
                planeDistScale[i] = Fixed.One / cos;
            }
        }

        private void ClearPlaneRendering()
        {
            var angle = viewAngle - Angle.Ang90;
            planeBaseXScale = Trig.Cos(angle) / centerXFrac;
            planeBaseYScale = -(Trig.Sin(angle) / centerXFrac);

            ceilingPrevSector = null;
            ceilingPrevX = int.MaxValue;

            floorPrevSector = null;
            floorPrevX = int.MaxValue;
        }



        ////////////////////////////////////////////////////////////
        // Sky rendering
        ////////////////////////////////////////////////////////////

        private const int angleToSkyShift = 22;
        private Fixed skyTextureAlt;
        private Fixed skyInvScale;

        private void InitSkyRendering()
        {
            skyTextureAlt = Fixed.FromInt(100);
        }

        private void ResetSkyRendering()
        {
            // The code below is based on PrBoom+' sky rendering implementation.
            var num = (long)Fixed.FracUnit * screenWidth * 200;
            var den = windowWidth * screenHeight;
            skyInvScale = new Fixed((int)(num / den));
        }



        ////////////////////////////////////////////////////////////
        // Lighting
        ////////////////////////////////////////////////////////////

        private const int lightLevelCount = 16;
        private const int lightSegShift = 4;
        private const int scaleLightShift = 12;
        private const int zLightShift = 20;
        private const int colorMapCount = 32;

        private int maxScaleLight;
        private const int maxZLight = 128;

        private byte[][][] diminishingScaleLight;
        private byte[][][] diminishingZLight;
        private byte[][][] fixedLight;

        private byte[][][] scaleLight;
        private byte[][][] zLight;

        private int extraLight;
        private int fixedColorMap;

        private void InitLighting()
        {
            maxScaleLight = 48 * (screenWidth / 320);

            diminishingScaleLight = new byte[lightLevelCount][][];
            diminishingZLight = new byte[lightLevelCount][][];
            fixedLight = new byte[lightLevelCount][][];

            for (var i = 0; i < lightLevelCount; i++)
            {
                diminishingScaleLight[i] = new byte[maxScaleLight][];
                diminishingZLight[i] = new byte[maxZLight][];
                fixedLight[i] = new byte[Math.Max(maxScaleLight, maxZLight)][];
            }

            var distMap = 2;

            // Calculate the light levels to use for each level / distance combination.
            for (var i = 0; i < lightLevelCount; i++)
            {
                var start = ((lightLevelCount - 1 - i) * 2) * colorMapCount / lightLevelCount;
                for (var j = 0; j < maxZLight; j++)
                {
                    var scale = Fixed.FromInt(320 / 2) / new Fixed((j + 1) << zLightShift);
                    scale = new Fixed(scale.Data >> scaleLightShift);

                    var level = start - scale.Data / distMap;
                    if (level < 0)
                    {
                        level = 0;
                    }
                    if (level >= colorMapCount)
                    {
                        level = colorMapCount - 1;
                    }

                    diminishingZLight[i][j] = colorMap[level];
                }
            }
        }

        private void ResetLighting()
        {
            var distMap = 2;

            // Calculate the light levels to use for each level / scale combination.
            for (var i = 0; i < lightLevelCount; i++)
            {
                var start = ((lightLevelCount - 1 - i) * 2) * colorMapCount / lightLevelCount;
                for (var j = 0; j < maxScaleLight; j++)
                {
                    var level = start - j * 320 / windowWidth / distMap;
                    if (level < 0)
                    {
                        level = 0;
                    }
                    if (level >= colorMapCount)
                    {
                        level = colorMapCount - 1;
                    }

                    diminishingScaleLight[i][j] = colorMap[level];
                }
            }
        }

        private void ClearLighting()
        {
            if (fixedColorMap == 0)
            {
                scaleLight = diminishingScaleLight;
                zLight = diminishingZLight;
                fixedLight[0][0] = null;
            }
            else if (fixedLight[0][0] != colorMap[fixedColorMap])
            {
                for (var i = 0; i < lightLevelCount; i++)
                {
                    for (var j = 0; j < fixedLight[i].Length; j++)
                    {
                        fixedLight[i][j] = colorMap[fixedColorMap];
                    }
                }
                scaleLight = fixedLight;
                zLight = fixedLight;
            }
        }



        ////////////////////////////////////////////////////////////
        // Rendering history
        ////////////////////////////////////////////////////////////

        private short[] upperClip;
        private short[] lowerClip;

        private int negOneArray;
        private int windowHeightArray;

        private int clipRangeCount;
        private ClipRange[] clipRanges;

        private int clipDataLength;
        private short[] clipData;

        private int visWallRangeCount;
        private VisWallRange[] visWallRanges;

        private void InitRenderingHistory()
        {
            upperClip = new short[screenWidth];
            lowerClip = new short[screenWidth];

            clipRanges = new ClipRange[256];
            for (var i = 0; i < clipRanges.Length; i++)
            {
                clipRanges[i] = new ClipRange();
            }

            clipData = new short[128 * screenWidth];

            visWallRanges = new VisWallRange[512];
            for (var i = 0; i < visWallRanges.Length; i++)
            {
                visWallRanges[i] = new VisWallRange();
            }
        }

        private void ResetRenderingHistory()
        {
            for (var i = 0; i < windowWidth; i++)
            {
                clipData[i] = -1;
            }
            negOneArray = 0;

            for (var i = windowWidth; i < 2 * windowWidth; i++)
            {
                clipData[i] = (short)windowHeight;
            }
            windowHeightArray = windowWidth;
        }

        private void ClearRenderingHistory()
        {
            for (var x = 0; x < windowWidth; x++)
            {
                upperClip[x] = -1;
            }
            for (var x = 0; x < windowWidth; x++)
            {
                lowerClip[x] = (short)windowHeight;
            }

            clipRanges[0].First = -0x7fffffff;
            clipRanges[0].Last = -1;
            clipRanges[1].First = windowWidth;
            clipRanges[1].Last = 0x7fffffff;
            clipRangeCount = 2;

            clipDataLength = 2 * windowWidth;

            visWallRangeCount = 0;
        }



        ////////////////////////////////////////////////////////////
        // Sprite rendering
        ////////////////////////////////////////////////////////////

        private static readonly Fixed minZ = Fixed.FromInt(4);

        private int visSpriteCount;
        private VisSprite[] visSprites;

        private VisSpriteComparer visSpriteComparer;

        private void InitSpriteRendering()
        {
            visSprites = new VisSprite[256];
            for (var i = 0; i < visSprites.Length; i++)
            {
                visSprites[i] = new VisSprite();
            }

            visSpriteComparer = new VisSpriteComparer();
        }

        private void ClearSpriteRendering()
        {
            visSpriteCount = 0;
        }



        ////////////////////////////////////////////////////////////
        // Weapon rendering
        ////////////////////////////////////////////////////////////

        private VisSprite weaponSprite;
        private Fixed weaponScale;
        private Fixed weaponInvScale;

        private void InitWeaponRendering()
        {
            weaponSprite = new VisSprite();
        }

        private void ResetWeaponRendering()
        {
            weaponScale = new Fixed(Fixed.FracUnit * windowWidth / 320);
            weaponInvScale = new Fixed(Fixed.FracUnit * 320 / windowWidth);
        }



        ////////////////////////////////////////////////////////////
        // Fuzz effect
        ////////////////////////////////////////////////////////////

        private static sbyte[] fuzzTable = new sbyte[]
        {
            1, -1,  1, -1,  1,  1, -1,
            1,  1, -1,  1,  1,  1, -1,
            1,  1,  1, -1, -1, -1, -1,
            1, -1, -1,  1,  1,  1,  1, -1,
            1, -1,  1,  1, -1, -1,  1,
            1, -1, -1, -1, -1,  1,  1,
            1,  1, -1,  1,  1, -1,  1
        };

        private int fuzzPos;

        private void InitFuzzEffect()
        {
            fuzzPos = 0;
        }



        ////////////////////////////////////////////////////////////
        // Color translation
        ////////////////////////////////////////////////////////////

        private byte[] greenToGray;
        private byte[] greenToBrown;
        private byte[] greenToRed;

        private void InitColorTranslation()
        {
            greenToGray = new byte[256];
            greenToBrown = new byte[256];
            greenToRed = new byte[256];
            for (var i = 0; i < 256; i++)
            {
                greenToGray[i] = (byte)i;
                greenToBrown[i] = (byte)i;
                greenToRed[i] = (byte)i;
            }
            for (var i = 112; i < 128; i++)
            {
                greenToGray[i] -= 16;
                greenToBrown[i] -= 48;
                greenToRed[i] -= 80;
            }
        }



        ////////////////////////////////////////////////////////////
        // Window border
        ////////////////////////////////////////////////////////////

        private Patch borderTopLeft;
        private Patch borderTopRight;
        private Patch borderBottomLeft;
        private Patch borderBottomRight;
        private Patch borderTop;
        private Patch borderBottom;
        private Patch borderLeft;
        private Patch borderRight;
        private Flat backFlat;

        private void InitWindowBorder(Wad wad)
        {
            borderTopLeft = Patch.FromWad(wad, "BRDR_TL");
            borderTopRight = Patch.FromWad(wad, "BRDR_TR");
            borderBottomLeft = Patch.FromWad(wad, "BRDR_BL");
            borderBottomRight = Patch.FromWad(wad, "BRDR_BR");
            borderTop = Patch.FromWad(wad, "BRDR_T");
            borderBottom = Patch.FromWad(wad, "BRDR_B");
            borderLeft = Patch.FromWad(wad, "BRDR_L");
            borderRight = Patch.FromWad(wad, "BRDR_R");

            if (wad.GameMode == GameMode.Commercial)
            {
                backFlat = flats["GRNROCK"];
            }
            else
            {
                backFlat = flats["FLOOR7_2"];
            }
        }

        private void FillBackScreen()
        {
            var fillHeight = screenHeight - drawScale * StatusBarRenderer.Height;
            FillRect(0, 0, windowX, fillHeight);
            FillRect(screenWidth - windowX, 0, windowX, fillHeight);
            FillRect(windowX, 0, screenWidth - 2 * windowX, windowY);
            FillRect(windowX, fillHeight - windowY, screenWidth - 2 * windowX, windowY);

            var step = 8 * drawScale;

            for (var x = windowX; x < screenWidth - windowX; x += step)
            {
                screen.DrawPatch(borderTop, x, windowY - step, drawScale);
                screen.DrawPatch(borderBottom, x, fillHeight - windowY, drawScale);
            }

            for (var y = windowY; y < fillHeight - windowY; y += step)
            {
                screen.DrawPatch(borderLeft, windowX - step, y, drawScale);
                screen.DrawPatch(borderRight, screenWidth - windowX, y, drawScale);
            }

            screen.DrawPatch(borderTopLeft, windowX - step, windowY - step, drawScale);
            screen.DrawPatch(borderTopRight, screenWidth - windowX, windowY - step, drawScale);
            screen.DrawPatch(borderBottomLeft, windowX - step, fillHeight - windowY, drawScale);
            screen.DrawPatch(borderBottomRight, screenWidth - windowX, fillHeight - windowY, drawScale);
        }

        private void FillRect(int x, int y, int width, int height)
        {
            var data = backFlat.Data;

            var srcX = x / drawScale;
            var srcY = y / drawScale;

            var invScale = Fixed.One / drawScale;
            var xFrac = invScale - Fixed.Epsilon;

            for (var i = 0; i < width; i++)
            {
                var src = ((srcX + xFrac.ToIntFloor()) & 63) << 6;
                var dst = screenHeight * (x + i) + y;
                var yFrac = invScale - Fixed.Epsilon;
                for (var j = 0; j < height; j++)
                {
                    screenData[dst + j] = data[src | ((srcY + yFrac.ToIntFloor()) & 63)];
                    yFrac += invScale;
                }
                xFrac += invScale;
            }
        }



        ////////////////////////////////////////////////////////////
        // Camera view
        ////////////////////////////////////////////////////////////

        private World world;

        private Fixed viewX;
        private Fixed viewY;
        private Fixed viewZ;
        private Angle viewAngle;

        private Fixed viewSin;
        private Fixed viewCos;

        private int validCount;



        public void Render(Player player)
        {
            world = player.Mobj.World;

            viewX = player.Mobj.X;
            viewY = player.Mobj.Y;
            viewZ = player.ViewZ;
            viewAngle = player.Mobj.Angle;

            viewSin = Trig.Sin(viewAngle);
            viewCos = Trig.Cos(viewAngle);

            validCount = world.GetNewValidCount();

            extraLight = player.ExtraLight;
            fixedColorMap = player.FixedColorMap;

            ClearPlaneRendering();
            ClearLighting();
            ClearRenderingHistory();
            ClearSpriteRendering();

            RenderBspNode(world.Map.Nodes.Length - 1);
            RenderSprites();
            RenderMaskedTextures();
            DrawPlayerSprites(player);

            if (windowSize < 7)
            {
                FillBackScreen();
            }
        }



        private void RenderBspNode(int node)
        {
            if (Node.IsSubsector(node))
            {
                if (node == -1)
                {
                    DrawSubsector(0);
                }
                else
                {
                    DrawSubsector(Node.GetSubsector(node));
                }
                return;
            }

            var bsp = world.Map.Nodes[node];

            // Decide which side the view point is on.
            var side = Geometry.PointOnSide(viewX, viewY, bsp);

            // Recursively divide front space.
            RenderBspNode(bsp.Children[side]);

            // Possibly divide back space.
            if (IsPotentiallyVisible(bsp.BoundingBox[side ^ 1]))
            {
                RenderBspNode(bsp.Children[side ^ 1]);
            }
        }



        private void DrawSubsector(int subsector)
        {
            var target = world.Map.Subsectors[subsector];

            AddSprites(target.Sector, validCount);

            for (var i = 0; i < target.SegCount; i++)
            {
                DrawSeg(world.Map.Segs[target.FirstSeg + i]);
            }
        }



        private static readonly int[][] viewPosToFrustumTangent =
        {
            new[] { 3, 0, 2, 1 },
            new[] { 3, 0, 2, 0 },
            new[] { 3, 1, 2, 0 },
            new[] { 0 },
            new[] { 2, 0, 2, 1 },
            new[] { 0, 0, 0, 0 },
            new[] { 3, 1, 3, 0 },
            new[] { 0 },
            new[] { 2, 0, 3, 1 },
            new[] { 2, 1, 3, 1 },
            new[] { 2, 1, 3, 0 }
        };

        private bool IsPotentiallyVisible(Fixed[] bbox)
        {
            int bx;
            int by;

            // Find the corners of the box that define the edges from
            // current viewpoint.
            if (viewX <= bbox[Box.Left])
            {
                bx = 0;
            }
            else if (viewX < bbox[Box.Right])
            {
                bx = 1;
            }
            else
            {
                bx = 2;
            }

            if (viewY >= bbox[Box.Top])
            {
                by = 0;
            }
            else if (viewY > bbox[Box.Bottom])
            {
                by = 1;
            }
            else
            {
                by = 2;
            }

            var viewPos = (by << 2) + bx;
            if (viewPos == 5)
            {
                return true;
            }

            var x1 = bbox[viewPosToFrustumTangent[viewPos][0]];
            var y1 = bbox[viewPosToFrustumTangent[viewPos][1]];
            var x2 = bbox[viewPosToFrustumTangent[viewPos][2]];
            var y2 = bbox[viewPosToFrustumTangent[viewPos][3]];

            // Check clip list for an open space.
            var angle1 = Geometry.PointToAngle(viewX, viewY, x1, y1) - viewAngle;
            var angle2 = Geometry.PointToAngle(viewX, viewY, x2, y2) - viewAngle;

            var span = angle1 - angle2;

            // Sitting on a line?
            if (span >= Angle.Ang180)
            {
                return true;
            }

            var tSpan1 = angle1 + clipAngle;

            if (tSpan1 > clipAngle2)
            {
                tSpan1 -= clipAngle2;

                // Totally off the left edge?
                if (tSpan1 >= span)
                {
                    return false;
                }

                angle1 = clipAngle;
            }

            var tSpan2 = clipAngle - angle2;
            if (tSpan2 > clipAngle2)
            {
                tSpan2 -= clipAngle2;

                // Totally off the left edge?
                if (tSpan2 >= span)
                {
                    return false;
                }

                angle2 = -clipAngle;
            }

            // Find the first clippost that touches the source post
            // (adjacent pixels are touching).
            var sx1 = angleToX[(angle1 + Angle.Ang90).Data >> Trig.AngleToFineShift];
            var sx2 = angleToX[(angle2 + Angle.Ang90).Data >> Trig.AngleToFineShift];

            // Does not cross a pixel.
            if (sx1 == sx2)
            {
                return false;
            }

            sx2--;

            var start = 0;
            while (clipRanges[start].Last < sx2)
            {
                start++;
            }

            if (sx1 >= clipRanges[start].First && sx2 <= clipRanges[start].Last)
            {
                // The clippost contains the new span.
                return false;
            }

            return true;
        }



        private void DrawSeg(Seg seg)
        {
            // OPTIMIZE: quickly reject orthogonal back sides.
            var angle1 = Geometry.PointToAngle(viewX, viewY, seg.Vertex1.X, seg.Vertex1.Y);
            var angle2 = Geometry.PointToAngle(viewX, viewY, seg.Vertex2.X, seg.Vertex2.Y);

            // Clip to view edges.
            // OPTIMIZE: make constant out of 2 * clipangle (FIELDOFVIEW).
            var span = angle1 - angle2;

            // Back side? I.e. backface culling?
            if (span >= Angle.Ang180)
            {
                return;
            }

            // Global angle needed by segcalc.
            var rwAngle1 = angle1;

            angle1 -= viewAngle;
            angle2 -= viewAngle;

            var tSpan1 = angle1 + clipAngle;
            if (tSpan1 > clipAngle2)
            {
                tSpan1 -= clipAngle2;

                // Totally off the left edge?
                if (tSpan1 >= span)
                {
                    return;
                }

                angle1 = clipAngle;
            }

            var tSpan2 = clipAngle - angle2;
            if (tSpan2 > clipAngle2)
            {
                tSpan2 -= clipAngle2;

                // Totally off the left edge?
                if (tSpan2 >= span)
                {
                    return;
                }

                angle2 = -clipAngle;
            }

            // The seg is in the view range, but not necessarily visible.
            var x1 = angleToX[(angle1 + Angle.Ang90).Data >> Trig.AngleToFineShift];
            var x2 = angleToX[(angle2 + Angle.Ang90).Data >> Trig.AngleToFineShift];

            // Does not cross a pixel?
            if (x1 == x2)
            {
                return;
            }

            var frontSector = seg.FrontSector;
            var backSector = seg.BackSector;

            // Single sided line?
            if (backSector == null)
            {
                DrawSolidWall(seg, rwAngle1, x1, x2 - 1);
                return;
            }

            // Closed door.
            if (backSector.CeilingHeight <= frontSector.FloorHeight ||
                backSector.FloorHeight >= frontSector.CeilingHeight)
            {
                DrawSolidWall(seg, rwAngle1, x1, x2 - 1);
                return;
            }

            // Window.
            if (backSector.CeilingHeight != frontSector.CeilingHeight ||
                backSector.FloorHeight != frontSector.FloorHeight)
            {
                DrawPassWall(seg, rwAngle1, x1, x2 - 1);
                return;
            }

            // Reject empty lines used for triggers and special events.
            // Identical floor and ceiling on both sides, identical
            // light levels on both sides, and no middle texture.
            if (backSector.CeilingFlat == frontSector.CeilingFlat &&
                backSector.FloorFlat == frontSector.FloorFlat &&
                backSector.LightLevel == frontSector.LightLevel &&
                seg.SideDef.MiddleTexture == 0)
            {
                return;
            }

            DrawPassWall(seg, rwAngle1, x1, x2 - 1);
        }



        private void DrawSolidWall(Seg seg, Angle rwAngle1, int x1, int x2)
        {
            int next;
            int start;

            // Find the first range that touches the range
            // (adjacent pixels are touching).
            start = 0;
            while (clipRanges[start].Last < x1 - 1)
            {
                start++;
            }

            if (x1 < clipRanges[start].First)
            {
                if (x2 < clipRanges[start].First - 1)
                {
                    // Post is entirely visible (above start),
                    // so insert a new clippost.
                    DrawSolidWallRange(seg, rwAngle1, x1, x2);
                    next = clipRangeCount;
                    clipRangeCount++;

                    while (next != start)
                    {
                        clipRanges[next].CopyFrom(clipRanges[next - 1]);
                        next--;
                    }
                    clipRanges[next].First = x1;
                    clipRanges[next].Last = x2;
                    return;
                }

                // There is a fragment above *start.
                DrawSolidWallRange(seg, rwAngle1, x1, clipRanges[start].First - 1);

                // Now adjust the clip size.
                clipRanges[start].First = x1;
            }

            // Bottom contained in start?
            if (x2 <= clipRanges[start].Last)
            {
                return;
            }

            next = start;
            while (x2 >= clipRanges[next + 1].First - 1)
            {
                // There is a fragment between two posts.
                DrawSolidWallRange(seg, rwAngle1, clipRanges[next].Last + 1, clipRanges[next + 1].First - 1);
                next++;

                if (x2 <= clipRanges[next].Last)
                {
                    // Bottom is contained in next.
                    // Adjust the clip size.
                    clipRanges[start].Last = clipRanges[next].Last;
                    goto crunch;
                }
            }

            // There is a fragment after *next.
            DrawSolidWallRange(seg, rwAngle1, clipRanges[next].Last + 1, x2);

            // Adjust the clip size.
            clipRanges[start].Last = x2;

            // Remove start + 1 to next from the clip list,
            // because start now covers their area.
            crunch:
            if (next == start)
            {
                // Post just extended past the bottom of one post.
                return;
            }

            while (next++ != clipRangeCount)
            {
                // Remove a post.
                clipRanges[++start].CopyFrom(clipRanges[next]);
            }

            clipRangeCount = start + 1;
        }



        private void DrawPassWall(Seg seg, Angle rwAngle1, int x1, int x2)
        {
            int start;

            // Find the first range that touches the range
            // (adjacent pixels are touching).
            start = 0;
            while (clipRanges[start].Last < x1 - 1)
            {
                start++;
            }

            if (x1 < clipRanges[start].First)
            {
                if (x2 < clipRanges[start].First - 1)
                {
                    // Post is entirely visible (above start).
                    DrawPassWallRange(seg, rwAngle1, x1, x2, false);
                    return;
                }

                // There is a fragment above *start.
                DrawPassWallRange(seg, rwAngle1, x1, clipRanges[start].First - 1, false);
            }

            // Bottom contained in start?
            if (x2 <= clipRanges[start].Last)
            {
                return;
            }

            while (x2 >= clipRanges[start + 1].First - 1)
            {
                // There is a fragment between two posts.
                DrawPassWallRange(seg, rwAngle1, clipRanges[start].Last + 1, clipRanges[start + 1].First - 1, false);
                start++;

                if (x2 <= clipRanges[start].Last)
                {
                    return;
                }
            }

            // There is a fragment after *next.
            DrawPassWallRange(seg, rwAngle1, clipRanges[start].Last + 1, x2, false);
        }



        private Fixed ScaleFromGlobalAngle(Angle visAngle, Angle viewAngle, Angle rwNormal, Fixed rwDistance)
        {
            var num = projection * Trig.Sin(Angle.Ang90 + (visAngle - rwNormal));
            var den = rwDistance * Trig.Sin(Angle.Ang90 + (visAngle - viewAngle));

            Fixed scale;
            if (den.Data > num.Data >> 16)
            {
                scale = num / den;

                if (scale > Fixed.FromInt(64))
                {
                    scale = Fixed.FromInt(64);
                }
                else if (scale.Data < 256)
                {
                    scale = new Fixed(256);
                }
            }
            else
            {
                scale = Fixed.FromInt(64);
            }

            return scale;
        }



        private const int heightBits = 12;
        private const int heightUnit = 1 << heightBits;

        private void DrawSolidWallRange(Seg seg, Angle rwAngle1, int x1, int x2)
        {
            if (seg.BackSector != null)
            {
                DrawPassWallRange(seg, rwAngle1, x1, x2, true);
                return;
            }

            if (visWallRangeCount == visWallRanges.Length)
            {
                // Too many visible walls.
                return;
            }

            // Make some aliases to shorten the following code.
            var line = seg.LineDef;
            var side = seg.SideDef;
            var frontSector = seg.FrontSector;

            // Mark the segment as visible for auto map.
            line.Flags |= LineFlags.Mapped;

            // Calculate the relative plane heights of front and back sector.
            var worldFrontZ1 = frontSector.CeilingHeight - viewZ;
            var worldFrontZ2 = frontSector.FloorHeight - viewZ;

            // Check which parts must be rendered.
            var drawWall = side.MiddleTexture != 0;
            var drawCeiling = worldFrontZ1 > Fixed.Zero || frontSector.CeilingFlat == flats.SkyFlatNumber;
            var drawFloor = worldFrontZ2 < Fixed.Zero;

            //
            // Determine how the wall textures are vertically aligned.
            //

            var wallTexture = textures[world.Specials.TextureTranslation[side.MiddleTexture]];
            var wallWidthMask = wallTexture.Width - 1;

            Fixed middleTextureAlt;
            if ((line.Flags & LineFlags.DontPegBottom) != 0)
            {
                var vTop = frontSector.FloorHeight + Fixed.FromInt(wallTexture.Height);
                middleTextureAlt = vTop - viewZ;
            }
            else
            {
                middleTextureAlt = worldFrontZ1;
            }
            middleTextureAlt += side.RowOffset;

            //
            // Calculate the scaling factors of the left and right edges of the wall range.
            //

            var rwNormalAngle = seg.Angle + Angle.Ang90;

            var offsetAngle = Angle.Abs(rwNormalAngle - rwAngle1);
            if (offsetAngle > Angle.Ang90)
            {
                offsetAngle = Angle.Ang90;
            }

            var distAngle = Angle.Ang90 - offsetAngle;

            var hypotenuse = Geometry.PointToDist(viewX, viewY, seg.Vertex1.X, seg.Vertex1.Y);

            var rwDistance = hypotenuse * Trig.Sin(distAngle);

            var rwScale = ScaleFromGlobalAngle(viewAngle + xToAngle[x1], viewAngle, rwNormalAngle, rwDistance);

            Fixed scale1 = rwScale;
            Fixed scale2;
            Fixed rwScaleStep;
            if (x2 > x1)
            {
                scale2 = ScaleFromGlobalAngle(viewAngle + xToAngle[x2], viewAngle, rwNormalAngle, rwDistance);
                rwScaleStep = (scale2 - rwScale) / (x2 - x1);
            }
            else
            {
                scale2 = scale1;
                rwScaleStep = Fixed.Zero;
            }

            //
            // Determine how the wall textures are horizontally aligned
            // and which color map is used according to the light level (if necessary).
            //

            var textureOffsetAngle = rwNormalAngle - rwAngle1;
            if (textureOffsetAngle > Angle.Ang180)
            {
                textureOffsetAngle = -textureOffsetAngle;
            }
            if (textureOffsetAngle > Angle.Ang90)
            {
                textureOffsetAngle = Angle.Ang90;
            }

            var rwOffset = hypotenuse * Trig.Sin(textureOffsetAngle);
            if (rwNormalAngle - rwAngle1 < Angle.Ang180)
            {
                rwOffset = -rwOffset;
            }
            rwOffset += seg.Offset + side.TextureOffset;

            var rwCenterAngle = Angle.Ang90 + viewAngle - rwNormalAngle;

            var wallLightLevel = (frontSector.LightLevel >> lightSegShift) + extraLight;
            if (seg.Vertex1.Y == seg.Vertex2.Y)
            {
                wallLightLevel--;
            }
            else if (seg.Vertex1.X == seg.Vertex2.X)
            {
                wallLightLevel++;
            }

            var wallLights = scaleLight[Math.Clamp(wallLightLevel, 0, lightLevelCount - 1)];

            //
            // Determine where on the screen the wall is drawn.
            //

            // These values are right shifted to avoid overflow in the following process (maybe).
            worldFrontZ1 >>= 4;
            worldFrontZ2 >>= 4;

            // The Y positions of the top / bottom edges of the wall on the screen.
            var wallY1Frac = (centerYFrac >> 4) - worldFrontZ1 * rwScale;
            var wallY1Step = -(rwScaleStep * worldFrontZ1);
            var wallY2Frac = (centerYFrac >> 4) - worldFrontZ2 * rwScale;
            var wallY2Step = -(rwScaleStep * worldFrontZ2);

            //
            // Determine which color map is used for the plane according to the light level.
            //

            var planeLightLevel = (frontSector.LightLevel >> lightSegShift) + extraLight;
            if (planeLightLevel >= lightLevelCount)
            {
                planeLightLevel = lightLevelCount - 1;
            }
            var planeLights = zLight[planeLightLevel];

            //
            // Prepare to record the rendering history.
            //

            var visWallRange = visWallRanges[visWallRangeCount];
            visWallRangeCount++;

            visWallRange.Seg = seg;
            visWallRange.X1 = x1;
            visWallRange.X2 = x2;
            visWallRange.Scale1 = scale1;
            visWallRange.Scale2 = scale2;
            visWallRange.ScaleStep = rwScaleStep;
            visWallRange.Silhouette = Silhouette.Both;
            visWallRange.LowerSilHeight = Fixed.MaxValue;
            visWallRange.UpperSilHeight = Fixed.MinValue;
            visWallRange.MaskedTextureColumn = -1;
            visWallRange.UpperClip = windowHeightArray;
            visWallRange.LowerClip = negOneArray;

            //
            // Floor and ceiling.
            //

            var ceilingFlat = flats[world.Specials.FlatTranslation[frontSector.CeilingFlat]];
            var floorFlat = flats[world.Specials.FlatTranslation[frontSector.FloorFlat]];

            //
            // Now the rendering is carried out.
            //

            for (var x = x1; x <= x2; x++)
            {
                var drawWallY1 = (wallY1Frac.Data + heightUnit - 1) >> heightBits;
                var drawWallY2 = wallY2Frac.Data >> heightBits;

                if (drawCeiling)
                {
                    var cy1 = upperClip[x] + 1;
                    var cy2 = Math.Min(drawWallY1 - 1, lowerClip[x] - 1);
                    DrawCeilingColumn(frontSector, ceilingFlat, planeLights, x, cy1, cy2);
                }

                if (drawWall)
                {
                    var wy1 = Math.Max(drawWallY1, upperClip[x] + 1);
                    var wy2 = Math.Min(drawWallY2, lowerClip[x] - 1);

                    var angle = rwCenterAngle + xToAngle[x];
                    angle = new Angle(angle.Data & 0x7FFFFFFF);

                    var textureColumn = (rwOffset - Trig.Tan(angle) * rwDistance).ToIntFloor();
                    var source = wallTexture.Composite.Columns[textureColumn & wallWidthMask];

                    if (source.Length > 0)
                    {
                        var lightIndex = rwScale.Data >> scaleLightShift;
                        if (lightIndex >= maxScaleLight)
                        {
                            lightIndex = maxScaleLight - 1;
                        }

                        var invScale = new Fixed((int)(0xffffffffu / (uint)rwScale.Data));
                        DrawColumn(source[0], wallLights[lightIndex], x, wy1, wy2, invScale, middleTextureAlt);
                    }
                }

                if (drawFloor)
                {
                    var fy1 = Math.Max(drawWallY2 + 1, upperClip[x] + 1);
                    var fy2 = lowerClip[x] - 1;
                    DrawFloorColumn(frontSector, floorFlat, planeLights, x, fy1, fy2);
                }

                rwScale += rwScaleStep;
                wallY1Frac += wallY1Step;
                wallY2Frac += wallY2Step;
            }
        }



        private void DrawPassWallRange(Seg seg, Angle rwAngle1, int x1, int x2, bool drawAsSolidWall)
        {
            if (visWallRangeCount == visWallRanges.Length)
            {
                // Too many visible walls.
                return;
            }

            var range = x2 - x1 + 1;

            if (clipDataLength + 3 * range >= clipData.Length)
            {
                // Clip info buffer is not sufficient.
                return;
            }

            // Make some aliases to shorten the following code.
            var line = seg.LineDef;
            var side = seg.SideDef;
            var frontSector = seg.FrontSector;
            var backSector = seg.BackSector;

            // Mark the segment as visible for auto map.
            line.Flags |= LineFlags.Mapped;

            // Calculate the relative plane heights of front and back sector.
            // These values are later 4 bits right shifted to calculate the rendering area.
            var worldFrontZ1 = frontSector.CeilingHeight - viewZ;
            var worldFrontZ2 = frontSector.FloorHeight - viewZ;
            var worldBackZ1 = backSector.CeilingHeight - viewZ;
            var worldBackZ2 = backSector.FloorHeight - viewZ;

            // The hack below enables ceiling height change in outdoor area without showing the upper wall.
            if (frontSector.CeilingFlat == flats.SkyFlatNumber &&
                backSector.CeilingFlat == flats.SkyFlatNumber)
            {
                worldFrontZ1 = worldBackZ1;
            }

            //
            // Check which parts must be rendered.
            //

            bool drawUpperWall;
            bool drawCeiling;
            if (drawAsSolidWall ||
                worldFrontZ1 != worldBackZ1 ||
                frontSector.CeilingFlat != backSector.CeilingFlat ||
                frontSector.LightLevel != backSector.LightLevel)
            {
                drawUpperWall = side.TopTexture != 0 && worldBackZ1 < worldFrontZ1;
                drawCeiling = worldFrontZ1 >= Fixed.Zero || frontSector.CeilingFlat == flats.SkyFlatNumber;
            }
            else
            {
                drawUpperWall = false;
                drawCeiling = false;
            }

            bool drawLowerWall;
            bool drawFloor;
            if (drawAsSolidWall ||
                worldFrontZ2 != worldBackZ2 ||
                frontSector.FloorFlat != backSector.FloorFlat ||
                frontSector.LightLevel != backSector.LightLevel)
            {
                drawLowerWall = side.BottomTexture != 0 && worldBackZ2 > worldFrontZ2;
                drawFloor = worldFrontZ2 <= Fixed.Zero;
            }
            else
            {
                drawLowerWall = false;
                drawFloor = false;
            }

            var drawMaskedTexture = side.MiddleTexture != 0;

            // If nothing must be rendered, we can skip this seg.
            if (!drawUpperWall && !drawCeiling && !drawLowerWall && !drawFloor && !drawMaskedTexture)
            {
                return;
            }

            var segTextured = drawUpperWall || drawLowerWall || drawMaskedTexture;

            //
            // Determine how the wall textures are vertically aligned (if necessary).
            //

            var upperWallTexture = default(Texture);
            var upperWallWidthMask = default(int);
            var uperTextureAlt = default(Fixed);
            if (drawUpperWall)
            {
                upperWallTexture = textures[world.Specials.TextureTranslation[side.TopTexture]];
                upperWallWidthMask = upperWallTexture.Width - 1;

                if ((line.Flags & LineFlags.DontPegTop) != 0)
                {
                    uperTextureAlt = worldFrontZ1;
                }
                else
                {
                    var vTop = backSector.CeilingHeight + Fixed.FromInt(upperWallTexture.Height);
                    uperTextureAlt = vTop - viewZ;
                }
                uperTextureAlt += side.RowOffset;
            }

            var lowerWallTexture = default(Texture);
            var lowerWallWidthMask = default(int);
            var lowerTextureAlt = default(Fixed);
            if (drawLowerWall)
            {
                lowerWallTexture = textures[world.Specials.TextureTranslation[side.BottomTexture]];
                lowerWallWidthMask = lowerWallTexture.Width - 1;

                if ((line.Flags & LineFlags.DontPegBottom) != 0)
                {
                    lowerTextureAlt = worldFrontZ1;
                }
                else
                {
                    lowerTextureAlt = worldBackZ2;
                }
                lowerTextureAlt += side.RowOffset;
            }

            //
            // Calculate the scaling factors of the left and right edges of the wall range.
            //

            var rwNormalAngle = seg.Angle + Angle.Ang90;

            var offsetAngle = Angle.Abs(rwNormalAngle - rwAngle1);
            if (offsetAngle > Angle.Ang90)
            {
                offsetAngle = Angle.Ang90;
            }

            var distAngle = Angle.Ang90 - offsetAngle;

            var hypotenuse = Geometry.PointToDist(viewX, viewY, seg.Vertex1.X, seg.Vertex1.Y);

            var rwDistance = hypotenuse * Trig.Sin(distAngle);

            var rwScale = ScaleFromGlobalAngle(viewAngle + xToAngle[x1], viewAngle, rwNormalAngle, rwDistance);

            Fixed scale1 = rwScale;
            Fixed scale2;
            Fixed rwScaleStep;
            if (x2 > x1)
            {
                scale2 = ScaleFromGlobalAngle(viewAngle + xToAngle[x2], viewAngle, rwNormalAngle, rwDistance);
                rwScaleStep = (scale2 - rwScale) / (x2 - x1);
            }
            else
            {
                scale2 = scale1;
                rwScaleStep = Fixed.Zero;
            }

            //
            // Determine how the wall textures are horizontally aligned
            // and which color map is used according to the light level (if necessary).
            //

            var rwOffset = default(Fixed);
            var rwCenterAngle = default(Angle);
            var wallLights = default(byte[][]);
            if (segTextured)
            {
                var textureOffsetAngle = rwNormalAngle - rwAngle1;
                if (textureOffsetAngle > Angle.Ang180)
                {
                    textureOffsetAngle = -textureOffsetAngle;
                }
                if (textureOffsetAngle > Angle.Ang90)
                {
                    textureOffsetAngle = Angle.Ang90;
                }

                rwOffset = hypotenuse * Trig.Sin(textureOffsetAngle);
                if (rwNormalAngle - rwAngle1 < Angle.Ang180)
                {
                    rwOffset = -rwOffset;
                }
                rwOffset += seg.Offset + side.TextureOffset;

                rwCenterAngle = Angle.Ang90 + viewAngle - rwNormalAngle;

                var wallLightLevel = (frontSector.LightLevel >> lightSegShift) + extraLight;
                if (seg.Vertex1.Y == seg.Vertex2.Y)
                {
                    wallLightLevel--;
                }
                else if (seg.Vertex1.X == seg.Vertex2.X)
                {
                    wallLightLevel++;
                }

                wallLights = scaleLight[Math.Clamp(wallLightLevel, 0, lightLevelCount - 1)];
            }

            //
            // Determine where on the screen the wall is drawn.
            //

            // These values are right shifted to avoid overflow in the following process.
            worldFrontZ1 >>= 4;
            worldFrontZ2 >>= 4;
            worldBackZ1 >>= 4;
            worldBackZ2 >>= 4;

            // The Y positions of the top / bottom edges of the wall on the screen..
            var wallY1Frac = (centerYFrac >> 4) - worldFrontZ1 * rwScale;
            var wallY1Step = -(rwScaleStep * worldFrontZ1);
            var wallY2Frac = (centerYFrac >> 4) - worldFrontZ2 * rwScale;
            var wallY2Step = -(rwScaleStep * worldFrontZ2);

            // The Y position of the top edge of the portal (if visible).
            var portalY1Frac = default(Fixed);
            var portalY1Step = default(Fixed);
            if (drawUpperWall)
            {
                if (worldBackZ1 > worldFrontZ2)
                {
                    portalY1Frac = (centerYFrac >> 4) - worldBackZ1 * rwScale;
                    portalY1Step = -(rwScaleStep * worldBackZ1);
                }
                else
                {
                    portalY1Frac = (centerYFrac >> 4) - worldFrontZ2 * rwScale;
                    portalY1Step = -(rwScaleStep * worldFrontZ2);
                }
            }

            // The Y position of the bottom edge of the portal (if visible).
            var portalY2Frac = default(Fixed);
            var portalY2Step = default(Fixed);
            if (drawLowerWall)
            {
                if (worldBackZ2 < worldFrontZ1)
                {
                    portalY2Frac = (centerYFrac >> 4) - worldBackZ2 * rwScale;
                    portalY2Step = -(rwScaleStep * worldBackZ2);
                }
                else
                {
                    portalY2Frac = (centerYFrac >> 4) - worldFrontZ1 * rwScale;
                    portalY2Step = -(rwScaleStep * worldFrontZ1);
                }
            }

            //
            // Determine which color map is used for the plane according to the light level.
            //

            var planeLightLevel = (frontSector.LightLevel >> lightSegShift) + extraLight;
            if (planeLightLevel >= lightLevelCount)
            {
                planeLightLevel = lightLevelCount - 1;
            }
            var planeLights = zLight[planeLightLevel];

            //
            // Prepare to record the rendering history.
            //

            var visWallRange = visWallRanges[visWallRangeCount];
            visWallRangeCount++;

            visWallRange.Seg = seg;
            visWallRange.X1 = x1;
            visWallRange.X2 = x2;
            visWallRange.Scale1 = scale1;
            visWallRange.Scale2 = scale2;
            visWallRange.ScaleStep = rwScaleStep;

            visWallRange.UpperClip = -1;
            visWallRange.LowerClip = -1;
            visWallRange.Silhouette = 0;

            if (frontSector.FloorHeight > backSector.FloorHeight)
            {
                visWallRange.Silhouette = Silhouette.Lower;
                visWallRange.LowerSilHeight = frontSector.FloorHeight;
            }
            else if (backSector.FloorHeight > viewZ)
            {
                visWallRange.Silhouette = Silhouette.Lower;
                visWallRange.LowerSilHeight = Fixed.MaxValue;
            }

            if (frontSector.CeilingHeight < backSector.CeilingHeight)
            {
                visWallRange.Silhouette |= Silhouette.Upper;
                visWallRange.UpperSilHeight = frontSector.CeilingHeight;
            }
            else if (backSector.CeilingHeight < viewZ)
            {
                visWallRange.Silhouette |= Silhouette.Upper;
                visWallRange.UpperSilHeight = Fixed.MinValue;
            }

            if (backSector.CeilingHeight <= frontSector.FloorHeight)
            {
                visWallRange.LowerClip = negOneArray;
                visWallRange.LowerSilHeight = Fixed.MaxValue;
                visWallRange.Silhouette |= Silhouette.Lower;
            }

            if (backSector.FloorHeight >= frontSector.CeilingHeight)
            {
                visWallRange.UpperClip = windowHeightArray;
                visWallRange.UpperSilHeight = Fixed.MinValue;
                visWallRange.Silhouette |= Silhouette.Upper;
            }

            var maskedTextureColumn = default(int);
            if (drawMaskedTexture)
            {
                maskedTextureColumn = clipDataLength - x1;
                visWallRange.MaskedTextureColumn = maskedTextureColumn;
                clipDataLength += range;
            }
            else
            {
                visWallRange.MaskedTextureColumn = -1;
            }

            //
            // Floor and ceiling.
            //

            var ceilingFlat = flats[world.Specials.FlatTranslation[frontSector.CeilingFlat]];
            var floorFlat = flats[world.Specials.FlatTranslation[frontSector.FloorFlat]];

            //
            // Now the rendering is carried out.
            //

            for (var x = x1; x <= x2; x++)
            {
                var drawWallY1 = (wallY1Frac.Data + heightUnit - 1) >> heightBits;
                var drawWallY2 = wallY2Frac.Data >> heightBits;

                var textureColumn = default(int);
                var lightIndex = default(int);
                var invScale = default(Fixed);
                if (segTextured)
                {
                    var angle = rwCenterAngle + xToAngle[x];
                    angle = new Angle(angle.Data & 0x7FFFFFFF);
                    textureColumn = (rwOffset - Trig.Tan(angle) * rwDistance).ToIntFloor();

                    lightIndex = rwScale.Data >> scaleLightShift;
                    if (lightIndex >= maxScaleLight)
                    {
                        lightIndex = maxScaleLight - 1;
                    }

                    invScale = new Fixed((int)(0xffffffffu / (uint)rwScale.Data));
                }

                if (drawUpperWall)
                {
                    var drawUpperWallY1 = (wallY1Frac.Data + heightUnit - 1) >> heightBits;
                    var drawUpperWallY2 = portalY1Frac.Data >> heightBits;

                    if (drawCeiling)
                    {
                        var cy1 = upperClip[x] + 1;
                        var cy2 = Math.Min(drawWallY1 - 1, lowerClip[x] - 1);
                        DrawCeilingColumn(frontSector, ceilingFlat, planeLights, x, cy1, cy2);
                    }

                    var wy1 = Math.Max(drawUpperWallY1, upperClip[x] + 1);
                    var wy2 = Math.Min(drawUpperWallY2, lowerClip[x] - 1);
                    var source = upperWallTexture.Composite.Columns[textureColumn & upperWallWidthMask];
                    if (source.Length > 0)
                    {
                        DrawColumn(source[0], wallLights[lightIndex], x, wy1, wy2, invScale, uperTextureAlt);
                    }

                    if (upperClip[x] < wy2)
                    {
                        upperClip[x] = (short)wy2;
                    }

                    portalY1Frac += portalY1Step;
                }
                else if (drawCeiling)
                {
                    var cy1 = upperClip[x] + 1;
                    var cy2 = Math.Min(drawWallY1 - 1, lowerClip[x] - 1);
                    DrawCeilingColumn(frontSector, ceilingFlat, planeLights, x, cy1, cy2);

                    if (upperClip[x] < cy2)
                    {
                        upperClip[x] = (short)cy2;
                    }
                }

                if (drawLowerWall)
                {
                    var drawLowerWallY1 = (portalY2Frac.Data + heightUnit - 1) >> heightBits;
                    var drawLowerWallY2 = wallY2Frac.Data >> heightBits;

                    var wy1 = Math.Max(drawLowerWallY1, upperClip[x] + 1);
                    var wy2 = Math.Min(drawLowerWallY2, lowerClip[x] - 1);
                    var source = lowerWallTexture.Composite.Columns[textureColumn & lowerWallWidthMask];
                    if (source.Length > 0)
                    {
                        DrawColumn(source[0], wallLights[lightIndex], x, wy1, wy2, invScale, lowerTextureAlt);
                    }

                    if (drawFloor)
                    {
                        var fy1 = Math.Max(drawWallY2 + 1, upperClip[x] + 1);
                        var fy2 = lowerClip[x] - 1;
                        DrawFloorColumn(frontSector, floorFlat, planeLights, x, fy1, fy2);
                    }

                    if (lowerClip[x] > wy1)
                    {
                        lowerClip[x] = (short)wy1;
                    }

                    portalY2Frac += portalY2Step;
                }
                else if (drawFloor)
                {
                    var fy1 = Math.Max(drawWallY2 + 1, upperClip[x] + 1);
                    var fy2 = lowerClip[x] - 1;
                    DrawFloorColumn(frontSector, floorFlat, planeLights, x, fy1, fy2);

                    if (lowerClip[x] > drawWallY2 + 1)
                    {
                        lowerClip[x] = (short)fy1;
                    }
                }

                if (drawMaskedTexture)
                {
                    clipData[maskedTextureColumn + x] = (short)textureColumn;
                }

                rwScale += rwScaleStep;
                wallY1Frac += wallY1Step;
                wallY2Frac += wallY2Step;
            }

            //
            // Save sprite clipping info.
            //

            if (((visWallRange.Silhouette & Silhouette.Upper) != 0 ||
                drawMaskedTexture) && visWallRange.UpperClip == -1)
            {
                Array.Copy(upperClip, x1, clipData, clipDataLength, range);
                visWallRange.UpperClip = clipDataLength - x1;
                clipDataLength += range;
            }

            if (((visWallRange.Silhouette & Silhouette.Lower) != 0 ||
                drawMaskedTexture) && visWallRange.LowerClip == -1)
            {
                Array.Copy(lowerClip, x1, clipData, clipDataLength, range);
                visWallRange.LowerClip = clipDataLength - x1;
                clipDataLength += range;
            }

            if (drawMaskedTexture && (visWallRange.Silhouette & Silhouette.Upper) == 0)
            {
                visWallRange.Silhouette |= Silhouette.Upper;
                visWallRange.UpperSilHeight = Fixed.MinValue;
            }

            if (drawMaskedTexture && (visWallRange.Silhouette & Silhouette.Lower) == 0)
            {
                visWallRange.Silhouette |= Silhouette.Lower;
                visWallRange.LowerSilHeight = Fixed.MaxValue;
            }
        }



        private void RenderMaskedTextures()
        {
            for (var i = visWallRangeCount - 1; i >= 0; i--)
            {
                var drawSeg = visWallRanges[i];
                if (drawSeg.MaskedTextureColumn != -1)
                {
                    DrawMaskedRange(drawSeg, drawSeg.X1, drawSeg.X2);
                }
            }
        }



        private void DrawMaskedRange(VisWallRange drawSeg, int x1, int x2)
        {
            var seg = drawSeg.Seg;

            var wallLightLevel = (seg.FrontSector.LightLevel >> lightSegShift) + extraLight;
            if (seg.Vertex1.Y == seg.Vertex2.Y)
            {
                wallLightLevel--;
            }
            else if (seg.Vertex1.X == seg.Vertex2.X)
            {
                wallLightLevel++;
            }

            var wallLights = scaleLight[Math.Clamp(wallLightLevel, 0, lightLevelCount - 1)];

            var wallTexture = textures[world.Specials.TextureTranslation[seg.SideDef.MiddleTexture]];
            var mask = wallTexture.Width - 1;

            Fixed midTextureAlt;
            if ((seg.LineDef.Flags & LineFlags.DontPegBottom) != 0)
            {
                midTextureAlt = seg.FrontSector.FloorHeight > seg.BackSector.FloorHeight
                    ? seg.FrontSector.FloorHeight : seg.BackSector.FloorHeight;
                midTextureAlt = midTextureAlt + Fixed.FromInt(wallTexture.Height) - viewZ;
            }
            else
            {
                midTextureAlt = seg.FrontSector.CeilingHeight < seg.BackSector.CeilingHeight
                    ? seg.FrontSector.CeilingHeight : seg.BackSector.CeilingHeight;
                midTextureAlt = midTextureAlt - viewZ;
            }
            midTextureAlt += seg.SideDef.RowOffset;

            var scaleStep = drawSeg.ScaleStep;
            var scale = drawSeg.Scale1 + (x1 - drawSeg.X1) * scaleStep;

            for (var x = x1; x <= x2; x++)
            {
                var index = Math.Min(scale.Data >> scaleLightShift, maxScaleLight - 1);

                var col = clipData[drawSeg.MaskedTextureColumn + x];

                if (col != short.MaxValue)
                {
                    var topY = centerYFrac - midTextureAlt * scale;
                    var invScale = new Fixed((int)(0xffffffffu / (uint)scale.Data));
                    var ceilClip = clipData[drawSeg.UpperClip + x];
                    var floorClip = clipData[drawSeg.LowerClip + x];
                    DrawMaskedColumn(
                        wallTexture.Composite.Columns[col & mask],
                        wallLights[index],
                        x,
                        topY,
                        scale,
                        invScale,
                        midTextureAlt,
                        ceilClip,
                        floorClip);

                    clipData[drawSeg.MaskedTextureColumn + x] = short.MaxValue;
                }

                scale += scaleStep;
            }
        }



        private void DrawCeilingColumn(
            Sector sector,
            Flat flat,
            byte[][] planeLights,
            int x,
            int y1,
            int y2)
        {
            if (flat == flats.SkyFlat)
            {
                DrawSkyColumn(x, y1, y2);
                return;
            }

            if (y2 - y1 < 0)
            {
                return;
            }

            var height = Fixed.Abs(sector.CeilingHeight - viewZ);

            var flatData = flat.Data;

            if (sector == ceilingPrevSector && ceilingPrevX == x - 1)
            {
                var p1 = Math.Max(y1, ceilingPrevY1);
                var p2 = Math.Min(y2, ceilingPrevY2);

                var pos = screenHeight * (windowX + x) + windowY + y1;

                for (var y = y1; y < p1; y++)
                {
                    var distance = height * planeYSlope[y];
                    ceilingXStep[y] = distance * planeBaseXScale;
                    ceilingYStep[y] = distance * planeBaseYScale;

                    var length = distance * planeDistScale[x];
                    var angle = viewAngle + xToAngle[x];
                    var xFrac = viewX + Trig.Cos(angle) * length;
                    var yFrac = -viewY - Trig.Sin(angle) * length;
                    ceilingXFrac[y] = xFrac;
                    ceilingYFrac[y] = yFrac;

                    var colorMap = planeLights[Math.Min((uint)(distance.Data >> zLightShift), maxZLight - 1)];
                    ceilingLights[y] = colorMap;

                    var spot = ((yFrac.Data >> (16 - 6)) & (63 * 64)) + ((xFrac.Data >> 16) & 63);
                    screenData[pos] = colorMap[flatData[spot]];
                    pos++;
                }

                for (var y = p1; y <= p2; y++)
                {
                    var xFrac = ceilingXFrac[y] + ceilingXStep[y];
                    var yFrac = ceilingYFrac[y] + ceilingYStep[y];

                    var spot = ((yFrac.Data >> (16 - 6)) & (63 * 64)) + ((xFrac.Data >> 16) & 63);
                    screenData[pos] = ceilingLights[y][flatData[spot]];
                    pos++;

                    ceilingXFrac[y] = xFrac;
                    ceilingYFrac[y] = yFrac;
                }

                for (var y = p2 + 1; y <= y2; y++)
                {
                    var distance = height * planeYSlope[y];
                    ceilingXStep[y] = distance * planeBaseXScale;
                    ceilingYStep[y] = distance * planeBaseYScale;

                    var length = distance * planeDistScale[x];
                    var angle = viewAngle + xToAngle[x];
                    var xFrac = viewX + Trig.Cos(angle) * length;
                    var yFrac = -viewY - Trig.Sin(angle) * length;
                    ceilingXFrac[y] = xFrac;
                    ceilingYFrac[y] = yFrac;

                    var colorMap = planeLights[Math.Min((uint)(distance.Data >> zLightShift), maxZLight - 1)];
                    ceilingLights[y] = colorMap;

                    var spot = ((yFrac.Data >> (16 - 6)) & (63 * 64)) + ((xFrac.Data >> 16) & 63);
                    screenData[pos] = colorMap[flatData[spot]];
                    pos++;
                }
            }
            else
            {
                var pos = screenHeight * (windowX + x) + windowY + y1;

                for (var y = y1; y <= y2; y++)
                {
                    var distance = height * planeYSlope[y];
                    ceilingXStep[y] = distance * planeBaseXScale;
                    ceilingYStep[y] = distance * planeBaseYScale;

                    var length = distance * planeDistScale[x];
                    var angle = viewAngle + xToAngle[x];
                    var xFrac = viewX + Trig.Cos(angle) * length;
                    var yFrac = -viewY - Trig.Sin(angle) * length;
                    ceilingXFrac[y] = xFrac;
                    ceilingYFrac[y] = yFrac;

                    var colorMap = planeLights[Math.Min((uint)(distance.Data >> zLightShift), maxZLight - 1)];
                    ceilingLights[y] = colorMap;

                    var spot = ((yFrac.Data >> (16 - 6)) & (63 * 64)) + ((xFrac.Data >> 16) & 63);
                    screenData[pos] = colorMap[flatData[spot]];
                    pos++;
                }
            }

            ceilingPrevSector = sector;
            ceilingPrevX = x;
            ceilingPrevY1 = y1;
            ceilingPrevY2 = y2;
        }

        private void DrawFloorColumn(
            Sector sector,
            Flat flat,
            byte[][] planeLights,
            int x,
            int y1,
            int y2)
        {
            if (flat == flats.SkyFlat)
            {
                DrawSkyColumn(x, y1, y2);
                return;
            }

            if (y2 - y1 < 0)
            {
                return;
            }

            var height = Fixed.Abs(sector.FloorHeight - viewZ);

            var flatData = flat.Data;

            if (sector == floorPrevSector && floorPrevX == x - 1)
            {
                var p1 = Math.Max(y1, floorPrevY1);
                var p2 = Math.Min(y2, floorPrevY2);

                var pos = screenHeight * (windowX + x) + windowY + y1;

                for (var y = y1; y < p1; y++)
                {
                    var distance = height * planeYSlope[y];
                    floorXStep[y] = distance * planeBaseXScale;
                    floorYStep[y] = distance * planeBaseYScale;

                    var length = distance * planeDistScale[x];
                    var angle = viewAngle + xToAngle[x];
                    var xFrac = viewX + Trig.Cos(angle) * length;
                    var yFrac = -viewY - Trig.Sin(angle) * length;
                    floorXFrac[y] = xFrac;
                    floorYFrac[y] = yFrac;

                    var colorMap = planeLights[Math.Min((uint)(distance.Data >> zLightShift), maxZLight - 1)];
                    floorLights[y] = colorMap;

                    var spot = ((yFrac.Data >> (16 - 6)) & (63 * 64)) + ((xFrac.Data >> 16) & 63);
                    screenData[pos] = colorMap[flatData[spot]];
                    pos++;
                }

                for (var y = p1; y <= p2; y++)
                {
                    var xFrac = floorXFrac[y] + floorXStep[y];
                    var yFrac = floorYFrac[y] + floorYStep[y];

                    var spot = ((yFrac.Data >> (16 - 6)) & (63 * 64)) + ((xFrac.Data >> 16) & 63);
                    screenData[pos] = floorLights[y][flatData[spot]];
                    pos++;

                    floorXFrac[y] = xFrac;
                    floorYFrac[y] = yFrac;
                }

                for (var y = p2 + 1; y <= y2; y++)
                {
                    var distance = height * planeYSlope[y];
                    floorXStep[y] = distance * planeBaseXScale;
                    floorYStep[y] = distance * planeBaseYScale;

                    var length = distance * planeDistScale[x];
                    var angle = viewAngle + xToAngle[x];
                    var xFrac = viewX + Trig.Cos(angle) * length;
                    var yFrac = -viewY - Trig.Sin(angle) * length;
                    floorXFrac[y] = xFrac;
                    floorYFrac[y] = yFrac;

                    var colorMap = planeLights[Math.Min((uint)(distance.Data >> zLightShift), maxZLight - 1)];
                    floorLights[y] = colorMap;

                    var spot = ((yFrac.Data >> (16 - 6)) & (63 * 64)) + ((xFrac.Data >> 16) & 63);
                    screenData[pos] = colorMap[flatData[spot]];
                    pos++;
                }
            }
            else
            {
                var pos = screenHeight * (windowX + x) + windowY + y1;

                for (var y = y1; y <= y2; y++)
                {
                    var distance = height * planeYSlope[y];
                    floorXStep[y] = distance * planeBaseXScale;
                    floorYStep[y] = distance * planeBaseYScale;

                    var length = distance * planeDistScale[x];
                    var angle = viewAngle + xToAngle[x];
                    var xFrac = viewX + Trig.Cos(angle) * length;
                    var yFrac = -viewY - Trig.Sin(angle) * length;
                    floorXFrac[y] = xFrac;
                    floorYFrac[y] = yFrac;

                    var colorMap = planeLights[Math.Min((uint)(distance.Data >> zLightShift), maxZLight - 1)];
                    floorLights[y] = colorMap;

                    var spot = ((yFrac.Data >> (16 - 6)) & (63 * 64)) + ((xFrac.Data >> 16) & 63);
                    screenData[pos] = colorMap[flatData[spot]];
                    pos++;
                }
            }

            floorPrevSector = sector;
            floorPrevX = x;
            floorPrevY1 = y1;
            floorPrevY2 = y2;
        }



        private void DrawColumn(
            Column column,
            byte[] map,
            int x,
            int y1,
            int y2,
            Fixed invScale,
            Fixed textureAlt)
        {
            if (y2 - y1 < 0)
            {
                return;
            }

            // Framebuffer destination address.
            // Use ylookup LUT to avoid multiply with ScreenWidth.
            // Use columnofs LUT for subwindows? 
            var pos1 = screenHeight * (windowX + x) + windowY + y1;
            var pos2 = pos1 + (y2 - y1);

            // Determine scaling, which is the only mapping to be done.
            var fracStep = invScale;
            var frac = textureAlt + (y1 - centerY) * fracStep;

            // Inner loop that does the actual texture mapping,
            // e.g. a DDA-lile scaling.
            // This is as fast as it gets.
            var source = column.Data;
            var offset = column.Offset;
            for (var pos = pos1; pos <= pos2; pos++)
            {
                // Re-map color indices from wall texture column
                // using a lighting/special effects LUT.
                screenData[pos] = map[source[offset + ((frac.Data >> Fixed.FracBits) & 127)]];
                frac += fracStep;
            }
        }

        private void DrawColumnTranslation(
            Column column,
            byte[] translation,
            byte[] map,
            int x,
            int y1,
            int y2,
            Fixed invScale,
            Fixed textureAlt)
        {
            if (y2 - y1 < 0)
            {
                return;
            }

            // Framebuffer destination address.
            // Use ylookup LUT to avoid multiply with ScreenWidth.
            // Use columnofs LUT for subwindows? 
            var pos1 = screenHeight * (windowX + x) + windowY + y1;
            var pos2 = pos1 + (y2 - y1);

            // Determine scaling, which is the only mapping to be done.
            var fracStep = invScale;
            var frac = textureAlt + (y1 - centerY) * fracStep;

            // Inner loop that does the actual texture mapping,
            // e.g. a DDA-lile scaling.
            // This is as fast as it gets.
            var source = column.Data;
            var offset = column.Offset;
            for (var pos = pos1; pos <= pos2; pos++)
            {
                // Re-map color indices from wall texture column
                // using a lighting/special effects LUT.
                screenData[pos] = map[translation[source[offset + ((frac.Data >> Fixed.FracBits) & 127)]]];
                frac += fracStep;
            }
        }

        private void DrawFuzzColumn(
            Column column,
            int x,
            int y1,
            int y2)
        {
            if (y2 - y1 < 0)
            {
                return;
            }

            if (y1 == 0)
            {
                y1 = 1;
            }

            if (y2 == windowHeight - 1)
            {
                y2 = windowHeight - 2;
            }

            var pos1 = screenHeight * (windowX + x) + windowY + y1;
            var pos2 = pos1 + (y2 - y1);

            var map = colorMap[6];
            for (var pos = pos1; pos <= pos2; pos++)
            {
                screenData[pos] = map[screenData[pos + fuzzTable[fuzzPos]]];

                if (++fuzzPos == fuzzTable.Length)
                {
                    fuzzPos = 0;
                }
            }
        }

        private void DrawSkyColumn(int x, int y1, int y2)
        {
            var angle = (viewAngle + xToAngle[x]).Data >> angleToSkyShift;
            var mask = world.Map.SkyTexture.Width - 1;
            var source = world.Map.SkyTexture.Composite.Columns[angle & mask];
            DrawColumn(source[0], colorMap[0], x, y1, y2, skyInvScale, skyTextureAlt);
        }

        private void DrawMaskedColumn(
            Column[] columns,
            byte[] map,
            int x,
            Fixed topY,
            Fixed scale,
            Fixed invScale,
            Fixed textureAlt,
            int upperClip,
            int lowerClip)
        {
            foreach (var column in columns)
            {
                var y1Frac = topY + scale * column.TopDelta;
                var y2Frac = y1Frac + scale * column.Length;
                var y1 = (y1Frac.Data + Fixed.FracUnit - 1) >> Fixed.FracBits;
                var y2 = (y2Frac.Data - 1) >> Fixed.FracBits;

                y1 = Math.Max(y1, upperClip + 1);
                y2 = Math.Min(y2, lowerClip - 1);

                if (y1 <= y2)
                {
                    var alt = new Fixed(textureAlt.Data - (column.TopDelta << Fixed.FracBits));
                    DrawColumn(column, map, x, y1, y2, invScale, alt);
                }
            }
        }

        private void DrawMaskedColumnTranslation(
            Column[] columns,
            byte[] translation,
            byte[] map,
            int x,
            Fixed topY,
            Fixed scale,
            Fixed invScale,
            Fixed textureAlt,
            int upperClip,
            int lowerClip)
        {
            foreach (var column in columns)
            {
                var y1Frac = topY + scale * column.TopDelta;
                var y2Frac = y1Frac + scale * column.Length;
                var y1 = (y1Frac.Data + Fixed.FracUnit - 1) >> Fixed.FracBits;
                var y2 = (y2Frac.Data - 1) >> Fixed.FracBits;

                y1 = Math.Max(y1, upperClip + 1);
                y2 = Math.Min(y2, lowerClip - 1);

                if (y1 <= y2)
                {
                    var alt = new Fixed(textureAlt.Data - (column.TopDelta << Fixed.FracBits));
                    DrawColumnTranslation(column, translation, map, x, y1, y2, invScale, alt);
                }
            }
        }

        private void DrawMaskedFuzzColumn(
            Column[] columns,
            int x,
            Fixed topY,
            Fixed scale,
            int upperClip,
            int lowerClip)
        {
            foreach (var column in columns)
            {
                var y1Frac = topY + scale * column.TopDelta;
                var y2Frac = y1Frac + scale * column.Length;
                var y1 = (y1Frac.Data + Fixed.FracUnit - 1) >> Fixed.FracBits;
                var y2 = (y2Frac.Data - 1) >> Fixed.FracBits;

                y1 = Math.Max(y1, upperClip + 1);
                y2 = Math.Min(y2, lowerClip - 1);

                if (y1 <= y2)
                {
                    DrawFuzzColumn(column, x, y1, y2);
                }
            }
        }



        private void AddSprites(Sector sector, int validCount)
        {
            // BSP is traversed by subsector.
            // A sector might have been split into several subsectors during BSP building.
            // Thus we check whether its already added.
            if (sector.ValidCount == validCount)
            {
                return;
            }

            // Well, now it will be done.
            sector.ValidCount = validCount;

            var spriteLightLevel = (sector.LightLevel >> lightSegShift) + extraLight;
            var spriteLights = scaleLight[Math.Clamp(spriteLightLevel, 0, lightLevelCount - 1)];

            // Handle all things in sector.
            foreach (var thing in sector)
            {
                ProjectSprite(thing, spriteLights);
            }
        }

        private void ProjectSprite(Mobj thing, byte[][] spriteLights)
        {
            if (visSpriteCount == visSprites.Length)
            {
                // Too many sprites.
                return;
            }

            // Transform the origin point.
            var trX = thing.X - viewX;
            var trY = thing.Y - viewY;

            var gxt = (trX * viewCos);
            var gyt = -(trY * viewSin);

            var tz = gxt - gyt;

            // Thing is behind view plane?
            if (tz < minZ)
            {
                return;
            }

            var xScale = projection / tz;

            gxt = -trX * viewSin;
            gyt = trY * viewCos;
            var tx = -(gyt + gxt);

            // Too far off the side?
            if (Fixed.Abs(tx) > (tz << 2))
            {
                return;
            }

            var spriteDef = sprites[thing.Sprite];
            var frameNumber = thing.Frame & 0x7F;
            var spriteFrame = spriteDef.Frames[frameNumber];

            Patch lump;
            bool flip;
            if (spriteFrame.Rotate)
            {
                // Choose a different rotation based on player view.
                var ang = Geometry.PointToAngle(viewX, viewY, thing.X, thing.Y);
                var rot = (ang.Data - thing.Angle.Data + (uint)(Angle.Ang45.Data / 2) * 9) >> 29;
                lump = spriteFrame.Patches[rot];
                flip = spriteFrame.Flip[rot];
            }
            else
            {
                // Use single rotation for all views.
                lump = spriteFrame.Patches[0];
                flip = spriteFrame.Flip[0];
            }

            // Calculate edges of the shape.
            tx -= Fixed.FromInt(lump.LeftOffset);
            var x1 = (centerXFrac + (tx * xScale)).Data >> Fixed.FracBits;

            // Off the right side?
            if (x1 > windowWidth)
            {
                return;
            }

            tx += Fixed.FromInt(lump.Width);
            var x2 = ((centerXFrac + (tx * xScale)).Data >> Fixed.FracBits) - 1;

            // Off the left side?
            if (x2 < 0)
            {
                return;
            }

            // Store information in a vissprite.
            var vis = visSprites[visSpriteCount];
            visSpriteCount++;

            vis.MobjFlags = thing.Flags;
            vis.Scale = xScale;
            vis.GlobalX = thing.X;
            vis.GlobalY = thing.Y;
            vis.GlobalBottomZ = thing.Z;
            vis.GlobalTopZ = thing.Z + Fixed.FromInt(lump.TopOffset);
            vis.TextureAlt = vis.GlobalTopZ - viewZ;
            vis.X1 = x1 < 0 ? 0 : x1;
            vis.X2 = x2 >= windowWidth ? windowWidth - 1 : x2;

            var invScale = Fixed.One / xScale;

            if (flip)
            {
                vis.StartFrac = new Fixed(Fixed.FromInt(lump.Width).Data - 1);
                vis.InvScale = -invScale;
            }
            else
            {
                vis.StartFrac = Fixed.Zero;
                vis.InvScale = invScale;
            }

            if (vis.X1 > x1)
            {
                vis.StartFrac += vis.InvScale * (vis.X1 - x1);
            }

            vis.Patch = lump;

            if (fixedColorMap == 0)
            {
                if ((thing.Frame & 0x8000) == 0)
                {
                    vis.ColorMap = spriteLights[Math.Min(xScale.Data >> scaleLightShift, maxScaleLight - 1)];
                }
                else
                {
                    vis.ColorMap = colorMap.FullBright;
                }
            }
            else
            {
                vis.ColorMap = colorMap[fixedColorMap];
            }
        }

        private void RenderSprites()
        {
            Array.Sort(visSprites, 0, visSpriteCount, visSpriteComparer);

            for (var i = visSpriteCount - 1; i >= 0; i--)
            {
                DrawSprite(visSprites[i]);
            }
        }

        private void DrawSprite(VisSprite sprite)
        {
            for (var x = sprite.X1; x <= sprite.X2; x++)
            {
                lowerClip[x] = -2;
                upperClip[x] = -2;
            }

            // Scan drawsegs from end to start for obscuring segs.
            // The first drawseg that has a greater scale is the clip seg.
            for (var i = visWallRangeCount - 1; i >= 0; i--)
            {
                var wall = visWallRanges[i];

                // Determine if the drawseg obscures the sprite.
                if (wall.X1 > sprite.X2 ||
                    wall.X2 < sprite.X1 ||
                    (wall.Silhouette == 0 && wall.MaskedTextureColumn == -1))
                {
                    // Does not cover sprite.
                    continue;
                }

                var r1 = wall.X1 < sprite.X1 ? sprite.X1 : wall.X1;
                var r2 = wall.X2 > sprite.X2 ? sprite.X2 : wall.X2;

                Fixed lowScale;
                Fixed scale;
                if (wall.Scale1 > wall.Scale2)
                {
                    lowScale = wall.Scale2;
                    scale = wall.Scale1;
                }
                else
                {
                    lowScale = wall.Scale1;
                    scale = wall.Scale2;
                }

                if (scale < sprite.Scale ||
                    (lowScale < sprite.Scale &&
                        Geometry.PointOnSegSide(sprite.GlobalX, sprite.GlobalY, wall.Seg) == 0))
                {
                    // Masked mid texture?
                    if (wall.MaskedTextureColumn != -1)
                    {
                        DrawMaskedRange(wall, r1, r2);
                    }
                    // Seg is behind sprite.
                    continue;
                }

                // Clip this piece of the sprite.
                var silhouette = wall.Silhouette;

                if (sprite.GlobalBottomZ >= wall.LowerSilHeight)
                {
                    silhouette &= ~Silhouette.Lower;
                }

                if (sprite.GlobalTopZ <= wall.UpperSilHeight)
                {
                    silhouette &= ~Silhouette.Upper;
                }

                if (silhouette == Silhouette.Lower)
                {
                    // Bottom sil.
                    for (var x = r1; x <= r2; x++)
                    {
                        if (lowerClip[x] == -2)
                        {
                            lowerClip[x] = clipData[wall.LowerClip + x];
                        }
                    }
                }
                else if (silhouette == Silhouette.Upper)
                {
                    // Top sil.
                    for (var x = r1; x <= r2; x++)
                    {
                        if (upperClip[x] == -2)
                        {
                            upperClip[x] = clipData[wall.UpperClip + x];
                        }
                    }
                }
                else if (silhouette == Silhouette.Both)
                {
                    // Both.
                    for (var x = r1; x <= r2; x++)
                    {
                        if (lowerClip[x] == -2)
                        {
                            lowerClip[x] = clipData[wall.LowerClip + x];
                        }
                        if (upperClip[x] == -2)
                        {
                            upperClip[x] = clipData[wall.UpperClip + x];
                        }
                    }
                }
            }

            // All clipping has been performed, so draw the sprite.

            // Check for unclipped columns.
            for (var x = sprite.X1; x <= sprite.X2; x++)
            {
                if (lowerClip[x] == -2)
                {
                    lowerClip[x] = (short)windowHeight;
                }
                if (upperClip[x] == -2)
                {
                    upperClip[x] = -1;
                }
            }

            if ((sprite.MobjFlags & MobjFlags.Shadow) != 0)
            {
                var frac = sprite.StartFrac;
                for (var x = sprite.X1; x <= sprite.X2; x++)
                {
                    var textureColumn = frac.ToIntFloor();
                    DrawMaskedFuzzColumn(
                        sprite.Patch.Columns[textureColumn],
                        x,
                        centerYFrac - (sprite.TextureAlt * sprite.Scale),
                        sprite.Scale,
                        upperClip[x],
                        lowerClip[x]);
                    frac += sprite.InvScale;
                }
            }
            else if (((int)(sprite.MobjFlags & MobjFlags.Translation) >> (int)MobjFlags.TransShift) != 0)
            {
                byte[] translation;
                switch (((int)(sprite.MobjFlags & MobjFlags.Translation) >> (int)MobjFlags.TransShift))
                {
                    case 1:
                        translation = greenToGray;
                        break;
                    case 2:
                        translation = greenToBrown;
                        break;
                    default:
                        translation = greenToRed;
                        break;
                }
                var frac = sprite.StartFrac;
                for (var x = sprite.X1; x <= sprite.X2; x++)
                {
                    var textureColumn = frac.ToIntFloor();
                    DrawMaskedColumnTranslation(
                        sprite.Patch.Columns[textureColumn],
                        translation,
                        sprite.ColorMap,
                        x,
                        centerYFrac - (sprite.TextureAlt * sprite.Scale),
                        sprite.Scale,
                        Fixed.Abs(sprite.InvScale),
                        sprite.TextureAlt,
                        upperClip[x],
                        lowerClip[x]);
                    frac += sprite.InvScale;
                }
            }
            else
            {
                var frac = sprite.StartFrac;
                for (var x = sprite.X1; x <= sprite.X2; x++)
                {
                    var textureColumn = frac.ToIntFloor();
                    DrawMaskedColumn(
                        sprite.Patch.Columns[textureColumn],
                        sprite.ColorMap,
                        x,
                        centerYFrac - (sprite.TextureAlt * sprite.Scale),
                        sprite.Scale,
                        Fixed.Abs(sprite.InvScale),
                        sprite.TextureAlt,
                        upperClip[x],
                        lowerClip[x]);
                    frac += sprite.InvScale;
                }
            }
        }



        private void DrawPlayerSprite(PlayerSpriteDef psp, byte[][] spriteLights, bool fuzz)
        {
            // Decide which patch to use.
            var spriteDef = sprites[psp.State.Sprite];

            var spriteFrame = spriteDef.Frames[psp.State.Frame & 0x7fff];

            var lump = spriteFrame.Patches[0];
            var flip = spriteFrame.Flip[0];

            // Calculate edges of the shape.
            var tx = psp.Sx - Fixed.FromInt(160);
            tx -= Fixed.FromInt(lump.LeftOffset);
            var x1 = (centerXFrac + tx * weaponScale).Data >> Fixed.FracBits;

            // Off the right side?
            if (x1 > windowWidth)
            {
                return;
            }

            tx += Fixed.FromInt(lump.Width);
            var x2 = ((centerXFrac + tx * weaponScale).Data >> Fixed.FracBits) - 1;

            // Off the left side?
            if (x2 < 0)
            {
                return;
            }

            // Store information in a vissprite.
            var vis = weaponSprite;
            vis.MobjFlags = 0;
            // The code below is based on Crispy Doom's weapon rendering code.
            vis.TextureAlt = Fixed.FromInt(100) + Fixed.One / 4 - (psp.Sy - Fixed.FromInt(lump.TopOffset));
            vis.X1 = x1 < 0 ? 0 : x1;
            vis.X2 = x2 >= windowWidth ? windowWidth - 1 : x2;
            vis.Scale = weaponScale;

            if (flip)
            {
                vis.InvScale = -weaponInvScale;
                vis.StartFrac = Fixed.FromInt(lump.Width) - new Fixed(1);
            }
            else
            {
                vis.InvScale = weaponInvScale;
                vis.StartFrac = Fixed.Zero;
            }

            if (vis.X1 > x1)
            {
                vis.StartFrac += vis.InvScale * (vis.X1 - x1);
            }

            vis.Patch = lump;

            if (fixedColorMap == 0)
            {
                if ((psp.State.Frame & 0x8000) == 0)
                {
                    vis.ColorMap = spriteLights[maxScaleLight - 1];
                }
                else
                {
                    vis.ColorMap = colorMap.FullBright;
                }
            }
            else
            {
                vis.ColorMap = colorMap[fixedColorMap];
            }

            if (fuzz)
            {
                var frac = vis.StartFrac;
                for (var x = vis.X1; x <= vis.X2; x++)
                {
                    var texturecolumn = frac.Data >> Fixed.FracBits;
                    DrawMaskedFuzzColumn(
                        vis.Patch.Columns[texturecolumn],
                        x,
                        centerYFrac - (vis.TextureAlt * vis.Scale),
                        vis.Scale,
                        -1,
                        windowHeight);
                    frac += vis.InvScale;
                }
            }
            else
            {
                var frac = vis.StartFrac;
                for (var x = vis.X1; x <= vis.X2; x++)
                {
                    var texturecolumn = frac.Data >> Fixed.FracBits;
                    DrawMaskedColumn(
                        vis.Patch.Columns[texturecolumn],
                        vis.ColorMap,
                        x,
                        centerYFrac - (vis.TextureAlt * vis.Scale),
                        vis.Scale,
                        Fixed.Abs(vis.InvScale),
                        vis.TextureAlt,
                        -1,
                        windowHeight);
                    frac += vis.InvScale;
                }
            }
        }



        private void DrawPlayerSprites(Player player)
        {
            // Get light level.
            var spriteLightLevel = (player.Mobj.Subsector.Sector.LightLevel >> lightSegShift) + extraLight;

            byte[][] spriteLights;
            if (spriteLightLevel < 0)
            {
                spriteLights = scaleLight[0];
            }
            else if (spriteLightLevel >= lightLevelCount)
            {
                spriteLights = scaleLight[lightLevelCount - 1];
            }
            else
            {
                spriteLights = scaleLight[spriteLightLevel];
            }

            bool fuzz;
            if (player.Powers[(int)PowerType.Invisibility] > 4 * 32 ||
                (player.Powers[(int)PowerType.Invisibility] & 8) != 0)
            {
                // Shadow draw.
                fuzz = true;
            }
            else
            {
                fuzz = false;
            }

            // Add all active psprites.
            for (var i = 0; i < (int)PlayerSprite.Count; i++)
            {
                var psp = player.PlayerSprites[i];
                if (psp.State != null)
                {
                    DrawPlayerSprite(psp, spriteLights, fuzz);
                }
            }
        }



        public int WindowSize
        {
            get
            {
                return windowSize;
            }

            set
            {
                windowSize = value;
                SetWindowSize(windowSize);
            }
        }



        private class ClipRange
        {
            public int First;
            public int Last;

            public void CopyFrom(ClipRange range)
            {
                First = range.First;
                Last = range.Last;
            }
        }

        private class VisWallRange
        {
            public Seg Seg;

            public int X1;
            public int X2;

            public Fixed Scale1;
            public Fixed Scale2;
            public Fixed ScaleStep;

            public Silhouette Silhouette;
            public Fixed UpperSilHeight;
            public Fixed LowerSilHeight;

            public int UpperClip;
            public int LowerClip;
            public int MaskedTextureColumn;
        }

        [Flags]
        private enum Silhouette
        {
            Upper = 1,
            Lower = 2,
            Both = 3
        }

        private class VisSprite
        {
            public int X1;
            public int X2;

            // For line side calculation.
            public Fixed GlobalX;
            public Fixed GlobalY;

            // Global bottom / top for silhouette clipping.
            public Fixed GlobalBottomZ;
            public Fixed GlobalTopZ;

            // Horizontal position of x1.
            public Fixed StartFrac;

            public Fixed Scale;

            // Negative if flipped.
            public Fixed InvScale;

            public Fixed TextureAlt;
            public Patch Patch;

            // For color translation and shadow draw.
            public byte[] ColorMap;

            public MobjFlags MobjFlags;
        }

        private class VisSpriteComparer : IComparer<VisSprite>
        {
            public int Compare(VisSprite x, VisSprite y)
            {
                return y.Scale.Data - x.Scale.Data;
            }
        }
    }
}
