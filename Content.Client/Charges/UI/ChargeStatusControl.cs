using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client.Charges.UI;

/// <summary>
/// Displays limited charges information for <see cref="LimitedChargesComponent"/>.
/// </summary>
/// <seealso cref="ChargesSystem"/>
public sealed partial class ChargeStatusControl : PollingItemStatusControl<ChargeStatusControl.Data>
{
    [Dependency] private IEntityManager _entityManager = default!;

    private readonly Entity<LimitedChargesComponent> _parent;
    private readonly SharedChargesSystem _chargesSystem;
    private readonly RichTextLabel _label;

    public ChargeStatusControl(Entity<LimitedChargesComponent> parent)
    {
        IoCManager.InjectDependencies(this);

        _parent = parent;
        _chargesSystem = _entityManager.System<SharedChargesSystem>();
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
    }

    protected override Data PollData()
    {
        var charges = _parent.Comp;
        var currentCharges = _chargesSystem.GetCurrentCharges((_parent.Owner, charges, null));
        var maxCharges = charges.MaxCharges;

        TimeSpan? nextRecharge = null;
        if (_entityManager.TryGetComponent<AutoRechargeComponent>(_parent.Owner, out var autoRecharge))
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
