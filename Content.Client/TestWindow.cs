using Content.Client.UserInterface;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Client
{
    [UsedImplicitly]
    internal sealed class TestWindowCommand : IConsoleCommand
    {
        public string Command => "window";
        public string Description => "A";
        public string Help => "A";

        public async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var window = await IoCManager.Resolve<IClyde>().CreateWindow();
            var root = IoCManager.Resolve<IUserInterfaceManager>().CreateWindowRoot(window);
            window.DisposeOnClose = true;

            /*
            var entity = IoCManager.Resolve<IEntityManager>().SpawnEntity("", new MapCoordinates(0, 0, new MapId(1)));
            var eye = entity.AddComponent<EyeComponent>();

            var vpControl = new ScalingViewport();
            root.AddChild(vpControl);
            vpControl.Eye = eye.Eye;
            vpControl.ViewportSize = (17 * 32, 17 * 32);
            */

            /*var vp = IoCManager.Resolve<IClyde>().CreateViewport(window.RenderTarget.Size);

            var entity = IoCManager.Resolve<IEntityManager>().SpawnEntity("", new MapCoordinates(0, 0, new MapId(1)));
            var eye = entity.AddComponent<EyeComponent>();
            vp.Eye = eye.Eye;

            root.AddChild(new ViewportControl(vp));
            */
            root.AddChild(new Button() { Text = "AAAAAAAA", HorizontalAlignment = Control.HAlignment.Center, VerticalAlignment = Control.VAlignment.Center});
        }
    }
}
