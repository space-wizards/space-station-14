using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Buffers;

/// <summary>
/// An array pool that contains a fixed amount of arrays with a fixed size.
/// Useful for cases where you need to easily manage large amounts of buffers with fixed size.
/// </summary>
/// <remarks>
/// Works similar to <see cref="RobustArrayPool{T}"/>, but has more strict checks that don't allow to return arrays with a wrong size.
/// </remarks>
public sealed class ConstantArrayPool<T> : IRobustArrayPool<T>
{
    public T[][] Buffer { get; set; }

    /// <summary>
    /// Amount of remaining arrays in the pool.
    /// </summary>
    public int Length { get; set; }

    public Func<T>? Factory { get; set; }

    /// <summary>
    /// Size of the pooled arrays.
    /// </summary>
    public int ArraySize { get; }

    public ConstantArrayPool(int arraySize, int maxBucketSize, Func<T>? factory = null, bool init = false)
    {
        Buffer = new T[maxBucketSize][];
        Factory = factory;
        Length = maxBucketSize - 1;
        ArraySize = arraySize;

        for (int i = 0; i < maxBucketSize; i++)
        {
            Buffer[i] = new T[arraySize];

            if(!init || factory == null)
                continue;

            for (int j = 0; j < arraySize; j++)
            {
                Buffer[j][i] = factory();
            }
        }
    }

    /// <summary>
    /// Takes an array from the pool.
    /// </summary>
    /// <returns>An array from the pool.</returns>
    public T[] Rent()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(Length);
        var objectSelected = Buffer[Length];
        Length--;
        return objectSelected;
    }

    public T[] Rent(int minSize)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minSize, ArraySize);
        return Rent();
    }

    /// <summary>
    /// Tries to get an array from the pool.
    /// </summary>
    /// <param name="obj">An available array from this pool.</param>
    /// <returns>True if the array was found successfully.</returns>
    public bool TryRent([NotNullWhen(true)] out T[]? obj)
    {
        obj = null;
        if (Length < 0)
            return false;

        var objectSelected = Buffer[Length];
        Length--;
        obj = objectSelected;
        return true;
    }

    public bool TryRent(int minSize, [NotNullWhen(true)] out T[]? obj)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minSize, ArraySize);
        return TryRent(out obj);
    }

    /// <summary>
    /// Returns an array back to the pool.
    /// </summary>
    /// <remarks>
    /// The array size has to be equal to the <see cref="ArraySize"/> of the pool.
    /// </remarks>
    /// <param name="obj">An array to return back.</param>
    public void Return(T[] obj)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(obj.Length, ArraySize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Length + 1, ArraySize);
        Length++;
        Buffer[Length] = obj;
    }

    /// <summary>
    /// Returns an array back to the pool and clears its contents back to the default state.
    /// If <see cref="Factory"/> is not null, also initializes each element in the array.
    /// </summary>
    /// <remarks>
    /// The array size has to be equal to the <see cref="ArraySize"/> of the pool.
    /// </remarks>
    /// <param name="obj">An array to return back.</param>
    public void ReturnClean(T[] obj)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(obj.Length, ArraySize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Length + 1, ArraySize);

        var objSpan = obj.AsSpan();
        objSpan.Clear();
        if (Factory != null)
            objSpan.Fill(Factory());
        objSpan.CopyTo(obj);

        Length++;
        Buffer[Length] = obj;
    }
}
