using Content.Client.Pinpointer.UI;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.GatewayStation;

public sealed partial class StationGatewayNavMapControl : NavMapControl
{
    public NetEntity? Focus;

    private Label _trackedEntityLabel;
    private PanelContainer _trackedEntityPanel;
    public StationGatewayNavMapControl() : base()
    {
        WallColor = new Color(32, 96, 128);
        TileColor = new Color(12, 50, 69);
        BackgroundColor = Color.FromSrgb(TileColor.WithAlpha(BackgroundOpacity));

        _trackedEntityLabel = new Label
        {
            Margin = new Thickness(10f, 8f),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Modulate = Color.White,
        };
        _trackedEntityPanel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = BackgroundColor,
            },

            Margin = new Thickness(5f, 10f),
            HorizontalAlignment = HAlignment.Left,
            VerticalAlignment = VAlignment.Bottom,
            Visible = false,
        };

        _trackedEntityPanel.AddChild(_trackedEntityLabel);
        this.AddChild(_trackedEntityPanel);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (Focus == null)
        {
            _trackedEntityLabel.Text = string.Empty;
            _trackedEntityPanel.Visible = false;

            return;
        }

        foreach ((var netEntity, var blip) in TrackedEntities)
        {
            if (netEntity != Focus)
                continue;


            var message = "lol idk" + "\nLocation: [x = " + MathF.Round(blip.Coordinates.X) + ", y = " + MathF.Round(blip.Coordinates.Y) + "]";

            _trackedEntityLabel.Text = message;
            _trackedEntityPanel.Visible = true;

            return;
        }

        _trackedEntityLabel.Text = string.Empty;
        _trackedEntityPanel.Visible = false;
    }
}
