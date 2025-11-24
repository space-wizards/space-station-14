using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Anomaly.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Anomaly.UI;

/// <summary>
/// Displays anomaly core charge information based on <see cref="AnomalyCoreComponent"/> in the slot of.
/// <see cref="CorePoweredThrowerComponent"/>.
/// </summary>
public sealed class AnomalyStatusControl : PollingItemStatusControl<AnomalyStatusControl.Data>
{
    private readonly Entity<CorePoweredThrowerComponent> _parent;
    private readonly IEntityManager _entityManager;
    private readonly ItemSlotsSystem _itemSlots;
    private readonly RichTextLabel _label;

    public AnomalyStatusControl(
        Entity<CorePoweredThrowerComponent> parent,
        IEntityManager entityManager,
        ItemSlotsSystem itemSlots)
    {
        _parent = parent;
        _entityManager = entityManager;
        _itemSlots = itemSlots;
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
    }

    protected override Data PollData()
    {
        if (_itemSlots.GetItemOrNull(_parent.Owner, _parent.Comp.CoreSlotId) is { } coreEnt
            && _entityManager.TryGetComponent(coreEnt, out AnomalyCoreComponent? core))
        {
            return new Data(true, core.IsDecayed, core.Charge);
        }

        return new Data(false, false, 0);
    }

    protected override void Update(in Data data)
    {
        string markup;
        if (!data.IsDecayed)
        {
            markup = Loc.GetString("anomaly-status-infinite");
        }
        else
        {
            markup = Loc.GetString("anomaly-status-charges", ("charges", data.Charges));
        }

        _label.SetMarkup(markup);
    }

    public readonly record struct Data(bool HasCore, bool IsDecayed, int Charges);
}
