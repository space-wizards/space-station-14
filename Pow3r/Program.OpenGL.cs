using System;
using System.Numerics;
using System.Text;
using System.Linq;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Pow3r
{
    internal sealed unsafe partial class Program
    {
        private int _glFontTexture;

        private void InitOpenGL()
        {
            if (GL.GetString(StringName.Extensions).Split(' ').Contains("GL_ARB_debug_output"))
                GL.Arb.DebugMessageCallback(GLDebugCallbackDelegate, 0x0105);

            GL.Enable(EnableCap.ScissorTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFuncSeparate(
                BlendingFactorSrc.SrcAlpha,
                BlendingFactorDest.OneMinusSrcAlpha,
                BlendingFactorSrc.One,
                BlendingFactorDest.OneMinusSrcAlpha);

            var io = ImGui.GetIO();

            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out var width, out var height, out _);

            _glFontTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _glFontTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, (nint) pixels);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);

            /*
            GL.TextureParameter(_fontTexture, TextureParameterName.TextureSwizzleR, 1);
            GL.TextureParameter(_fontTexture, TextureParameterName.TextureSwizzleG, 1);
            GL.TextureParameter(_fontTexture, TextureParameterName.TextureSwizzleB, 1);
            GL.TextureParameter(_fontTexture, TextureParameterName.TextureSwizzleA, (int) All.Red);*/

            io.Fonts.SetTexID(_glFontTexture);
            io.Fonts.ClearTexData();
        }

        private void RenderOpenGL()
        {
            GLFW.GetFramebufferSize(_window.WindowPtr, out var fbW, out var fbH);
            GL.Viewport(0, 0, fbW, fbH);
            GL.Disable(EnableCap.ScissorTest);
            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Enable(EnableCap.ScissorTest);
            GL.Enable(EnableCap.Texture2D);

            var drawData = ImGui.GetDrawData();

            var l = drawData.DisplayPos.X;
            var r = drawData.DisplayPos.X + drawData.DisplaySize.X;
            var t = drawData.DisplayPos.Y;
            var b = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

            var matrix = Matrix4x4.CreateOrthographicOffCenter(l, r, b, t, -1, 1);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix((float*) &matrix);

            var clipOff = drawData.DisplayPos;
            var clipScale = drawData.FramebufferScale;

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            GL.EnableClientState(ArrayCap.ColorArray);

            for (var n = 0; n < drawData.CmdListsCount; n++)
            {
                var drawList = drawData.CmdListsRange[n];

                for (var cmdI = 0; cmdI < drawList.CmdBuffer.Size; cmdI++)
                {
                    var cmd = drawList.CmdBuffer[cmdI];

                    GL.BindTexture(TextureTarget.Texture2D, (uint) cmd.TextureId);

                    Vector4 clipRect = default;
                    clipRect.X = (cmd.ClipRect.X - clipOff.X) * clipScale.X;
                    clipRect.Y = (cmd.ClipRect.Y - clipOff.Y) * clipScale.Y;
                    clipRect.Z = (cmd.ClipRect.Z - clipOff.X) * clipScale.X;
                    clipRect.W = (cmd.ClipRect.W - clipOff.Y) * clipScale.Y;

                    GL.Scissor((int) clipRect.X, (int) (fbH - clipRect.W), (int) (clipRect.Z - clipRect.X),
                        (int) (clipRect.W - clipRect.Y));

                    IntPtr adjustedVB = drawList.VtxBuffer.Data + (nint) (sizeof(ImDrawVert) * cmd.VtxOffset);

                    GL.VertexPointer(2, VertexPointerType.Float, sizeof(ImDrawVert), adjustedVB);
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, sizeof(ImDrawVert), adjustedVB + 8);
                    GL.ColorPointer(4, ColorPointerType.UnsignedByte, sizeof(ImDrawVert), adjustedVB + 16);

                    GL.DrawElements(PrimitiveType.Triangles, (int) cmd.ElemCount,
                        DrawElementsType.UnsignedShort,
                        drawList.IdxBuffer.Data + (nint) (cmd.IdxOffset * 2));
                }
            }

            _window.SwapBuffers();
        }

        private static readonly DebugProcArb GLDebugCallbackDelegate = GLDebugCallback;

        private static void GLDebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity,
            int length, IntPtr message, IntPtr userParam)
        {
            var msg = Encoding.UTF8.GetString((byte*) message, length);

            if (severity == DebugSeverity.DebugSeverityNotification)
                return;

            Console.WriteLine($"[{type}][{severity}] {source}: {msg}");
        }
    }
}
