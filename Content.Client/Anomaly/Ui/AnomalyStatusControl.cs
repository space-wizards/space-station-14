using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Anomaly.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client.Anomaly.UI;

/// <summary>
/// Displays anomaly core charge information based on <see cref="AnomalyCoreComponent"/> in the slot of.
/// <see cref="CorePoweredThrowerComponent"/>.
/// </summary>
/// <seealso cref="AnomalySystem"/>
public sealed partial class AnomalyStatusControl : PollingItemStatusControl<AnomalyStatusControl.Data>
{
    [Dependency] private IEntityManager _entityManager = default!;

    private readonly Entity<CorePoweredThrowerComponent> _parent;
    private readonly ItemSlotsSystem _itemSlots;
    private readonly RichTextLabel _label;

    public AnomalyStatusControl(Entity<CorePoweredThrowerComponent> parent)
    {
        IoCManager.InjectDependencies(this);

        _parent = parent;
        _itemSlots = _entityManager.System<ItemSlotsSystem>();
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
    }

    protected override Data PollData()
    {
        if (_itemSlots.GetItemOrNull(_parent.Owner, _parent.Comp.CoreSlotId) is { } coreEnt
            && _entityManager.TryGetComponent<AnomalyCoreComponent>(coreEnt, out var core))
        {
            return new Data(true, core.IsDecayed, core.Charge);
        }

        return new Data(false, false, 0);
    }

    protected override void Update(in Data data)
    {
        string markup;
        if (!data.IsDecayed)
            markup = Loc.GetString("anomaly-status-infinite");
        else
            markup = Loc.GetString("anomaly-status-charges", ("charges", data.Charges));

        _label.SetMarkup(markup);
    }

    public readonly record struct Data(bool HasCore, bool IsDecayed, int Charges);
}
