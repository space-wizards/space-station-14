using Content.Shared.GameObjects.Components.Power;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;

namespace Content.Client.GameObjects.Components.Power
{
    public class PowerDebugTool : SharedPowerDebugTool
    {
        SS14Window LastWindow;
        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case OpenDataWindowMsg msg:
                    if (LastWindow != null && !LastWindow.Disposed)
                    {
                        LastWindow.Dispose();
                    }
                    LastWindow = new SS14Window(IoCManager.Resolve<IDisplayManager>())
                    {
                        Title = "Power Debug Tool",
                    };
                    LastWindow.Contents.AddChild(new Label() { Text = msg.Data });
                    LastWindow.AddToScreen();
                    LastWindow.Open();
                    break;
            }
        }
    }
}
