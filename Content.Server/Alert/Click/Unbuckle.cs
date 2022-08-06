using Content.Server.Buckle.Components;
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
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(player, out BuckleComponent? buckle))
            {
                buckle.TryUnbuckle(player);
            }
        }
    }
}
