using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Pulling;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Alert.Click
{
    /// <summary>
    /// Stop pulling something
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public class StopBeingPulled : IAlertClick
    {
        public void AlertClicked(ClickAlertEventArgs args)
        {
            args.Player.GetComponentOrNull<SharedPullableComponent>()?.TryStopPull();
        }
    }
}
