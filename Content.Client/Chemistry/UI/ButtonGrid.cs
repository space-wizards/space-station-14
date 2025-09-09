using System.Linq;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Chemistry.UI;

/// <summary>
///     Creates a grid of buttons given a comma-seperated list of Text
/// </summary>
public sealed class ButtonGrid : GridContainer
{
    private List<string> _buttonList = [];

    /// <summary>
    /// A list of strings to use for each button. These will be inserted sequentially.
    /// Duplicate strings are probably a bad idea.
    /// </summary>
    public List<string> ButtonList
    {
        get => _buttonList;
        set
        {
            if (_buttonList.SequenceEqual(value))
                return;
            _buttonList = value;
            Update();
        }
    }

    public bool RadioGroup { get; set; } = false;

    private string? _selected;

    /// <summary>
    /// Which button is currently selected. Only matters when <see cref="RadioGroup" /> is true.
    /// </summary>
    public string? Selected
    {
        get => _selected;
        set
        {
            if (RadioGroup && _selected is not null && _buttons.TryGetValue(_selected, out var oldButton))
                oldButton.Pressed = false;

            _selected = value;

            if (RadioGroup && _selected is not null && _buttons.TryGetValue(_selected, out var newButton))
                newButton.Pressed = true;
        }
    }

    public Action<string>? OnButtonPressed;

    /// <seealso cref="GridContainer.Columns"/>
    public new int Columns
    {
        get => base.Columns;
        set
        {
            base.Columns = value;
            Update();
        }
    }

    /// <seealso cref="GridContainer.Rows"/>
    public new int Rows
    {
        get => base.Rows;
        set
        {
            base.Rows = value;
            Update();
        }
    }

    private readonly Dictionary<string, Button> _buttons = [];

    private void Update()
    {
        if (ButtonList.Count == 0)
            return;

        Children.Clear();
        _buttons.Clear();

        var i = 0;
        var group = new ButtonGroup();

        foreach (var button in ButtonList)
        {
            var btn = new Button { Text = button };
            btn.OnPressed += _ =>
            {
                if (RadioGroup)
                    btn.Pressed = true;
                Selected = button;
                OnButtonPressed?.Invoke(button);
            };

            if (button == Selected)
                btn.Pressed = true;

            btn.Group = group;

            var row = i / Columns;
            var col = i % Columns;
            var last = i == ButtonList.Count - 1;
            var lastCol = i == Columns - 1;
            var lastRow = row == ButtonList.Count / Columns - 1;

            if (row == 0 && (lastCol || last))
                btn.AddStyleClass("OpenLeft");
            else if (col == 0 && lastRow)
                btn.AddStyleClass("OpenRight");
            else
                btn.AddStyleClass("OpenBoth");

            Children.Add(btn);
            _buttons.Add(button, btn);

            i++;
        }
    }
}
