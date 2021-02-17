using Content.Server.GameObjects.Components.Movement;
 using Content.Shared.Alert;
 using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Server.Alert.Click
{
    /// <summary>
    /// Stop piloting shuttle
    /// </summary>
    [UsedImplicitly]
    public class StopPiloting : IAlertClick
    {
        void IExposeData.ExposeData(ObjectSerializer serializer) { }

        public void AlertClicked(ClickAlertEventArgs args)
        {
            if (args.Player.TryGetComponent(out ShuttleControllerComponent controller))
            {
                controller.RemoveController();
            }
        }
    }
}
