using Content.Shared.Interaction;

namespace Content.Shared.FoodSequence;

public partial class SharedFoodSequenceSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoodSequenceStartPointComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<FoodSequenceStartPointComponent> ent, ref InteractUsingEvent args)
    {
        if (TryComp<FoodSequenceElementComponent>(args.Used, out var sequenceElement))
            TryAddFoodElement(ent, (args.Used, sequenceElement));
    }

    private bool TryAddFoodElement(Entity<FoodSequenceStartPointComponent> start, Entity<FoodSequenceElementComponent> element)
    {
        FoodSequenceElementEntry? elementData = null;
        foreach (var entry in element.Comp.Entries)
        {
            if (entry.Key == start.Comp.Key)
            {
                elementData = entry.Value;
                break;
            }
        }

        if (elementData is null)
            return false;

        if (elementData.Value.State is not null)
        {
            start.Comp.FoodLayers.Add(elementData.Value.State);
            Dirty(start);
        }

        return true;
    }
}
