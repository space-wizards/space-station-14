using System.Buffers;

namespace Content.Shared.Buffers;

/// <summary>
/// An array pool that pools arrays that are at least a certain length.
/// Automatically creates new arrays if there are not enough of them, and doesn't have a limit to store arrays.
/// This is the most universal solution for a sandboxed <see cref="ArrayPool{T}"/>.
/// </summary>
public sealed class RobustArrayPool<T> : IRobustArrayPool<T>
{
    public T[][] Buffer { get; set; }

    /// <summary>
    /// An index of the last currently available array in the <see cref="Buffer"/>.
    /// </summary>
    public int Length { get; set; }

    public Func<T>? Factory { get; set; }

    public RobustArrayPool(int startArraySize, int startBucketSize, Func<T>? factory = null, bool init = false)
    {
        Buffer = new T[startBucketSize][];
        Factory = factory;
        Length = startBucketSize - 1;

        for (int i = 0; i < startBucketSize; i++)
        {
            Buffer[i] = new T[startArraySize];

            if(!init || factory == null)
                continue;

            for (int j = 0; j < startArraySize; j++)
            {
                Buffer[j][i] = factory();
            }
        }
    }

    public T[] Rent()
    {
        if (Length < 0)
            return Array.Empty<T>();

        var objectSelected = Buffer[Length];
        Length--;
        return objectSelected;
    }

    public T[] Rent(int minSize)
    {
        // No arrays - create a new one
        if (Length < 0)
            return new T[minSize];

        // Try to find an array with specified size or bigger
        for (int i = 0; i < Length; i++)
        {
            if (Buffer[i].Length <= minSize)
                continue;

            return Buffer[i];
        }

        // Resize an already existing array in case if all of them are too small
        var selected = Buffer[Length];
        Length--;
        Array.Resize(ref selected, minSize * 2);
        return selected;
    }

    public void Return(T[] obj)
    {
        Length++;
        var buffer = Buffer;
        if (buffer.Length == Length)
            Array.Resize(ref buffer, Length);

        buffer[Length] = obj;
    }

    public void ReturnClean(T[] obj)
    {
        var objSpan = obj.AsSpan();
        objSpan.Clear();
        if (Factory != null)
            objSpan.Fill(Factory());
        objSpan.CopyTo(obj);

        Length++;
        var buffer = Buffer;
        if (buffer.Length == Length)
            Array.Resize(ref buffer, Length);

        Buffer[Length] = obj;
    }
}
