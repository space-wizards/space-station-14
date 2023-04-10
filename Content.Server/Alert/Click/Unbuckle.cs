using Content.Server.Buckle.Systems;
using Content.Shared.Alert;
using JetBrains.Annotations;

namespace Content.Server.Alert.Click
{
    /// <summary>
    /// Unbuckles if player is currently buckled.
    /// </summary>
	[UsedImplicitly]
    [DataDefinition]
    public sealed class Unbuckle : IAlertClick
    {
        public void AlertClicked(EntityUid player)
        {
            IoCManager.Resolve<IEntityManager>().System<BuckleSystem>().TryUnbuckle(player, player);
        }
    }
}
