using Content.Server.Shuttles;
using Content.Shared.Alert;
using Content.Shared.Shuttles;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Alert.Click
{
    /// <summary>
    /// Stop piloting shuttle
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public class StopPiloting : IAlertClick
    {
        public void AlertClicked(ClickAlertEventArgs args)
        {
            if (args.Player.TryGetComponent(out PilotComponent? pilotComponent) &&
                pilotComponent.Console != null)
            {
                EntitySystem.Get<ShuttleConsoleSystem>().RemovePilot(pilotComponent);
            }
        }
    }
}
