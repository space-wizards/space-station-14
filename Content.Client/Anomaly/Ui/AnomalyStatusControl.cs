using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Anomaly;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Anomaly.UI;

/// <summary>
/// Displays anomaly core charge information for <see cref="AnomalyItemStatusComponent"/>.
/// </summary>
public sealed class AnomalyStatusControl : PollingItemStatusControl<AnomalyStatusControl.Data>
{
    private readonly Entity<AnomalyItemStatusComponent> _parent;
    private readonly RichTextLabel _label;

    public AnomalyStatusControl(
        Entity<AnomalyItemStatusComponent> parent)
    {
        _parent = parent;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        AddChild(_label);
    }

    protected override Data PollData()
    {
        return new Data(_parent.Comp.HasCore, _parent.Comp.IsDecayed, _parent.Comp.Charges);
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
