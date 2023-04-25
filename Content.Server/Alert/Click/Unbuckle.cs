using Content.Shared.Alert;
using Content.Shared.Buckle;
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
            IoCManager.Resolve<IEntityManager>().System<SharedBuckleSystem>().TryUnbuckle(player, player);
        }
    }
}
