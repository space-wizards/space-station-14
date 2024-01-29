using Content.Server.Cuffs;
using Content.Shared.Alert;
using JetBrains.Annotations;

namespace Content.Server.Alert.Click
{
    /// <summary>
    ///     Try to remove handcuffs from yourself
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class RemoveCuffs : IAlertClick
    {
        public void AlertClicked(EntityUid player)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var cuffableSys = entityManager.System<CuffableSystem>();
            cuffableSys.TryUncuff(player, player);
        }
    }
}
