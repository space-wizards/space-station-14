using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Storage.Interfaces;

/// <summary>
/// BUI for opening a radial menu on the player when they interact with an entity whose item slots can be accessed that way.
/// </summary>
[UsedImplicitly]
public sealed partial class RadialItemSlotMenuBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private EntityQuery<MetaDataComponent> _metaQuery;

    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        if (!EntMan.TryGetComponent<ItemSlotsComponent>(Owner, out var itemSlots))
            return;

        base.Open();

        var selectableEntities = CreateButtons(itemSlots);

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.SetButtons(selectableEntities);

        _menu.OpenOverMouseScreenPosition();
    }

    private IEnumerable<RadialMenuOptionBase> CreateButtons(ItemSlotsComponent itemSlots)
    {
        // We don't wanna fill up the radial menu with the same things, so we save what we added before.
        // We use the name, so secret items that imitate other items don't get their own option.
        HashSet<string> usedItemNames = [];

        // We reverse it here, so the last filled item slot will be shown as an option in the case of multiple identical slots.
        // e.g. placing an explosive wet floor sign and a normal one into a janitorial trolley in that order will eject the normal one.
        var slots = itemSlots.Slots.Values.Reverse();

        foreach (var slot in slots)
        {
            if(!slot.HasItem)
                continue;

            if (!_metaQuery.TryComp(slot.Item.Value, out var meta)
                || meta.EntityPrototype == null
                || slot.ID == null
                || !usedItemNames.Add(meta.EntityName))
                continue;

            var option = new RadialMenuActionOption<string>(EjectItem, slot.ID)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(slot.Item),
                ToolTip = Loc.GetString("take-item-verb-text", ("subject", slot.Item)),
            };
            yield return option;
        }
    }

    private void EjectItem(string slotId)
    {
        var message = new ItemSlotButtonPressedEvent(slotId, true, false);
        SendPredictedMessage(message);
    }
}
