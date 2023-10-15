using Content.Shared.Cuffs;
using JetBrains.Annotations;

namespace Content.Shared.Alert.Click;

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
        var cuffableSys = entityManager.System<SharedCuffableSystem>();
        cuffableSys.TryUncuff(player, player);
    }
}
