using Content.Server.Shuttles.Systems;
using Content.Shared.Alert;
using Content.Shared.Shuttles.Components;
using JetBrains.Annotations;

namespace Content.Server.Alert.Click
{
    /// <summary>
    /// Stop piloting shuttle
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class StopPiloting : IAlertClick
    {
        public void AlertClicked(EntityUid player)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();

            if (entManager.TryGetComponent(player, out PilotComponent? pilotComponent)
            && pilotComponent.Console != null)
            {
                entManager.System<ShuttleConsoleSystem>().RemovePilot(player, pilotComponent);
            }
        }
    }
}
