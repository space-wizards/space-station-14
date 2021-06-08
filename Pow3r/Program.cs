using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ImGuiNET;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ErrorCode = OpenTK.Windowing.GraphicsLibraryFramework.ErrorCode;
using Vector2 = System.Numerics.Vector2;

// ReSharper disable PossibleNullReferenceException

namespace Pow3r
{
    internal sealed unsafe partial class Program
    {
        public const float TicksPerSecond = 60;
        private static readonly TimeSpan TickSpan = TimeSpan.FromSeconds(1 / TicksPerSecond);

        private Renderer _renderer = Renderer.Veldrid;

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

        private bool[] _mouseJustPressed = new bool[5];

        private GameWindow _window;
        private readonly Stopwatch _stopwatch = new();
        private readonly Cursor*[] _cursors = new Cursor*[9];

        private void Run(string[] args)
        {
            if (args.Length >= 2 && args[0] == "--renderer")
            {
                _renderer = Enum.Parse<Renderer>(args[1]);
            }

            //NativeLibrary.Load("nvapi64.dll");
            GLFW.Init();
            GLFW.SetErrorCallback(ErrorCallback);

            // var sw = Stopwatch.StartNew();
            GLFW.WindowHint(WindowHintBool.SrgbCapable, true);
            var windowSettings = new NativeWindowSettings
            {
                Size = (1280, 720),
                WindowState = WindowState.Maximized,
                StartVisible = false,

                Title = "Pow3r",
            };

            Console.WriteLine($"Renderer is {_renderer}");

            switch (_renderer)
            {
                case Renderer.OpenGL:
                    windowSettings.API = ContextAPI.OpenGL;
                    windowSettings.Profile = ContextProfile.Core;
                    windowSettings.APIVersion = new Version(4, 6);
                    windowSettings.Flags = ContextFlags.Debug | ContextFlags.ForwardCompatible;
                    break;

                case Renderer.Veldrid:
                    windowSettings.API = ContextAPI.NoAPI;
                    break;
            }

            _window = new GameWindow(GameWindowSettings.Default, windowSettings);

            // Console.WriteLine(sw.ElapsedMilliseconds);

            if (_renderer == Renderer.OpenGL)
            {
                _window.VSync = VSyncMode.On;
            }

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

            InitRenderer();

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

        private void InitRenderer()
        {
            switch (_renderer)
            {
                case Renderer.OpenGL:
                    InitOpenGL();
                    break;

                case Renderer.Veldrid:
                    InitVeldrid();
                    break;
            }
        }

        private void Render()
        {
            ImGui.Render();

            switch (_renderer)
            {
                case Renderer.OpenGL:
                    RenderOpenGL();
                    break;

                case Renderer.Veldrid:
                    RenderVeldrid();
                    break;
            }
        }

        private static void Main(string[] args)
        {
            new Program().Run(args);
        }

        public enum Renderer
        {
            OpenGL,
            Veldrid
        }
    }
}
