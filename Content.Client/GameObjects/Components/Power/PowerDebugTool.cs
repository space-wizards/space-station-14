using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Power;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;

namespace Content.Client.GameObjects.Components.Power
{
    public class PowerDebugTool : SharedPowerDebugTool
    {
        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case OpenDataWindowMsg msg:
                    var window = new SS14Window
                    {
                        Title = "Power Debug Tool"
                    };
                    window.Contents.AddChild(new Label() { Text = msg.Data });
                    window.AddToScreen();
                    window.Open();
                    break;
            }
        }
    }
}
