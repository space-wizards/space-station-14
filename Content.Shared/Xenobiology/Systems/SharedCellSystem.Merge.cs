namespace Content.Shared.Xenobiology.Systems;

public abstract partial class SharedCellSystem
{
    public static float GetMergedStability(Cell cellA, Cell cellB)
    {
        var max = Math.Max(cellA.Stability, cellB.Stability);
        var delta = Math.Abs(cellA.Stability - cellB.Stability);

        // This is a simple but not the best implementation,
        // I think more thought should be given to this formula
        return max * (1 - delta);
    }

    public static string GetMergedName(Cell cellA, Cell cellB)
    {
        var nameA = cellA.Name[..(cellA.Name.Length / 2)];
        var nameB = cellB.Name[(cellA.Name.Length / 2)..];
        return $"{nameA}{nameB}";
    }

    public static Color GetMergedColor(Cell cellA, Cell cellB)
    {
        return Color.InterpolateBetween(cellA.Color, cellB.Color, 0.5f);
    }

    public static int GetMergedCost(Cell cellA, Cell cellB)
    {
        return cellA.Cost + cellB.Cost;
    }
}
