using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Weapons.Ranged.UI;

/// <summary>
/// Displays magazine ammunition information for <see cref="BallisticAmmoProviderComponent"/>.
/// </summary>
/// <seealso cref="GunSystem"/>
public sealed partial class MagazineStatusControl : PollingItemStatusControl<MagazineStatusControl.Data>
{
    private readonly Entity<BallisticAmmoProviderComponent> _parent;
    private readonly RichTextLabel _label;

    public MagazineStatusControl(Entity<BallisticAmmoProviderComponent> parent)
    {
        _parent = parent;
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
    }

    protected override Data PollData()
    {
        var ammoProvider = _parent.Comp;
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
