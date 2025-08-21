using System.IO;
using System.Threading.Tasks;
using Content.Client.Viewport;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.State;
using Robust.Shared.ContentPack;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.Screenshot
{
    internal sealed class ScreenshotHook : IScreenshotHook
    {
        private static readonly ResPath BaseScreenshotPath = new("/Screenshots");

        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;
        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;

        private ISawmill _sawmill = default!;

        public void Initialize()
        {
            _sawmill = Logger.GetSawmill("screenshot");
            _sawmill.Level = LogLevel.Info;

            _inputManager.SetInputCommand(ContentKeyFunctions.TakeScreenshot, InputCmdHandler.FromDelegate(_ =>
            {
                _clyde.Screenshot(ScreenshotType.Final, Take);
            }));

            _inputManager.SetInputCommand(ContentKeyFunctions.TakeScreenshotNoUI, InputCmdHandler.FromDelegate(_ =>
            {
                if (_stateManager.CurrentState is IMainViewportState state)
                {
                    state.Viewport.Viewport.Screenshot(Take);
                }
                else
                {
                    _sawmill.Info("Can't take no-UI screenshot: current state is not GameScreen");
                }
            }));
        }

        private async void Take<T>(Image<T> screenshot) where T : unmanaged, IPixel<T>
        {
            var time = DateTime.Now.ToString("yyyy-M-dd_HH.mm.ss");

            if (!_resourceManager.UserData.IsDir(BaseScreenshotPath))
            {
                _resourceManager.UserData.CreateDir(BaseScreenshotPath);
            }

            for (var i = 0; i < 5; i++)
            {
                try
                {
                    var filename = time;

                    if (i != 0)
                    {
                        filename = $"{filename}-{i}";
                    }

                    await using var file =
                        _resourceManager.UserData.Open(BaseScreenshotPath / $"{filename}.png", FileMode.CreateNew, FileAccess.Write, FileShare.None);

                    await Task.Run(() =>
                    {
                        // Saving takes forever, so don't hang the game on it.
                        screenshot.SaveAsPng(file);
                    });

                    _sawmill.Info("Screenshot taken as {0}.png", filename);
                    return;
                }
                catch (IOException e)
                {
                    _sawmill.Warning("Failed to save screenshot, retrying?:\n{0}", e);
                }
            }

            _sawmill.Error("Unable to save screenshot.");
        }
    }

    public interface IScreenshotHook
    {
        void Initialize();
    }
}
