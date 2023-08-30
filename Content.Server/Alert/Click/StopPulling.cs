using Content.Shared.Alert;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
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

            var ps = entManager.System<SharedPullingSystem>();
            var playerTarget = ps.GetPulled(player);
            if (playerTarget != default && entManager.TryGetComponent(playerTarget, out SharedPullableComponent? playerPullable))
            {
                ps.TryStopPull(playerPullable);
            }
        }
    }
}
