using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Displays a single-select list of category buttons.
/// Categories may use either text or an icon, with their action invoked when selected.
/// </summary>
public sealed class CategorySelector : GridContainer
{
    public string? ButtonStyleClass { get; set; }
    public int ButtonSize { get; set; }

    /// <summary>
    /// Rebuilds the category buttons and marks <paramref name="selected"/> as pressed without invoking its action.
    /// </summary>
    public void SetCategories(IReadOnlyList<CategorySelectorEntry> categories, CategorySelectorEntry? selected)
    {
        RemoveAllChildren();

        var group = new ButtonGroup();

        foreach (var category in categories)
        {
            var button = CreateButton(category, group);
            button.OnPressed += _ => category.OnSelected();

            AddChild(button);

            if (ReferenceEquals(category, selected))
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
/// Describes a category shown by <see cref="CategorySelector"/>.
/// </summary>
public sealed class CategorySelectorEntry(
    string name,
    Action onSelected,
    Texture? icon = null,
    Vector2? iconSize = null)
{
    public string Name { get; } = name;
    public Action OnSelected { get; } = onSelected;
    public Texture? Icon { get; } = icon;
    public Vector2? IconSize { get; } = iconSize;
}
