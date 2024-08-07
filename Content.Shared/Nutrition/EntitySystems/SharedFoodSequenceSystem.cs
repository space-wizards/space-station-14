using System.Text;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared.Nutrition.EntitySystems;

public partial class SharedFoodSequenceSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoodSequenceStartPointComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<FoodSequenceStartPointComponent> ent, ref InteractUsingEvent args)
    {
        if (TryComp<FoodSequenceElementComponent>(args.Used, out var sequenceElement))
            TryAddFoodElement(ent, (args.Used, sequenceElement), args.User);
    }

    private bool TryAddFoodElement(Entity<FoodSequenceStartPointComponent> start, Entity<FoodSequenceElementComponent> element, EntityUid? user = null)
    {
        if (start.Comp.FoodLayers.Count >= start.Comp.MaxLayers)
        {
            if (user is not null && _net.IsServer)
                _popup.PopupEntity(Loc.GetString("food-sequence-no-space"), start, user.Value);
            return false;
        }

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
            start.Comp.FoodLayers.Add(elementData.Value);
            Dirty(start);
        }

        UpdateFoodName(start);
        MergeFoodSolutions(start, element);
        QueueDel(element);
        return true;
    }

    public virtual void MergeFoodSolutions(Entity<FoodSequenceStartPointComponent> start, Entity<FoodSequenceElementComponent> element)
    {
    }

    private void UpdateFoodName(Entity<FoodSequenceStartPointComponent> start)
    {
        if (start.Comp.NameGeneration is null)
            return;

        var content = new StringBuilder();
        var separator = "";
        if (start.Comp.ContentSeparator is not null)
            separator = Loc.GetString(start.Comp.ContentSeparator);

        foreach (var layer in start.Comp.FoodLayers)
        {
            if (layer.Name is not null)
                content.Append(Loc.GetString(layer.Name.Value));

            content.Append(separator);
        }

        var newName = Loc.GetString(start.Comp.NameGeneration.Value,
            ("prefix", start.Comp.NamePrefix is not null ? Loc.GetString(start.Comp.NamePrefix) : ""),
            ("content", content),
            ("suffix", start.Comp.NameSuffix is not null ? Loc.GetString(start.Comp.NameSuffix) : ""));

        _metaData.SetEntityName(start, newName);
    }
}
