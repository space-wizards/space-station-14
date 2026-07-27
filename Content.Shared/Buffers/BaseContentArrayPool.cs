using System.Diagnostics.Contracts;

namespace Content.Shared.Buffers;

/// <summary>
/// A version of array pool that can have different implementations on server and client sides.
/// </summary>
/// <remarks>
/// TODO this should be replaced by a more performant and flexible version in RT.
/// </remarks>
public abstract class BaseContentArrayPool<T>
{
    /// <summary>
    /// Takes an array from the pool that has at least the specified size.
    /// </summary>
    /// <param name="minSize">Minimal size of an array.</param>
    /// <returns>An array from the pool.</returns>
    [Pure]
    public abstract T[] Rent(int minSize);

    /// <summary>
    /// Returns an array back to the pool.
    /// </summary>
    /// <param name="obj">An array to return back.</param>
    public abstract void Return(T[] obj);
}
