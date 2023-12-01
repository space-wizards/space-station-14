using System.Linq;

namespace Content.Shared.Storage;

public static class StorageHelper
{
    public static Box2i GetBoundingBox(this IReadOnlyList<Box2i> boxes)
    {
        if (boxes.Count == 0)
            return new Box2i();

        var minBottom = boxes.Min(x => x.Bottom);
        var minLeft = boxes.Min(x => x.Left);
        var maxTop = boxes.Max(x => x.Top);
        var maxRight = boxes.Max(x => x.Right);
        return new Box2i(new Vector2i(minLeft, minBottom), new Vector2i(maxRight, maxTop));
    }

    public static int GetArea(this IReadOnlyList<Box2i> boxes)
    {
        var area = 0;
        var bounding = boxes.GetBoundingBox();
        for (var y = bounding.Bottom; y <= bounding.Top; y++)
        {
            for (var x = bounding.Left; x <= bounding.Right; x++)
            {
                if (boxes.Contains(x, y))
                    area++;
            }
        }

        return area;
    }

    public static bool Contains(this IReadOnlyList<Box2i> boxes, int x, int y)
    {
        foreach (var box in boxes)
        {
            if (box.Contains(x, y))
                return true;
        }

        return false;
    }

    public static bool Contains(this IReadOnlyList<Box2i> boxes, Vector2i point)
    {
        foreach (var box in boxes)
        {
            if (box.Contains(point))
                return true;
        }

        return false;
    }
}
