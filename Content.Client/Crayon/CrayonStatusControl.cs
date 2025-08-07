using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Crayon;
using Content.Shared.Decals;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Crayon;

public sealed class CrayonStatusControl : PollingItemStatusControl<CrayonStatusControl.Data>
{
    private readonly Entity<CrayonComponent> _parent;
    private readonly RichTextLabel _label;

    protected override Data PollData()
    {
        return new Data(_parent.Comp.SelectedState, _parent.Comp.Color, _parent.Comp.Charges, _parent.Comp.Capacity);
    }

    protected override void Update(in Data data)
    {
        _label.SetMarkup(Robust.Shared.Localization.Loc.GetString("crayon-drawing-label",
            ("color", data.Color),
            ("state", data.SelectedState.GetValueOrDefault()),
            ("charges", data.Charges),
            ("capacity", data.Capacity)));
    }

    public CrayonStatusControl(Entity<CrayonComponent> parent)
    {
        _parent = parent;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        AddChild(_label);
    }

    public record struct Data(ProtoId<DecalPrototype>? SelectedState, Color Color, int Charges, int Capacity);
}
