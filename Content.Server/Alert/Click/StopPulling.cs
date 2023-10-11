using Content.Shared.Alert;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using JetBrains.Annotations;

namespace Content.Server.Alert.Click
{
    /// <summary>
    /// Stop pulling something
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class StopPulling : IAlertClick
    {
        public void AlertClicked(EntityUid player)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();

            var ps = entManager.System<PullingSystem>();
            var playerTarget = ps.GetPulled(player);
            if (playerTarget != default && entManager.TryGetComponent(playerTarget, out PullableComponent? playerPullable))
            {
                ps.TryStopPull(playerPullable);
            }
        }
    }
}
