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

    /// <summary>
    ///
    /// </summary>
    Vector2? Size { get; init; }
}

/// <summary>
///
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IQuickDialogEntry<T> : IQuickDialogEntry
    where T : INumber<T>
{
    /// <summary>
    ///
    /// </summary>
    T Min { get; init; }

    /// <summary>
    ///
    /// </summary>
    T Max { get; init; }
}

/// <summary>
///
/// </summary>
/// <param name="Min"></param>
/// <param name="Max"></param>
/// <param name="Prompt"></param>
/// <param name="Placeholder"></param>
/// <param name="Size"></param>
[Serializable, NetSerializable]
public readonly record struct QuickDialogEntryString(int Min, int Max, LocId? Prompt = null, LocId? Placeholder = null, Vector2? Size = null) :
    IQuickDialogEntry<int>
{
    /// <summary>
    ///
    /// </summary>
    public readonly Type Type => typeof(string);
}

/// <summary>
///
/// </summary>
/// <param name="Min"></param>
/// <param name="Max"></param>
/// <param name="Prompt"></param>
/// <param name="Placeholder"></param>
/// <param name="Size"></param>
[Serializable, NetSerializable]
public readonly record struct QuickDialogEntryInt(int Min, int Max, LocId? Prompt = null, LocId? Placeholder = null, Vector2? Size = null) :
    IQuickDialogEntry<int>
{
    /// <summary>
    ///
    /// </summary>
    public readonly Type Type => typeof(int);
}

/// <summary>
///
/// </summary>
/// <param name="Min"></param>
/// <param name="Max"></param>
/// <param name="Prompt"></param>
/// <param name="Placeholder"></param>
/// <param name="Size"></param>
[Serializable, NetSerializable]
public readonly record struct QuickDialogEntryUInt(uint Min, uint Max, LocId? Prompt = null, LocId? Placeholder = null, Vector2? Size = null) :
    IQuickDialogEntry<uint>
{
    /// <summary>
    ///
    /// </summary>
    public readonly Type Type => typeof(uint);
}

/// <summary>
///
/// </summary>
/// <param name="Min"></param>
/// <param name="Max"></param>
/// <param name="Prompt"></param>
/// <param name="Placeholder"></param>
/// <param name="Size"></param>
[Serializable, NetSerializable]
public readonly record struct QuickDialogEntryLong(long Min, long Max, LocId? Prompt = null, LocId? Placeholder = null, Vector2? Size = null) :
    IQuickDialogEntry<long>
{
    /// <summary>
    ///
    /// </summary>
    public readonly Type Type => typeof(long);
}

/// <summary>
///
/// </summary>
/// <param name="Min"></param>
/// <param name="Max"></param>
/// <param name="Prompt"></param>
/// <param name="Placeholder"></param>
/// <param name="Size"></param>
[Serializable, NetSerializable]
public readonly record struct QuickDialogEntryULong(ulong Min, ulong Max, LocId? Prompt = null, LocId? Placeholder = null, Vector2? Size = null) :
    IQuickDialogEntry<ulong>
{
    /// <summary>
    ///
    /// </summary>
    public readonly Type Type => typeof(ulong);
}

/// <summary>
///
/// </summary>
/// <param name="Min"></param>
/// <param name="Max"></param>
/// <param name="Prompt"></param>
/// <param name="Placeholder"></param>
/// <param name="Size"></param>
[Serializable, NetSerializable]
public readonly record struct QuickDialogEntryFloat(float Min, float Max, LocId? Prompt = null, LocId? Placeholder = null, Vector2? Size = null) :
    IQuickDialogEntry<float>
{
    /// <summary>
    ///
    /// </summary>
    public readonly Type Type => typeof(float);
}

/// <summary>
///
/// </summary>
/// <param name="Min"></param>
/// <param name="Max"></param>
/// <param name="Prompt"></param>
/// <param name="Placeholder"></param>
/// <param name="Size"></param>
[Serializable, NetSerializable]
public readonly record struct QuickDialogEntryDouble(double Min, double Max, LocId? Prompt = null, LocId? Placeholder = null, Vector2? Size = null) :
    IQuickDialogEntry<double>
{
    /// <summary>
    ///
    /// </summary>
    public readonly Type Type => typeof(double);
}
