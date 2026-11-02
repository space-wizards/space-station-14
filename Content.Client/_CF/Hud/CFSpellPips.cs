using System.Numerics;
using Content.Shared.CF.Character;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CF.Hud;

/// <summary>
/// Spell-slot pips for each rank the class actually has, plus focus points.
/// A pip is lit while the slot is ready and gray when it was marked expended on the character sheet.
/// Cantrips are at-will, so they are not drawn here.
/// </summary>
public sealed class CFSpellPips : BoxContainer
{
    private string _shown = "";

    public CFSpellPips()
    {
        Orientation = BoxContainer.LayoutOrientation.Horizontal;
        SeparationOverride = 8;
        Visible = false;
    }

    public void SetBook(CFSpellbookComponent? book)
    {
        var key = Key(book);
        if (key == _shown)
            return;

        _shown = key;
        RemoveAllChildren();
        if (book == null || key.Length == 0)
        {
            Visible = false;
            return;
        }

        for (var rank = 1; rank < book.Slots.Count; rank++)
        {
            var count = book.Slots[rank];
            if (count <= 0)
                continue;

            var spent = rank < book.Expended.Count ? book.Expended[rank] : 0;
            AddChild(Column(rank.ToString(), Loc.GetString("cf-hud-rank", ("rank", rank)), count, spent));
        }

        if (book.FocusPoints > 0)
            AddChild(Column("F", Loc.GetString("cf-hud-focus"), book.FocusPoints, book.FocusSpent));

        Visible = ChildCount > 0;
    }

    private static string Key(CFSpellbookComponent? book)
    {
        if (book == null)
            return "";

        var any = book.FocusPoints > 0;
        for (var rank = 1; rank < book.Slots.Count; rank++)
        {
            if (book.Slots[rank] > 0)
                any = true;
        }

        if (!any)
            return "";

        return string.Join(',', book.Slots) + "|" + string.Join(',', book.Expended) + "|" + book.FocusPoints + "|" + book.FocusSpent;
    }

    private static BoxContainer Column(string label, string tooltip, int count, int spent)
    {
        var column = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 2,
            Align = BoxContainer.AlignMode.Center,
            ToolTip = tooltip,
        };
        column.AddChild(new Label
        {
            Text = label,
            HorizontalAlignment = HAlignment.Center,
            ToolTip = tooltip,
        });

        var grid = new GridContainer
        {
            Columns = 2,
            HSeparationOverride = 2,
            VSeparationOverride = 2,
        };
        var ready = Math.Max(0, count - Math.Max(0, spent));
        for (var i = 0; i < count; i++)
        {
            grid.AddChild(new PanelContainer
            {
                MinSize = new Vector2(16, 16),
                PanelOverride = Box(i < ready),
                ToolTip = tooltip,
            });
        }

        column.AddChild(grid);
        return column;
    }

    private static StyleBoxFlat Box(bool ready)
    {
        return new StyleBoxFlat
        {
            BackgroundColor = ready ? new Color(64, 140, 210) : new Color(48, 48, 54),
            BorderColor = ready ? new Color(186, 220, 245) : new Color(90, 90, 98),
            BorderThickness = new Thickness(1),
        };
    }
}
