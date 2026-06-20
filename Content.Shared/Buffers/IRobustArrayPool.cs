using System.Buffers;
using JetBrains.Annotations;

namespace Content.Shared.Buffers;

/// <summary>
/// An interface for different types of array pools that have special functionality, similar to <see cref="ArrayPool{T}"/>.
/// </summary>
/// <remarks>
/// This version doesn't violate sandbox checks, since it doesn't contain a Shared pool.
/// </remarks>
public interface IRobustArrayPool<T>
{
    /// <summary>
    /// The buffer that contains the arrays.
    /// </summary>
    protected T[][] Buffer { get; set; }

    /// <summary>
    /// Pointer index to the latest initialized element in the buffer.
    /// </summary>
    int Length { get; protected set; }

    /// <summary>
    /// Factory that initializes elements in the array when they are first created or returned with a clear parameter.
    /// </summary>
    protected Func<T>? Factory { get; set; }

    /// <summary>
    /// Takes an array from the pool, without caring about its size.
    /// </summary>
    /// <returns>An array from the pool.</returns>
    [Pure]
    T[] Rent();

    /// <summary>
    /// Takes an array from the pool that has at least the specified size.
    /// </summary>
    /// <param name="minSize">Minimal size of an array.</param>
    /// <returns>An array from the pool.</returns>
    [Pure]
    T[] Rent(int minSize);

    /// <summary>
    /// Returns an array back to the pool.
    /// </summary>
    /// <param name="obj">An array to return back.</param>
    void Return(T[] obj);

    /// <summary>
    /// Returns an array back to the pool and clears its contents back to the default state.
    /// If <see cref="Factory"/> is not null, also initializes each element in the array.
    /// </summary>
    /// <param name="obj">An array to return back.</param>
    void ReturnClean(T[] obj);
}
