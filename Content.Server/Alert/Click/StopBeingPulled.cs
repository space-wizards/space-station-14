using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Pulling.Components;
using Content.Shared.Pulling;
using JetBrains.Annotations;
using PullableComponent = Content.Shared.Movement.Pulling.Components.PullableComponent;
using PullingSystem = Content.Shared.Movement.Pulling.Systems.PullingSystem;

namespace Content.Server.Alert.Click
{
    /// <summary>
    /// Stop pulling something
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class StopBeingPulled : IAlertClick
    {
        public void AlertClicked(EntityUid player)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.System<ActionBlockerSystem>().CanInteract(player, null))
                return;

            if (entityManager.TryGetComponent(player, out PullableComponent? playerPullable))
            {
                entityManager.System<PullingSystem>().TryStopPull(playerPullable);
            }
        }
    }
}
