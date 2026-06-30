using Content.Client.Weapons.Ranged.Components;
using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Weapons.Ranged.UI;

/// <summary>
/// Displays magazine ammunition information for <see cref="MagazineItemStatusComponent"/>.
/// </summary>
/// <seealso cref="MagazineItemStatusSystem"/>
public sealed partial class MagazineStatusControl : PollingItemStatusControl<MagazineStatusControl.Data>
{
    private readonly Entity<MagazineItemStatusComponent> _parent;
    private readonly IEntityManager _entityManager;
    private readonly RichTextLabel _label;

    public MagazineStatusControl(
        Entity<MagazineItemStatusComponent> parent,
        IEntityManager entityManager)
    {
        _parent = parent;
        _entityManager = entityManager;
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
    }

    protected override Data PollData()
    {
        if (!_entityManager.TryGetComponent<BallisticAmmoProviderComponent>(_parent.Owner, out var ammoProvider))
            return default;

        var currentRounds = ammoProvider.Count;
        var maxRounds = ammoProvider.Capacity;

        return new Data(currentRounds, maxRounds);
    }

    protected override void Update(in Data data)
    {
        var markup = Loc.GetString("magazine-status-rounds",
            ("current", data.CurrentRounds),
            ("max", data.MaxRounds));

        _label.SetMarkup(markup);
    }

    public readonly record struct Data(int CurrentRounds, int MaxRounds);
}
