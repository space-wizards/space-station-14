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
    /// Populates an ingredient grid with interactive entity buttons.
    /// </summary>
    public static void PopulateIngredientsGrid(
        GridContainer grid,
        IEntityManager entMan,
        IEnumerable<EntityUid> entities,
        Action<EntityUid> onPressed)
    {
        grid.Children.Clear();

        foreach (var entity in entities)
        {
            if (entMan.Deleted(entity))
                continue;

            var button = BuildIngredientButton(entMan, entity);
            button.OnPressed += _ => onPressed(entity);
            grid.AddChild(button);
        }
    }

    private static Button BuildIngredientButton(IEntityManager entMan, EntityUid entity)
    {
        var entityName = entMan.GetComponent<MetaDataComponent>(entity).EntityName;
        var visual = BuildIngredientVisual(entMan, entity, entityName);
        var button = new Button
        {
            SetSize = IngredientButtonSize,
            RectClipContent = true,
            StyleClasses = { StyleClass.ButtonOpenBoth },
            ToolTip = entityName,
            Modulate = Color.White.WithAlpha(0.5f)
        };

        button.AddChild(visual);
        return button;
    }

    private static Control BuildIngredientVisual(IEntityManager entMan, EntityUid entity, string entityName)
    {
        if (entMan.HasComponent<SpriteComponent>(entity))
        {
            var spriteView = new SpriteView
            {
                Stretch = SpriteView.StretchMode.Fill
            };
            spriteView.SetEntity(entity);
            return spriteView;
        }

        if (entMan.TryGetComponent<IconComponent>(entity, out var icon))
        {
            return new TextureRect
            {
                Texture = entMan.System<SpriteSystem>().GetIcon(icon),
                Stretch = TextureRect.StretchMode.KeepAspectCentered
            };
        }

        return new Label
        {
            Text = entityName,
            ClipText = true,
            Align = Label.AlignMode.Center,
            Margin = new Thickness(4, 0)
        };
    }
}
