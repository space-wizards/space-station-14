using Robust.Shared.Random;

namespace Content.Shared.Starlight;

public static class ListExtensions
{
    public static void RemoveSwapBack<T>(this List<T> list, int index)
    {
        var last = list.Count - 1;
        list[index] = list[last];
        list.RemoveAt(last);
    }
}