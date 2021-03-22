using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ErrorCode = OpenTK.Windowing.GraphicsLibraryFramework.ErrorCode;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

// ReSharper disable PossibleNullReferenceException

namespace Pow3r
{
    internal sealed unsafe partial class Program
    {
        public const float TicksPerSecond = 60;
        private static readonly TimeSpan TickSpan = TimeSpan.FromSeconds(1 / TicksPerSecond);

        private const string FragmentShader = @"
#version 460
in vec2 Frag_UV;
in vec4 Frag_Color;
uniform sampler2D Texture;
layout (location = 0) out vec4 Out_Color;
void main()
{
    Out_Color = Frag_Color * texture(Texture, Frag_UV.st);
}";

        private const string VertexShader = @"
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

        [UnmanagedCallersOnly]
        private static byte* GetClipboardTextCallback(void* userData)
        {
            return GLFW.GetClipboardStringRaw((Window*) userData);
        }

        [UnmanagedCallersOnly]
        private static void SetClipboardTextCallback(void* userData, byte* text)
        {
            GLFW.SetClipboardStringRaw((Window*) userData, text);
        }

        private static readonly GLFWCallbacks.ErrorCallback ErrorCallback = GlfwErrorCallback;

        private static void GlfwErrorCallback(ErrorCode error, string description)
        {
            Console.WriteLine($"{error}: {description}");
        }

        private int _shaderProgram;
        private int _fontTexture;
        private int _vao;
        private int _vbo;
        private int _ebo;

        private int _uniformTexture;
        private int _uniformProjMatrix;
        private bool[] _mouseJustPressed = new bool[5];

        private GameWindow _window;
        private readonly Stopwatch _stopwatch = new();
        private readonly Cursor*[] _cursors = new Cursor*[9];

        private void Run()
        {
            //NativeLibrary.Load("nvapi64.dll");
            GLFW.Init();
            GLFW.SetErrorCallback(ErrorCallback);

            var sw = Stopwatch.StartNew();
            GLFW.WindowHint(WindowHintBool.SrgbCapable, true);
            _window = new GameWindow(GameWindowSettings.Default, new NativeWindowSettings
            {
                API = ContextAPI.OpenGL,
                Profile = ContextProfile.Core,
                APIVersion = new Version(4, 5),
                Flags = ContextFlags.Debug | ContextFlags.ForwardCompatible,
                Size = (1280, 720),
                WindowState = WindowState.Maximized,
                StartVisible = false,

                Title = "Pow3r",
            });

            GLFW.GetWindowContentScale(_window.WindowPtr, out var scaleX, out var scaleY);

            Console.WriteLine(sw.ElapsedMilliseconds);

            _window.VSync = VSyncMode.On;

            var context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);
            ImGui.StyleColorsDark();

            var io = ImGui.GetIO();
            io.Fonts.AddFontDefault();

            delegate* unmanaged<void*, byte*> getClipboardCallback = &GetClipboardTextCallback;
            io.GetClipboardTextFn = (IntPtr) getClipboardCallback;
            delegate* unmanaged<void*, byte*, void> setClipboardCallback = &SetClipboardTextCallback;
            io.GetClipboardTextFn = (IntPtr) setClipboardCallback;
            io.ClipboardUserData = (IntPtr) _window.WindowPtr;
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
            io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;

            io.KeyMap[(int) ImGuiKey.Tab] = (int) Keys.Tab;
            io.KeyMap[(int) ImGuiKey.LeftArrow] = (int) Keys.Left;
            io.KeyMap[(int) ImGuiKey.RightArrow] = (int) Keys.Right;
            io.KeyMap[(int) ImGuiKey.UpArrow] = (int) Keys.Up;
            io.KeyMap[(int) ImGuiKey.DownArrow] = (int) Keys.Down;
            io.KeyMap[(int) ImGuiKey.PageUp] = (int) Keys.PageUp;
            io.KeyMap[(int) ImGuiKey.PageDown] = (int) Keys.PageDown;
            io.KeyMap[(int) ImGuiKey.Home] = (int) Keys.Home;
            io.KeyMap[(int) ImGuiKey.End] = (int) Keys.End;
            io.KeyMap[(int) ImGuiKey.Delete] = (int) Keys.Delete;
            io.KeyMap[(int) ImGuiKey.Backspace] = (int) Keys.Backspace;
            io.KeyMap[(int) ImGuiKey.Enter] = (int) Keys.Enter;
            io.KeyMap[(int) ImGuiKey.Escape] = (int) Keys.Escape;
            io.KeyMap[(int) ImGuiKey.A] = (int) Keys.A;
            io.KeyMap[(int) ImGuiKey.C] = (int) Keys.C;
            io.KeyMap[(int) ImGuiKey.V] = (int) Keys.V;
            io.KeyMap[(int) ImGuiKey.X] = (int) Keys.X;
            io.KeyMap[(int) ImGuiKey.Y] = (int) Keys.Y;
            io.KeyMap[(int) ImGuiKey.Z] = (int) Keys.Z;

