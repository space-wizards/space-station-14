using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Displays mutually exclusive category buttons using either text or an icon.
/// Pressing a button invokes the corresponding category action.
/// </summary>
public sealed class CategorySelector : GridContainer
{
    public string? ButtonStyleClass { get; set; }
    public int ButtonSize { get; set; }

    /// <summary>
    /// Rebuilds the buttons. Marks the button at <paramref name="selectedIndex"/>
    /// as pressed without invoking its action.
    /// </summary>
    public void SetCategories(IReadOnlyList<CategorySelectorEntry> categories, int selectedIndex)
    {
        RemoveAllChildren();

        var group = new ButtonGroup();

        for (var index = 0; index < categories.Count; index++)
        {
            var button = CreateButton(categories[index], group);

            AddChild(button);

            if (index == selectedIndex)
                button.Pressed = true;
        }
    }

    private Button CreateButton(CategorySelectorEntry category, ButtonGroup group)
    {
        var button = new Button
        {
            Group = group,
            ToolTip = category.Icon == null ? null : category.Name
        };
        button.OnPressed += _ => category.OnSelected();

        if (ButtonSize > 0)
            button.SetSize = new Vector2(ButtonSize);

        if (ButtonStyleClass != null)
            button.AddStyleClass(ButtonStyleClass);

        if (category.Icon == null)
        {
            button.Text = category.Name;
            return button;
        }

        button.AddChild(new TextureRect
        {
            Texture = category.Icon,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            SetSize = category.IconSize ?? new Vector2(32),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center
        });
        return button;
    }
}

/// <summary>
/// Content and action for one category button.
/// </summary>
public sealed record CategorySelectorEntry(
    string Name,
    Action OnSelected,
    Texture? Icon = null,
    Vector2? IconSize = null);
