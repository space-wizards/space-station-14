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

            if (entManager.TryGetComponent(player, out PullerComponent? puller) &&
                entManager.TryGetComponent(puller.Pulling, out PullableComponent? pullableComp))
            {
                ps.TryStopPull(puller.Pulling.Value, pullableComp, user: player);
            }
        }
    }
}