            _cursors[(int) ImGuiMouseCursor.Arrow] = GLFW.CreateStandardCursor(CursorShape.Arrow);
            _cursors[(int) ImGuiMouseCursor.TextInput] = GLFW.CreateStandardCursor(CursorShape.IBeam);
            _cursors[(int) ImGuiMouseCursor.ResizeNS] = GLFW.CreateStandardCursor(CursorShape.VResize);
            _cursors[(int) ImGuiMouseCursor.ResizeEW] = GLFW.CreateStandardCursor(CursorShape.HResize);
            _cursors[(int) ImGuiMouseCursor.Hand] = GLFW.CreateStandardCursor(CursorShape.Hand);
            _cursors[(int) ImGuiMouseCursor.ResizeAll] = GLFW.CreateStandardCursor(CursorShape.Arrow);
            _cursors[(int) ImGuiMouseCursor.ResizeNESW] = GLFW.CreateStandardCursor(CursorShape.Arrow);
            _cursors[(int) ImGuiMouseCursor.ResizeNWSE] = GLFW.CreateStandardCursor(CursorShape.Arrow);
            _cursors[(int) ImGuiMouseCursor.NotAllowed] = GLFW.CreateStandardCursor(CursorShape.Arrow);

            InitOpenGL();

            _window.MouseDown += OnMouseDown;
            _window.TextInput += WindowOnTextInput;
            _window.MouseWheel += WindowOnMouseWheel;
            _window.KeyDown += args => KeyCallback(args, true);
            _window.KeyUp += args => KeyCallback(args, false);

            _stopwatch.Start();

            var lastTick = TimeSpan.Zero;
            var lastFrame = TimeSpan.Zero;

            LoadFromDisk();

            _window.IsVisible = true;

            while (!GLFW.WindowShouldClose(_window.WindowPtr))
            {
                _window.ProcessEvents();

                var curTime = _stopwatch.Elapsed;
                while (curTime - lastTick > TickSpan)
                {
                    lastTick += TickSpan;

                    Tick((float) TickSpan.TotalSeconds);
                }

                var dt = curTime - lastFrame;
                lastFrame = curTime;

                FrameUpdate((float) dt.TotalSeconds);
                Render();
            }

            SaveToDisk();
        }

        private static void KeyCallback(KeyboardKeyEventArgs obj, bool down)
        {
            var io = ImGui.GetIO();

            var keyInt = (int) obj.Key;
            io.KeysDown[keyInt] = down;
            io.KeyCtrl = io.KeysDown[(int) Keys.LeftControl] || io.KeysDown[(int) Keys.RightControl];
            io.KeyShift = io.KeysDown[(int) Keys.LeftShift] || io.KeysDown[(int) Keys.RightShift];
            io.KeyAlt = io.KeysDown[(int) Keys.LeftAlt] || io.KeysDown[(int) Keys.RightAlt];
        }

        private static void WindowOnMouseWheel(MouseWheelEventArgs obj)
        {
            var io = ImGui.GetIO();
            io.MouseWheelH += obj.OffsetX;
            io.MouseWheel += obj.OffsetY;
        }

        private static void WindowOnTextInput(TextInputEventArgs obj)
        {
            var io = ImGui.GetIO();
            io.AddInputCharacter((uint) obj.Unicode);
        }

        private void OnMouseDown(MouseButtonEventArgs obj)
        {
            var button = (int) obj.Button;
            if (obj.IsPressed && button < _mouseJustPressed.Length)
                _mouseJustPressed[button] = true;
        }

        private void FrameUpdate(float dt)
        {
            var io = ImGui.GetIO();
            GLFW.GetFramebufferSize(_window.WindowPtr, out var fbW, out var fbH);
            GLFW.GetWindowSize(_window.WindowPtr, out var wW, out var wH);
            io.DisplaySize = new Vector2(wW, wH);
            io.DisplayFramebufferScale = new Vector2(fbW / (float) wW, fbH / (float) wH);
            io.DeltaTime = dt;

            UpdateMouseState(io);
            UpdateCursorState(io);

            ImGui.NewFrame();

            DoDraw(dt);
        }

        private void UpdateCursorState(ImGuiIOPtr io)
        {
            var cursor = ImGui.GetMouseCursor();
            if (cursor == ImGuiMouseCursor.None)
            {
                GLFW.SetInputMode(_window.WindowPtr, CursorStateAttribute.Cursor, CursorModeValue.CursorHidden);
            }
            else
            {
                GLFW.SetCursor(_window.WindowPtr, _cursors[(int) cursor]);
                GLFW.SetInputMode(_window.WindowPtr, CursorStateAttribute.Cursor, CursorModeValue.CursorNormal);
            }
        }

