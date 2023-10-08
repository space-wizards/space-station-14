// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.PictureViewer;

public sealed partial class PictureViewerControls : BoxContainer
{
    private PictureViewer? _viewer;

    private readonly Label _zoomLabel = new()
    {
        VerticalAlignment = VAlignment.Top,
        Margin = new Thickness(8f, 8f),
    };

    private readonly Button _recenterButton = new()
    {
        Text = "Центрировать",
        VerticalAlignment = VAlignment.Top,
        HorizontalAlignment = HAlignment.Right,
        Margin = new Thickness(8f, 4f),
        Disabled = true,
    };

    public PictureViewerControls()
    {
        var topPanel = new PanelContainer()
        {
            PanelOverride = new StyleBoxFlat()
            {
                BackgroundColor = StyleNano.ButtonColorContext.WithAlpha(0.75f),
                BorderColor = StyleNano.PanelDark
            },
            VerticalExpand = false,
            Children =
            {
                _zoomLabel,
                _recenterButton,
            }
        };

        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;

        AddChild(topPanel);
        AddChild(new Control()
        {
            Name = "DrawingControl",
            VerticalExpand = true,
            Margin = new Thickness(5f, 5f)
        });

        _recenterButton.OnPressed += (BaseButton.ButtonEventArgs _) =>
        {
            _viewer?.Recenter();
        };
    }

    override protected void Draw(DrawingHandleScreen handle)
    {
        UpdateAll();
    }

    public void AttachToViewer(PictureViewer viewer)
    {
        _viewer = viewer;
    }

    public void UpdateAll()
    {
        if (_viewer == null)
            return;

        _zoomLabel.Text = $"Zoom: {((1 - (_viewer.Zoom - PictureViewer.MinZoom) / (PictureViewer.MaxZoom - PictureViewer.MinZoom)) * 100.0f):0.00}%";
        _recenterButton.Disabled = _viewer != null ? !_viewer.CanBeRecentered() : false;
    }
}
