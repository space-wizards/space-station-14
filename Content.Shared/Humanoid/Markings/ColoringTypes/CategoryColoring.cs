using System.Linq;

namespace Content.Shared.Humanoid.Markings;

/// <summary>
///     Colors marking in color of first defined marking from specified category (in e.x. from Hair category)
/// </summary>
public sealed partial class CategoryColoring : LayerColoringType
{
    [DataField("category", required: true)]
    public MarkingCategories Category;

    public override Color? GetCleanColor(Color? skin, Color? eyes, MarkingSet markingSet)
    {
        Color? outColor = null;
        if (markingSet.TryGetCategory(Category, out var markings) &&
            markings.Count > 0)
        {
            outColor = markings[0].MarkingColors.FirstOrDefault();
        }

        return outColor;
    }
}
