using System.Linq;
using Content.Client.Stylesheets;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Atmos.UI;

/// <summary>
/// Displays a striped list of gases with localized names, mole amounts, and gas color markers.
/// </summary>
public sealed class GasDisplayControl : BoxContainer
{
    private readonly SharedAtmosphereSystem _atmosphere;
    private int _rowCount;

    public GasDisplayControl()
    {
        IoCManager.InjectDependencies(this);
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        _atmosphere = IoCManager.Resolve<IEntityManager>().System<SharedAtmosphereSystem>();
    }

    /// <summary>
    /// Removes all displayed gas rows and resets row striping.
    /// </summary>
    public void ClearDisplay()
    {
        RemoveAllChildren();
        _rowCount = 0;
    }

    /// <summary>
    /// Replaces the displayed gas rows with the supplied positive gas entries.
    /// </summary>
    /// <param name="gases">The gases and their amounts in moles.</param>
    public void Populate(IEnumerable<GasEntry> gases)
    {
        ClearDisplay();

        var entries = gases.Where(x => x.Amount > 0f).ToArray();

        foreach (var entry in entries)
        {
            var gas = _atmosphere.GetGas(entry.Gas);
            var rowColor = Color.FromHex(_rowCount++ % 2 == 0 ? "#202025" : "#1B1B1E");
            var row = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Children =
                {
                    new Label { Text = $"{Loc.GetString(gas.Name)}: " },
                    new Label
                    {
                        Text = $"{entry.Amount:0.##} {Loc.GetString("gas-analyzer-window-molarity-column-name")}",
                        StyleClasses = { StyleClass.LabelWeak },
                    },
                    new Control { HorizontalExpand = true },
                    new PanelContainer
                    {
                        VerticalExpand = true,
                        MinWidth = 4,
                        PanelOverride = new StyleBoxFlat(gas.Color),
                        Margin = new Thickness(0, 1),
                    },
                },
            };

            AddChild(new PanelContainer
            {
                PanelOverride = new StyleBoxFlat(rowColor),
                Children = { row },
            });
        }
    }
}
