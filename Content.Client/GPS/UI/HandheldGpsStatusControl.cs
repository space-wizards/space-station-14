using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.GPS.Components;
using Content.Shared.GPS.Systems;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.GPS.UI;

public sealed class HandheldGpsStatusControl : Control
{
    private readonly Entity<HandheldGPSComponent> _parent;
    private readonly RichTextLabel _label;
    private float _updateDif;

    private readonly HandheldGpsSystem _handheldGps;

    public HandheldGpsStatusControl(Entity<HandheldGPSComponent> parent)
    {
        _parent = parent;
        _handheldGps = IoCManager.Resolve<IEntityManager>().System<HandheldGpsSystem>();
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
        UpdateGpsDetails();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        // don't display the label if the gps component is being removed
        if (_parent.Comp.LifeStage > ComponentLifeStage.Running)
        {
            _label.Visible = false;
            return;
        }

        _updateDif += args.DeltaSeconds;
        if (_updateDif < _parent.Comp.UpdateRate)
            return;

        _updateDif -= _parent.Comp.UpdateRate;

        UpdateGpsDetails();
    }

    private void UpdateGpsDetails()
    {
        _label.SetMarkup(_handheldGps.GetGpsDisplayMarkup(_parent, abbreviated: true));
    }
}
