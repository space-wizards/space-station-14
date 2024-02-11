using Content.Client.Pinpointer.UI;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Medical.CrewMonitoring;

public sealed partial class CrewMonitoringNavMapControl : NavMapControl
{
    public NetEntity? Focus;
    public Dictionary<NetEntity, string> LocalizedNames = new();

    private Label _trackedEntityLabel;
    private PanelContainer _trackedEntityPanel;

    public CrewMonitoringNavMapControl() : base()
    {
        WallColor = new Color(192, 122, 196);
        TileColor = new(71, 42, 72);
        _backgroundColor = Color.FromSrgb(TileColor.WithAlpha(_backgroundOpacity));

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
                BackgroundColor = _backgroundColor,
            },

            Margin = new Thickness(5f, 10f),
            HorizontalAlignment = HAlignment.Left,
            VerticalAlignment = VAlignment.Bottom,
            Visible = false,
        };

        _trackedEntityPanel.AddChild(_trackedEntityLabel);
        this.AddChild(_trackedEntityPanel);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

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

            if (!LocalizedNames.TryGetValue(netEntity, out var name))
                name = "Unknown";

            var message = name + "\nLocation: [x = " + MathF.Round(blip.Coordinates.X) + ", y = " + MathF.Round(blip.Coordinates.Y) + "]";

            _trackedEntityLabel.Text = message;
            _trackedEntityPanel.Visible = true;

            return;
        }

        _trackedEntityLabel.Text = string.Empty;
        _trackedEntityPanel.Visible = false;
    }
}
