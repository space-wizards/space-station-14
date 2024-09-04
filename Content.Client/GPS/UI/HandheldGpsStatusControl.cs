using Content.Shared.GPS.Components;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.GPS.UI;

public sealed class HandheldGpsStatusControl : Control
{
    private readonly Entity<HandheldGPSComponent> _parent;
    private readonly RichTextLabel _label;

    public HandheldGpsStatusControl(Entity<HandheldGPSComponent> parent)
    {
        _parent = parent;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        AddChild(_label);

        _label.SetMarkup(_parent.Comp.StoredCoords);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _label.SetMarkup(_parent.Comp.StoredCoords);
    }
}
