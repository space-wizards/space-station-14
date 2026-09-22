using Content.Client.UserInterface.Controls;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Containers.ItemSlots.Events;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Storage.Interfaces;

/// <summary>
/// This handles opening a radial menu on the player when they interact with an entity whose item slots can be accessed that way.
/// </summary>
[UsedImplicitly]
public sealed partial class RadialItemSlotMenuBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;
    private EntityQuery<MetaDataComponent> _metaQuery;

    protected override void Open()
    {
        base.Open();
        _metaQuery = EntMan.GetEntityQuery<MetaDataComponent>();

        if (!EntMan.TryGetComponent<ItemSlotsComponent>(Owner, out var itemSlots))
            return;

        var selectableEntities = CreateButtons(itemSlots);

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.SetButtons(selectableEntities);

        _menu.OpenCentered();
    }

    private List<RadialMenuOptionBase> CreateButtons(ItemSlotsComponent itemSlots)
    {
        var options = new List<RadialMenuOptionBase>();

        List<ItemSlot> occupiedSlots = [];

        foreach (var slot in itemSlots.Slots)
        {
            if (slot.Value.HasItem) // Only show occupied slots.
                occupiedSlots.Add(slot.Value);
        }

        // We don't wanna fill up the radial menu with the same things, so we save what we added before.
        List<EntProtoId> usedProtos = [];

        foreach (var slot in occupiedSlots)
        {
            // Usage of the "!" modifier. It cannot be null due to the above. If you got a more elegant solution, try it.
            if (!_metaQuery.TryComp(slot.Item!.Value, out var meta)
                || meta.EntityPrototype == null
                || usedProtos.Contains(meta.EntityPrototype)
                || slot.ID == null)
                continue;

            usedProtos.Add(meta.EntityPrototype);

            var option = new RadialMenuActionOption<string>(EjectItem, slot.ID)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(slot.Item),
                ToolTip = Loc.GetString("take-item-verb-text", ("subject", slot.Item)),
            };
            options.Add(option);
        }

        return options;
    }

    private void EjectItem(string slotId)
    {
        var message = new EjectItemFromSlotMessage(slotId);
        SendPredictedMessage(message);
    }
}
