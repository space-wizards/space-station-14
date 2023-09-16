using System;
using System.IO;
using System.Threading.Tasks;
using Content.Client.Viewport;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.State;
using Robust.Shared.ContentPack;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Content.Client.UserInterface.Screens;
using Content.Shared.CCVar;
using Content.Shared.HUD;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.Fullscreen
{
    internal sealed class FullscreenHook : IFullscreenHook
    {
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        public void Initialize()
        {
            _inputManager.SetInputCommand(ContentKeyFunctions.ToggleFullscreen, InputCmdHandler.FromDelegate(_ =>
            {
                ToggleFullscreen();
            }));
        }


        private void ToggleFullscreen()
        {
            var currentWindowMode = _cfg.GetCVar<int>(CVars.DisplayWindowMode);

            if (currentWindowMode == (int) WindowMode.Windowed)
            {
                _cfg.SetCVar(CVars.DisplayWindowMode, (int) WindowMode.Fullscreen);
                Logger.InfoS("ToggleFullscreen", "Switched to Fullscreen mode");
            }
            else
            {
                _cfg.SetCVar(CVars.DisplayWindowMode, (int) WindowMode.Windowed);
                Logger.InfoS("ToggleFullscreen", "Switched to Windowed mode");
            }
        }


    }

    public interface IFullscreenHook
    {
        void Initialize();
    }
}
