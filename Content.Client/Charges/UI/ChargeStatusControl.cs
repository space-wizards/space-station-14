using Content.Client.Charges.Components;
using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Charges.UI;

/// <summary>
/// Displays limited charges information for <see cref="ChargeItemStatusComponent"/>.
/// </summary>
/// <seealso cref="ChargeItemStatusSystem"/>
public sealed class ChargeStatusControl : PollingItemStatusControl<ChargeStatusControl.Data>
{
    private readonly Entity<ChargeItemStatusComponent> _parent;
    private readonly IEntityManager _entityManager;
    private readonly SharedChargesSystem _chargesSystem;
    private readonly RichTextLabel _label;

    public ChargeStatusControl(
        Entity<ChargeItemStatusComponent> parent,
        IEntityManager entityManager,
        SharedChargesSystem chargesSystem)
    {
        _parent = parent;
        _entityManager = entityManager;
        _chargesSystem = chargesSystem;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        AddChild(_label);
    }

    protected override Data PollData()
    {
        // Try to get limited charges component
        if (!_entityManager.TryGetComponent(_parent.Owner, out LimitedChargesComponent? charges))
            return default;

        var currentCharges = _chargesSystem.GetCurrentCharges((_parent.Owner, charges, null));
        var maxCharges = charges.MaxCharges;

        TimeSpan? nextRecharge = null;
        if (_parent.Comp.ShowRechargeTimer &&
            _entityManager.TryGetComponent(_parent.Owner, out AutoRechargeComponent? autoRecharge))
        {
            var nextRechargeTime = _chargesSystem.GetNextRechargeTime((_parent.Owner, charges, autoRecharge));
            if (nextRechargeTime > TimeSpan.Zero)
                nextRecharge = nextRechargeTime;
        }

        return new Data(currentCharges, maxCharges, nextRecharge);
    }

    protected override void Update(in Data data)
    {
        var markup = Loc.GetString("charge-status-count",
            ("current", data.CurrentCharges),
            ("max", data.MaxCharges));

        if (data.NextRecharge.HasValue)
        {
            var seconds = (int)data.NextRecharge.Value.TotalSeconds;
            markup += "\n" + Loc.GetString("charge-status-recharge", ("seconds", seconds));
        }

        _label.SetMarkup(markup);
    }

    public readonly record struct Data(int CurrentCharges, int MaxCharges, TimeSpan? NextRecharge);
}
