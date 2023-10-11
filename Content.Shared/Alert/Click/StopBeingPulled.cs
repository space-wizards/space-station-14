using Content.Shared.ActionBlocker;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
using JetBrains.Annotations;

namespace Content.Shared.Alert.Click
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

            if (entityManager.TryGetComponent(player, out SharedPullableComponent? playerPullable))
            {
                entityManager.System<SharedPullingSystem>().TryStopPull(playerPullable);
            }
        }
    }
}
