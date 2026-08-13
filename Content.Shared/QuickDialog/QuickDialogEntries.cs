using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.QuickDialog;

/// <summary>
/// An entry in a quick dialog.
/// </summary>
public interface IQuickDialogEntry
{
    /// <summary>
    /// The prompt to show the user.
    /// </summary>
    string Prompt { get; init; }

    /// <summary>
    ///
    /// </summary>
    float Width { get; init; }

    /// <summary>
    ///
    /// </summary>
    bool Required { get; init; }

    /// <summary>
    /// String to replace the type-specific placeholder with.
    /// </summary>
    /// <returns></returns>
    object? GetPlaceholder();

    /// <summary>
    ///
    /// </summary>
    /// <param name="toParse"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    bool TryParse(object? toParse, [NotNullWhen(true)] out object? output);
}

/// <summary>
///
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="T1"></typeparam>
[Serializable, NetSerializable]
public abstract class BaseQuickDialogEntry<T, T1> : IQuickDialogEntry
    where T : notnull
    where T1 : notnull, INumber<T1>
{
    /// <inheritdoc/>
    public string Prompt { get; init; } = "None";

    /// <inheritdoc/>
    public float Width { get; init; } = 100f;

    /// <inheritdoc/>
    public bool Required { get; init; } = true;

    /// <inheritdoc/>
    public object? GetPlaceholder()
    {
        return Placeholder;
    }

    /// <inheritdoc/>
    bool IQuickDialogEntry.TryParse(object? toParse, [NotNullWhen(true)] out object? output)
    {
        var result = TryParse(toParse, out var typedOutput);
        output = typedOutput;
        return result;
    }

    /// <summary>
    ///
    /// </summary>
    public abstract T1 Min { get; init; }

    /// <summary>
    ///
    /// </summary>
    public abstract T1 Max { get; init; }

    /// <summary>
    ///
    /// </summary>
    public T? Placeholder { get; init; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="toParse"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    public abstract bool TryParse(object? toParse, [NotNullWhen(true)] out T? output);
}

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public sealed class QuickDialogEntryString : BaseQuickDialogEntry<string, int>
{
    /// <inheritdoc/>
    public override int Min { get; init; } = 0;

    /// <inheritdoc/>
    public override int Max { get; init; } = int.MaxValue;

    /// <inheritdoc/>
    public override bool TryParse(object? toParse, [NotNullWhen(true)] out string? output)
    {
        output = null;
        if (toParse is not string value)
            return false;

        value = value.Trim();
        if (string.IsNullOrEmpty(value))
            return false;

        output = value;
        return true;
    }
}

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public sealed class QuickDialogEntryInt : BaseQuickDialogEntry<int, int>
{
    /// <inheritdoc/>
    public override int Min { get; init; } = int.MinValue;

    /// <inheritdoc/>
    public override int Max { get; init; } = int.MaxValue;

    /// <inheritdoc/>
    public override bool TryParse(object? toParse, out int output)
    {
        output = 0;
        if (toParse is not int value)
            return false;

        output = Math.Clamp(value, Min, Max);
        return true;
    }
}
