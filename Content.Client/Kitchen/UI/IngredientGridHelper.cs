using System.Linq;
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
    /// Each button shows the entity's visual and allows ejection.
    /// </summary>
    /// <param name="grid">The grid container to populate.</param>
    /// <param name="entMan">The entity manager.</param>
    /// <param name="entities">Collection of entities to display.</param>
    /// <param name="onEject">Action to perform when an ingredient is ejected.</param>
    public static void PopulateIngredientsGrid(
        GridContainer grid,
        IEntityManager entMan,
        IEnumerable<EntityUid> entities,
        Action<EntityUid> onEject)
    {
        grid.Children.Clear();

        foreach (var entity in entities.Where(entity => !entMan.Deleted(entity)))
        {
            var button = BuildIngredientButton(entMan, entity);
            button.OnPressed += _ => onEject(entity);
            grid.AddChild(button);
        }
    }

    /// <summary>
    /// Populates the grid with buttons representing networked ingredients.
    /// Each button shows the entity's visual and allows ejection.
    /// </summary>
    /// <param name="grid">The grid container to populate.</param>
    /// <param name="entMan">The entity manager.</param>
    /// <param name="entities">Collection of networked entities to display.</param>
    /// <param name="onEject">Action to perform when an ingredient is ejected.</param>
    // TODO: Revisit this overload once microwave uses predicted/local state. See microwave prediction PR #43129.
    public static void PopulateIngredientsGrid(
        GridContainer grid,
        IEntityManager entMan,
        IEnumerable<NetEntity> entities,
        Action<NetEntity> onEject)
    {
        grid.Children.Clear();

        foreach (var netEntity in entities)
        {
            var button = BuildIngredientButton(entMan, netEntity);
            button.OnPressed += _ => onEject(netEntity);
            grid.AddChild(button);
        }
    }

    private static Button BuildIngredientButton(IEntityManager entMan, EntityUid entity)
    {
        var entityName = entMan.GetComponent<MetaDataComponent>(entity).EntityName;
        var visual = BuildIngredientVisual(entMan, entity, entityName);
        return BuildIngredientButton(visual, entityName);
    }

    private static Button BuildIngredientButton(IEntityManager entMan, NetEntity netEntity)
    {
        if (entMan.TryGetEntity(netEntity, out var entity) && !entMan.Deleted(entity.Value))
            return BuildIngredientButton(entMan, entity.Value);

        var spriteView = BuildSpriteView();
        spriteView.SetEntity(netEntity);
        return BuildIngredientButton(spriteView);
    }

    private static Button BuildIngredientButton(Control visual, string? toolTip = null)
    {
        var button = new Button
        {
            SetSize = IngredientButtonSize,
            RectClipContent = true,
            StyleClasses = { StyleClass.ButtonOpenBoth },
            ToolTip = toolTip,
            Modulate = Color.White.WithAlpha(0.5f)
        };

        button.AddChild(visual);
        return button;
    }

    private static Control BuildIngredientVisual(IEntityManager entMan, EntityUid entity, string entityName)
    {
        if (entMan.HasComponent<SpriteComponent>(entity))
        {
            var spriteView = BuildSpriteView();
            spriteView.SetEntity(entity);
            return spriteView;
        }

        if (entMan.TryGetComponent<IconComponent>(entity, out var icon))
        {
            return new TextureRect
            {
                Texture = entMan.System<SpriteSystem>().GetIcon(icon),
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                HorizontalExpand = true,
                VerticalExpand = true
            };
        }

        return new Label
        {
            Text = entityName,
            ClipText = true,
            HorizontalExpand = true,
            VerticalExpand = true,
            HorizontalAlignment = Control.HAlignment.Center,
            VerticalAlignment = Control.VAlignment.Center,
            Margin = new Thickness(4, 0)
        };
    }

    private static SpriteView BuildSpriteView()
    {
        return new SpriteView
        {
            Stretch = SpriteView.StretchMode.Fill,
            HorizontalExpand = true,
            VerticalExpand = true
        };
    }
}
