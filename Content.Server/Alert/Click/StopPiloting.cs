using Content.Server.Shuttles;
using Content.Server.Shuttles.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.Shuttles;
using Content.Shared.Shuttles.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Alert.Click
{
    /// <summary>
    /// Stop piloting shuttle
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed class StopPiloting : IAlertClick
    {
        public void AlertClicked(EntityUid player)
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(player, out PilotComponent? pilotComponent) &&
                pilotComponent.Console != null)
            {
                EntitySystem.Get<ShuttleConsoleSystem>().RemovePilot(pilotComponent);
            }
        }
    }
}
