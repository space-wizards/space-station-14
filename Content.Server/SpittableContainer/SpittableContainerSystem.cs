using Content.Server.Body.Components;
using Content.Shared.SpittableContainer;
using Content.Shared.SpittableContainer.Components;

namespace Content.Server.SpittableContainer;

public sealed class SpittableContainerSystem : SharedSpittableContainerSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpittableContainerComponent, BeingGibbedEvent>(OnGibbed);
    }

    private void OnGibbed(Entity<SpittableContainerComponent> ent, ref BeingGibbedEvent args)
    {
        _containerSystem.EmptyContainer(ent.Comp.Container);
    }
}
