using Content.Client.Items.Components;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Content.Shared.GPS;

namespace Content.Client.GPS
{
    [RegisterComponent]
    internal sealed class HandheldGPSComponent : SharedHandheldGPSComponent, IItemStatus
    {
        Control IItemStatus.MakeControl()
        {
            return new StatusControl(this);
        }

        private sealed class StatusControl : Control
        {
            private readonly HandheldGPSComponent _parent;
            private readonly RichTextLabel _label;
            private float UpdateDif;
            private readonly IEntityManager _entMan = default!;

            public StatusControl(HandheldGPSComponent parent)
            {
                _parent = parent;
                _entMan = IoCManager.Resolve<IEntityManager>();
                _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
                AddChild(_label);
                UpdateGPSDetails();
            }

            protected override void FrameUpdate(FrameEventArgs args)
            {
                base.FrameUpdate(args);

                UpdateDif += args.DeltaSeconds;
                if (UpdateDif < _parent.UpdateRate)
                    return;

                UpdateDif -= _parent.UpdateRate;

                UpdateGPSDetails();
            }

            public void UpdateGPSDetails()
            {
                string posText = "Error";
                if (_entMan.TryGetComponent<TransformComponent>(_parent.Owner, out TransformComponent? transComp))
                {
                    if (transComp.Coordinates != null)
                    {
                        var pos =  transComp.MapPosition;
                        var x = (int) pos.X;
                        var y = (int) pos.Y;
                        posText = $"({x}, {y})";
                    }
                }
                _label.SetMarkup(Loc.GetString("handheld-gps-coordinates-title", ("coordinates", posText)));
            }
        }
    }
}
