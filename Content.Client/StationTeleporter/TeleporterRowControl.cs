using Content.Client.Message;
using Content.Shared.StationTeleporter;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.StationTeleporter;

/// <summary>
/// A single row in <see cref="StationTeleporterConsoleWindow"/>'s teleporter list: the name label plus the
/// locate/link buttons for one <see cref="StationTeleporterStatus"/>.
/// </summary>
public sealed class TeleporterRowControl : PanelContainer
{
    private static readonly Color LinkedBackgroundColor = new(18, 61, 82); //TODO: UI palette usage
    private static readonly Color UnlinkedBackgroundColor = new(30, 30, 34); //TODO: UI palette usage
    private static readonly Color SelectedBackgroundColor = new(49, 117, 7); //TODO: UI palette usage

    public readonly NetEntity TeleporterUid;
    public readonly EntityCoordinates? Coordinates;
    public readonly TeleporterButton LocateButton;
    public readonly TeleporterButton LinkButton;

    public TeleporterRowControl(StationTeleporterStatus teleporter, bool selected, EntityCoordinates? coordinates)
    {
        TeleporterUid = teleporter.TeleporterUid;
        Coordinates = coordinates;

        var linked = teleporter.LinkCoordinates is not null;

        var bgColor = linked ? LinkedBackgroundColor : UnlinkedBackgroundColor;
        if (selected)
            bgColor = SelectedBackgroundColor;

        HorizontalAlignment = HAlignment.Center;
        VerticalAlignment = VAlignment.Center;
        HorizontalExpand = true;
        Margin = new Thickness(10);
        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = bgColor,
            BorderColor = Color.Black,
            BorderThickness = new(2),
        };

        var mainBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };
        AddChild(mainBox);

        // Teleporter name
        var nameLabel = new RichTextLabel
        {
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Center,
            Margin = new Thickness(0, 5),
        };
        nameLabel.SetMarkup($"[bold]{teleporter.Name}[/bold]");
        mainBox.AddChild(nameLabel);

        // Left subpart
        var leftBox = new BoxContainer
        {
            SetWidth = 30,
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };
        mainBox.AddChild(leftBox);

        // Right subpart
        var rightBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };
        mainBox.AddChild(rightBox);

        // Locating button
        LocateButton = new TeleporterButton
        {
            Text = Loc.GetString("teleporter-console-user-interface-locate"),
            TeleporterUid = teleporter.TeleporterUid,
            Coordinates = coordinates,
            HorizontalAlignment = HAlignment.Right,
            SetWidth = 200f,
        };
        rightBox.AddChild(LocateButton);

        // Link/Unlink button
        var buttonLoc = "teleporter-console-user-interface-start-connection";
        if (!teleporter.Powered)
            buttonLoc = "teleporter-console-user-interface-no-power";
        else if (linked)
            buttonLoc = "teleporter-console-user-interface-cut-connection";

        LinkButton = new TeleporterButton
        {
            Text = Loc.GetString(buttonLoc),
            TeleporterUid = teleporter.TeleporterUid,
            Coordinates = coordinates,
            HorizontalAlignment = HAlignment.Right,
            SetWidth = 200f,
            Disabled = !teleporter.Powered,
        };
        rightBox.AddChild(LinkButton);
    }
}

public sealed class TeleporterButton : Button
{
    public int IndexInTable;
    public NetEntity TeleporterUid;
    public EntityCoordinates? Coordinates;
}
