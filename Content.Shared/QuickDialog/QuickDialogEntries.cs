using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.QuickDialog;

/// <summary>
///
/// </summary>
public interface IQuickDialogEntry
{
    /// <summary>
    ///
    /// </summary>
    Type Type { get; }

    /// <summary>
    ///
    /// </summary>
    LocId? Prompt { get; init; }

    /// <summary>
    ///
    /// </summary>
    LocId? Placeholder { get; init; }
}

/// <summary>
///
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="T1"></typeparam>
public interface IQuickDialogEntry<T, T1> : IQuickDialogEntry
    where T : INumber<T>
    where T1 : notnull
{
    /// <summary>
    ///
    /// </summary>
    (T Min, T Max) MinMax { get; init; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    bool TryParse(string input, [NotNullWhen(true)] out T1? output);
}

/// <summary>
///
/// </summary>
/// <param name="MinMax"></param>
/// <param name="Prompt"></param>
/// <param name="Placeholder"></param>
[Serializable, NetSerializable]
public readonly record struct QuickDialogEntryString((int Min, int Max) MinMax, LocId? Prompt = null, LocId? Placeholder = null) :
    IQuickDialogEntry<int, string>
{
    /// <inheritdoc/>
    public readonly Type Type => typeof(string);

    /// <inheritdoc/>
    public bool TryParse(string input, [NotNullWhen(true)] out string? output)
    {
        output = null;
        if (input.Length < MinMax.Min || input.Length > MinMax.Max)
            return false;

        output = input;
        return true;
    }
}

/// <summary>
///
/// </summary>
/// <param name="MinMax"></param>
/// <param name="Prompt"></param>
/// <param name="Placeholder"></param>
[Serializable, NetSerializable]
public readonly record struct QuickDialogEntryInt((int Min, int Max) MinMax, LocId? Prompt = null, LocId? Placeholder = null) :
    IQuickDialogEntry<int, int>
{
    /// <inheritdoc/>
    public readonly Type Type => typeof(int);

    /// <inheritdoc/>
    public bool TryParse(string input, out int output)
    {
        if (!int.TryParse(input, out output))
            return false;

        if (output < MinMax.Min || output > MinMax.Max)
            return false;

        return true;
    }
}

/// <summary>
///
/// </summary>
/// <param name="MinMax"></param>
/// <param name="Prompt"></param>
/// <param name="Placeholder"></param>
[Serializable, NetSerializable]
public readonly record struct QuickDialogEntryUInt((uint Min, uint Max) MinMax, LocId? Prompt = null, LocId? Placeholder = null) :
    IQuickDialogEntry<uint, uint>
{
    /// <inheritdoc/>
    public readonly Type Type => typeof(uint);

    /// <inheritdoc/>
    public bool TryParse(string input, out uint output)
    {
        if (!uint.TryParse(input, out output))
            return false;

        if (output < MinMax.Min || output > MinMax.Max)
            return false;

        return true;
    }
}

/// <summary>
///
/// </summary>
/// <param name="MinMax"></param>
/// <param name="Prompt"></param>
/// <param name="Placeholder"></param>
[Serializable, NetSerializable]
public readonly record struct QuickDialogEntryLong((long Min, long Max) MinMax, LocId? Prompt = null, LocId? Placeholder = null) :
    IQuickDialogEntry<long, long>
{
    /// <inheritdoc/>
    public readonly Type Type => typeof(long);

    /// <inheritdoc/>
    public bool TryParse(string input, out long output)
    {
        if (!long.TryParse(input, out output))
            return false;

        if (output < MinMax.Min || output > MinMax.Max)
            return false;

        return true;
    }
}

/// <summary>
///
/// </summary>
/// <param name="MinMax"></param>
/// <param name="Prompt"></param>
/// <param name="Placeholder"></param>
[Serializable, NetSerializable]
public readonly record struct QuickDialogEntryULong((ulong Min, ulong Max) MinMax, LocId? Prompt = null, LocId? Placeholder = null) :
    IQuickDialogEntry<ulong, ulong>
{
    /// <inheritdoc/>
    public readonly Type Type => typeof(ulong);

    /// <inheritdoc/>
    public bool TryParse(string input, out ulong output)
    {
        if (!ulong.TryParse(input, out output))
            return false;

        if (output < MinMax.Min || output > MinMax.Max)
            return false;

        return true;
    }
}

/// <summary>
///
/// </summary>
/// <param name="MinMax"></param>
/// <param name="Prompt"></param>
/// <param name="Placeholder"></param>
[Serializable, NetSerializable]
public readonly record struct QuickDialogEntryFloat((float Min, float Max) MinMax, LocId? Prompt = null, LocId? Placeholder = null) :
    IQuickDialogEntry<float, float>
{
    /// <inheritdoc/>
    public readonly Type Type => typeof(float);

    /// <inheritdoc/>
    public bool TryParse(string input, out float output)
    {
        if (!float.TryParse(input, out output))
            return false;

        if (output < MinMax.Min || output > MinMax.Max)
            return false;

        return true;
    }
}

/// <summary>
///
/// </summary>
/// <param name="MinMax"></param>
/// <param name="Prompt"></param>
/// <param name="Placeholder"></param>
[Serializable, NetSerializable]
public readonly record struct QuickDialogEntryDouble((double Min, double Max) MinMax, LocId? Prompt = null, LocId? Placeholder = null) :
    IQuickDialogEntry<double, double>
{
    /// <inheritdoc/>
    public readonly Type Type => typeof(double);

    /// <inheritdoc/>
    public bool TryParse(string input, out double output)
    {
        if (!double.TryParse(input, out output))
            return false;

        if (output < MinMax.Min || output > MinMax.Max)
            return false;

        return true;
    }
}