        private void UpdateMouseState(ImGuiIOPtr io)
        {
            for (var i = 0; i < io.MouseDown.Count; i++)
            {
                io.MouseDown[i] = _mouseJustPressed[i] ||
                                  GLFW.GetMouseButton(_window.WindowPtr, (MouseButton) i) == InputAction.Press;
                _mouseJustPressed[i] = false;
            }

            var oldMousePos = io.MousePos;
            io.MousePos = new Vector2(float.PositiveInfinity, float.PositiveInfinity);

            var focused = _window.IsFocused;
            if (focused)
            {
                if (io.WantSetMousePos)
                {
                    GLFW.SetCursorPos(_window.WindowPtr, oldMousePos.X, oldMousePos.Y);
                }
                else
                {
                    GLFW.GetCursorPos(_window.WindowPtr, out var x, out var y);
                    io.MousePos = new Vector2((float) x, (float) y);
                }
            }
        }

        private void Render()
        {
            ImGui.Render();

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

            GL.ProgramUniformMatrix4(_shaderProgram, _uniformProjMatrix, 1, false, (float*) &matrix);
            GL.ProgramUniform1(_shaderProgram, _uniformTexture, 0);
            GL.BindVertexArray(_vao);
            GL.UseProgram(_shaderProgram);

            var clipOff = drawData.DisplayPos;
            var clipScale = drawData.FramebufferScale;

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);

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

            GL.ShaderSource(frag, FragmentShader);
            GL.ShaderSource(vert, VertexShader);

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

            _shaderProgram = GL.CreateProgram();
            GL.AttachShader(_shaderProgram, vert);
            GL.AttachShader(_shaderProgram, frag);

            GL.LinkProgram(_shaderProgram);

            GL.GetProgram(_shaderProgram, GetProgramParameterName.LinkStatus, out status);
            if (status != 1)
            {
                var log = GL.GetProgramInfoLog(_shaderProgram);
                throw new Exception($"Shader failed to link: {log}");
            }

            GL.DeleteShader(frag);
            GL.DeleteShader(vert);

            _uniformProjMatrix = GL.GetUniformLocation(_shaderProgram, "ProjMtx");
            _uniformTexture = GL.GetUniformLocation(_shaderProgram, "Texture");

            var io = ImGui.GetIO();

            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out var width, out var height, out _);

            GL.CreateTextures(TextureTarget.Texture2D, 1, out _fontTexture);
            GL.TextureStorage2D(_fontTexture, 1, SizedInternalFormat.Rgba8, width, height);
            GL.TextureSubImage2D(_fontTexture, 0, 0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte,
                (nint) pixels);
            GL.TextureParameter(_fontTexture, TextureParameterName.TextureMaxLevel, 0);

            /*
            GL.TextureParameter(_fontTexture, TextureParameterName.TextureSwizzleR, 1);
            GL.TextureParameter(_fontTexture, TextureParameterName.TextureSwizzleG, 1);
            GL.TextureParameter(_fontTexture, TextureParameterName.TextureSwizzleB, 1);
            GL.TextureParameter(_fontTexture, TextureParameterName.TextureSwizzleA, (int) All.Red);*/

            io.Fonts.SetTexID((nint) _fontTexture);
            io.Fonts.ClearTexData();

            // Buffers.
            GL.CreateBuffers(1, out _vbo);
            GL.CreateBuffers(1, out _ebo);

            GL.CreateVertexArrays(1, out _vao);
            GL.VertexArrayVertexBuffer(_vao, 0, _vbo, (nint) 0, sizeof(ImDrawVert));
            GL.VertexArrayElementBuffer(_vao, _ebo);

            GL.EnableVertexArrayAttrib(_vao, 0);
            GL.VertexArrayAttribBinding(_vao, 0, 0);
            GL.VertexArrayAttribFormat(_vao, 0, 2, VertexAttribType.Float, false, 0);

            GL.EnableVertexArrayAttrib(_vao, 1);
            GL.VertexArrayAttribBinding(_vao, 1, 0);
            GL.VertexArrayAttribFormat(_vao, 1, 2, VertexAttribType.Float, false, 8);

            GL.EnableVertexArrayAttrib(_vao, 2);
            GL.VertexArrayAttribBinding(_vao, 2, 0);
            GL.VertexArrayAttribFormat(_vao, 2, 4, VertexAttribType.UnsignedByte, true, 16);
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

        private static void Main()
        {
            new Program().Run();
        }
    }
}
