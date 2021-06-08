using System;
using System.Numerics;
using System.Text;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Pow3r
{
    internal sealed unsafe partial class Program
    {
        private const string GLFragmentShader = @"
#version 460
in vec2 Frag_UV;
in vec4 Frag_Color;
uniform sampler2D Texture;
layout (location = 0) out vec4 Out_Color;
void main()
{
    Out_Color = Frag_Color * texture(Texture, Frag_UV.st);
}";

        private const string GLVertexShader = @"
#version 460
layout (location = 0) in vec2 Position;
layout (location = 1) in vec2 UV;
layout (location = 2) in vec4 Color;
uniform mat4 ProjMtx;
out vec2 Frag_UV;
out vec4 Frag_Color;
void main()
{
    Frag_UV = UV;
    Frag_Color = Color;
    gl_Position = ProjMtx * vec4(Position.xy,0,1);
};";

        private int _glShaderProgram;
        private int _glFontTexture;
        private int _glVao;
        private int _glVbo;
        private int _glEbo;

        private int _glUniformTexture;
        private int _glUniformProjMatrix;

        private void InitOpenGL()
        {
            GL.Enable(EnableCap.DebugOutput);
            GL.DebugMessageCallback(GLDebugCallbackDelegate, (nint) 0x3005);

            GL.Enable(EnableCap.ScissorTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFuncSeparate(
                BlendingFactorSrc.SrcAlpha,
                BlendingFactorDest.OneMinusSrcAlpha,
                BlendingFactorSrc.One,
                BlendingFactorDest.OneMinusSrcAlpha);

            var frag = GL.CreateShader(ShaderType.FragmentShader);
            var vert = GL.CreateShader(ShaderType.VertexShader);

            GL.ShaderSource(frag, GLFragmentShader);
            GL.ShaderSource(vert, GLVertexShader);

            GL.CompileShader(frag);
            GL.CompileShader(vert);

            GL.GetShader(frag, ShaderParameter.CompileStatus, out var status);
            if (status != 1)
            {
                var log = GL.GetShaderInfoLog(frag);
                throw new Exception($"Shader failed to compile: {log}");
            }

            GL.GetShader(vert, ShaderParameter.CompileStatus, out status);
            if (status != 1)
            {
                var log = GL.GetShaderInfoLog(vert);
                throw new Exception($"Shader failed to compile: {log}");
            }

            _glShaderProgram = GL.CreateProgram();
            GL.AttachShader(_glShaderProgram, vert);
            GL.AttachShader(_glShaderProgram, frag);

            GL.LinkProgram(_glShaderProgram);

            GL.GetProgram(_glShaderProgram, GetProgramParameterName.LinkStatus, out status);
            if (status != 1)
            {
                var log = GL.GetProgramInfoLog(_glShaderProgram);
                throw new Exception($"Shader failed to link: {log}");
            }

            GL.DeleteShader(frag);
            GL.DeleteShader(vert);

            _glUniformProjMatrix = GL.GetUniformLocation(_glShaderProgram, "ProjMtx");
            _glUniformTexture = GL.GetUniformLocation(_glShaderProgram, "Texture");

            var io = ImGui.GetIO();

            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out var width, out var height, out _);

            GL.CreateTextures(TextureTarget.Texture2D, 1, out _glFontTexture);
            GL.TextureStorage2D(_glFontTexture, 1, SizedInternalFormat.Rgba8, width, height);
            GL.TextureSubImage2D(_glFontTexture, 0, 0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte,
                (nint) pixels);
            GL.TextureParameter(_glFontTexture, TextureParameterName.TextureMaxLevel, 0);

            /*
            GL.TextureParameter(_fontTexture, TextureParameterName.TextureSwizzleR, 1);
            GL.TextureParameter(_fontTexture, TextureParameterName.TextureSwizzleG, 1);
            GL.TextureParameter(_fontTexture, TextureParameterName.TextureSwizzleB, 1);
            GL.TextureParameter(_fontTexture, TextureParameterName.TextureSwizzleA, (int) All.Red);*/

            io.Fonts.SetTexID((nint) _glFontTexture);
            io.Fonts.ClearTexData();

            // Buffers.
            GL.CreateBuffers(1, out _glVbo);
            GL.CreateBuffers(1, out _glEbo);

            GL.CreateVertexArrays(1, out _glVao);
            GL.VertexArrayVertexBuffer(_glVao, 0, _glVbo, (nint) 0, sizeof(ImDrawVert));
            GL.VertexArrayElementBuffer(_glVao, _glEbo);

            GL.EnableVertexArrayAttrib(_glVao, 0);
            GL.VertexArrayAttribBinding(_glVao, 0, 0);
            GL.VertexArrayAttribFormat(_glVao, 0, 2, VertexAttribType.Float, false, 0);

            GL.EnableVertexArrayAttrib(_glVao, 1);
            GL.VertexArrayAttribBinding(_glVao, 1, 0);
            GL.VertexArrayAttribFormat(_glVao, 1, 2, VertexAttribType.Float, false, 8);

            GL.EnableVertexArrayAttrib(_glVao, 2);
            GL.VertexArrayAttribBinding(_glVao, 2, 0);
            GL.VertexArrayAttribFormat(_glVao, 2, 4, VertexAttribType.UnsignedByte, true, 16);
        }

        private void RenderOpenGL()
        {
            GLFW.GetFramebufferSize(_window.WindowPtr, out var fbW, out var fbH);
            GL.Viewport(0, 0, fbW, fbH);
            GL.Disable(EnableCap.ScissorTest);
            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Enable(EnableCap.ScissorTest);

            var drawData = ImGui.GetDrawData();

            var l = drawData.DisplayPos.X;
            var r = drawData.DisplayPos.X + drawData.DisplaySize.X;
            var t = drawData.DisplayPos.Y;
            var b = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

            var matrix = Matrix4x4.CreateOrthographicOffCenter(l, r, b, t, -1, 1);

            GL.ProgramUniformMatrix4(_glShaderProgram, _glUniformProjMatrix, 1, false, (float*) &matrix);
            GL.ProgramUniform1(_glShaderProgram, _glUniformTexture, 0);
            GL.BindVertexArray(_glVao);
            GL.UseProgram(_glShaderProgram);

            var clipOff = drawData.DisplayPos;
            var clipScale = drawData.FramebufferScale;

            GL.BindBuffer(BufferTarget.ArrayBuffer, _glVbo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _glEbo);

            for (var n = 0; n < drawData.CmdListsCount; n++)
            {
                var drawList = drawData.CmdListsRange[n];

                GL.BufferData(BufferTarget.ArrayBuffer, drawList.VtxBuffer.Size * sizeof(ImDrawVert),
                    drawList.VtxBuffer.Data,
                    BufferUsageHint.StreamDraw);
                GL.BufferData(BufferTarget.ElementArrayBuffer, drawList.IdxBuffer.Size * sizeof(ushort),
                    drawList.IdxBuffer.Data,
                    BufferUsageHint.StreamDraw);


                for (var cmdI = 0; cmdI < drawList.CmdBuffer.Size; cmdI++)
                {
                    var cmd = drawList.CmdBuffer[cmdI];

                    GL.BindTextureUnit(0, (uint) cmd.TextureId);

                    Vector4 clipRect = default;
                    clipRect.X = (cmd.ClipRect.X - clipOff.X) * clipScale.X;
                    clipRect.Y = (cmd.ClipRect.Y - clipOff.Y) * clipScale.Y;
                    clipRect.Z = (cmd.ClipRect.Z - clipOff.X) * clipScale.X;
                    clipRect.W = (cmd.ClipRect.W - clipOff.Y) * clipScale.Y;

                    GL.Scissor((int) clipRect.X, (int) (fbH - clipRect.W), (int) (clipRect.Z - clipRect.X),
                        (int) (clipRect.W - clipRect.Y));

                    GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int) cmd.ElemCount,
                        DrawElementsType.UnsignedShort,
                        (nint) (cmd.IdxOffset * 2), (int) cmd.VtxOffset);
                }
            }

            _window.SwapBuffers();
        }

        private static readonly DebugProc GLDebugCallbackDelegate = GLDebugCallback;

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
