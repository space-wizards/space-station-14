using Content.Server.Ensnaring;
using Content.Server.Ensnaring.Components;
using Content.Shared.Alert;
using JetBrains.Annotations;

namespace Content.Server.Alert.Click;
[UsedImplicitly]
[DataDefinition]
public sealed class RemoveEnsnare : IAlertClick
{
    public void AlertClicked(EntityUid player)
    {
        var entManager = IoCManager.Resolve<IEntityManager>();
        if (entManager.TryGetComponent(player, out EnsnareableComponent? ensnareableComponent))
        {
            foreach (var ensnare in ensnareableComponent.Container.ContainedEntities)
            {
                if (!entManager.TryGetComponent(ensnare, out EnsnaringComponent? ensnaringComponent))
                    return;

                entManager.EntitySysManager.GetEntitySystem<EnsnareableSystem>().TryFree(player, ensnaringComponent);
            }
        }
    }
}
