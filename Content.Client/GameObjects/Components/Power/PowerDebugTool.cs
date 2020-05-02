using Content.Shared.GameObjects.Components.Power;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Players;

namespace Content.Client.GameObjects.Components.Power
{
    [RegisterComponent]
    public class PowerDebugTool : SharedPowerDebugTool
    {
        SS14Window LastWindow;
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            switch (message)
            {
                case OpenDataWindowMsg msg:
                    if (LastWindow != null && !LastWindow.Disposed)
                    {
                        LastWindow.Dispose();
                    }
                    LastWindow = new SS14Window()
                    {
                        Title = "Power Debug Tool",
                    };
                    LastWindow.Contents.AddChild(new Label() { Text = msg.Data });
                    LastWindow.Open();
                    break;
            }
        }
    }
}
