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
        if (IoCManager.Resolve<IEntityManager>().TryGetComponent(player, out EnsnareableComponent? ensnareableComponent))
        {
            foreach (var ensnare in ensnareableComponent.Container.ContainedEntities)
            {
                if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(ensnare, out EnsnaringComponent? ensnaringComponent))
                    return;

                EntitySystem.Get<EnsnaringSystem>().TryFree(player, ensnaringComponent);
            }
        }
    }
}
