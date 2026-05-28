using System.Numerics;
using Content.Client.Stylesheets;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Kitchen.UI;

/// <summary>
/// Helper class for populating ingredient grids.
/// </summary>
public static class IngredientGridHelper
{
    private static readonly Vector2 IngredientButtonSize = new(75, 75);

    /// <summary>
    /// Populates the grid with buttons representing ingredients.
    /// Each button shows the entity's sprite and allows ejection.
    /// </summary>
    /// <param name="grid">The grid container to populate.</param>
    /// <param name="entMan">The entity manager.</param>
    /// <param name="entities">Collection of entities to display.</param>
    /// <param name="onEject">Action to perform when an ingredient is ejected.</param>
    /// <param name="emptyText">Text to show when there are no visible ingredients.</param>
    /// <returns>The populated grid container.</returns>
    public static void PopulateIngredientsGrid(
        GridContainer grid,
        IEntityManager entMan,
        IEnumerable<EntityUid> entities,
        Action<NetEntity> onEject,
        string? emptyText = null)
    {
        grid.Children.Clear();
        var added = false;

        foreach (var entity in entities)
        {
            if (entMan.Deleted(entity))
                continue;

            if (!entMan.HasComponent<SpriteComponent>(entity))
                continue;

            var button = new Button
            {
                SetSize = IngredientButtonSize,
                RectClipContent = true,
                StyleClasses = { "OpenBoth" },
                ToolTip = entMan.GetComponent<MetaDataComponent>(entity).EntityName,
                Modulate = Color.White.WithAlpha(0.5f)
            };

            var spriteView = new SpriteView
            {
                Stretch = SpriteView.StretchMode.Fill
            };
            spriteView.SetEntity(entity);

            button.AddChild(spriteView);

            button.OnPressed += _ =>
            {
                onEject(entMan.GetNetEntity(entity));
            };

            grid.AddChild(button);
            added = true;
        }

        if (!added && emptyText != null)
        {
            grid.AddChild(new Label
            {
                Text = emptyText,
                StyleClasses = { StyleClass.LabelWeak },
                HorizontalAlignment = Control.HAlignment.Center,
                VerticalAlignment = Control.VAlignment.Center
            });
        }
    }
}
