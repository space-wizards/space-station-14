using Content.Shared.Shuttles.Components;
using JetBrains.Annotations;

namespace Content.Shared.Alert.Click
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
