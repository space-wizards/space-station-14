namespace Content.Shared.Storage;

public static class StorageHelper
{
    public static Box2i GetBoundingBox(this IReadOnlyList<Box2i> boxes)
    {
        if (boxes.Count == 0)
            return new Box2i();

        var firstBox = boxes[0];

        if (boxes.Count == 1)
            return firstBox;

        var bottom = firstBox.Bottom;
        var left = firstBox.Left;
        var top = firstBox.Top;
        var right = firstBox.Right;

        for (var i = 1; i < boxes.Count; i++)
        {
            var box = boxes[i];

            if (bottom > box.Bottom)
                bottom = box.Bottom;

            if (left > box.Left)
                left = box.Left;

            if (top < box.Top)
                top = box.Top;

            if (right < box.Right)
                right = box.Right;
        }
        return new Box2i(left, bottom, right, top);
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
