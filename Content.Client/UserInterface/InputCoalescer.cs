using System.Diagnostics.CodeAnalysis;

namespace Content.Client.UserInterface;

/// <summary>
/// A simple utility class to "coalesce" multiple input events into a single one, fired later.
/// </summary>
/// <typeparam name="T"></typeparam>
public struct InputCoalescer<T>
{
    public bool IsModified;
    public T LastValue;

    /// <summary>
    /// Replace the value in the <see cref="InputCoalescer{T}"/>. This sets <see cref="IsModified"/> to true.
    /// </summary>
    public void Set(T value)
    {
        LastValue = value;
        IsModified = true;
    }

    /// <summary>
    /// Check if the <see cref="InputCoalescer{T}"/> has been modified.
    /// If it was, return the value and clear <see cref="IsModified"/>.
    /// </summary>
    /// <returns>True if the value was modified since the last check.</returns>
    public bool CheckIsModified([MaybeNullWhen(false)] out T value)
    {
        if (IsModified)
        {
            value = LastValue;
            IsModified = false;
            return true;
        }

        value = default;
        return IsModified;
    }
}
