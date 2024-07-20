using Content.Shared.Interaction;
using Content.Shared.Placeable;

namespace Content.Server.BUG;

public sealed class BugFuckSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BugFuckComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnInteractHand(Entity<BugFuckComponent> ent, ref InteractHandEvent args)
    {
        if (!TryComp<ItemPlacerComponent>(ent, out var itemPlacer))
            return;

        foreach (var fuck in itemPlacer.PlacedEntities)
        {
            QueueDel(fuck);
        }
    }
}
