using JetBrains.Annotations;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.EntityTable;

/// <summary>
/// Context used by selectors and conditions to evaluate in generic gamestate information.
/// </summary>
public sealed class EntityTableContext : IEnumerable
{
    private readonly Dictionary<string, object> _data = new();

    /// <summary>
    /// Retrieves an arbitrary piece of data from the context based on a provided key.
    /// </summary>
    /// <param name="key">A string key that corresponds to the value we are searching for. </param>
    /// <param name="value">The value we are trying to extract from the context object</param>
    /// <typeparam name="T">The type of <see cref="value"/> that we are trying to retrieve</typeparam>
    /// <returns>If <see cref="key"/> has a corresponding value of type <see cref="T"/></returns>
    [PublicAPI]
    public bool TryGetData<T>(EntityTableContextKey<T> key, [NotNullWhen(true)] out T? value)
    {
        value = default;
        if (!_data.TryGetValue(key.Key, out var valueData) || valueData is not T castValueData)
            return false;

        value = castValueData;
        return true;
    }

    /// <summary>
    /// Sets data into context using provided key.
    /// </summary>
    [PublicAPI]
    public void SetData<T>(EntityTableContextKey<T> key, T data) where T : notnull
    {
        _data[key.Key] = data;
    }

    /// <summary>
    /// Add method duplicates <see cref="SetData{T}"/>, is used for object initializer.
    /// </summary>
    /// <exception cref="ArgumentException ">Thrown if there is already a value stored under same key.</exception>
    [PublicAPI]
    public void Add<T>(EntityTableContextKey<T> key, T data) where T : notnull
    {
        _data.Add(key.Key, data);
    }

    /// <summary>
    /// Removes data from the context, if the key exists.
    /// </summary>
    [PublicAPI]
    public void RemoveData<T>(EntityTableContextKey<T> key)
    {
        _data.Remove(key.Key);
    }

    /// <inheritdoc/>
    public IEnumerator GetEnumerator()
    {
        return _data.GetEnumerator();
    }

    /// <summary>
    /// Structure that holds values that were temporarily added to <see cref="EntityTableContext"/>
    /// and should be removed on dispose call.
    /// </summary>
    /// <typeparam name="T">Type of value in context, for which we want to control lifetime.</typeparam>
    public readonly struct TemporarilyAddToContext<T> : IDisposable where T : notnull
    {
        private readonly EntityTableContext _context;
        private readonly EntityTableContextKey<T> _key;

        /// <summary>
        /// Creates marker struct that will add <paramref cref="value"/> to <paramref cref="context"/>
        /// immidiately, and will remove it during <see cref="Dispose"/> call.
        /// </summary>
        /// <param name="context">Context with which we want to control.</param>
        /// <param name="key">Key for storing and removing value.</param>
        /// <param name="value">Value to store now, and removed on disposal of this struct.</param>
        public TemporarilyAddToContext(EntityTableContext context, EntityTableContextKey<T> key, T value)
        {
            _context = context;
            _key = key;
            _context.SetData(_key, value);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _context.RemoveData(_key);
        }
    }
}

/// <summary>
/// Key for <see cref="EntityTableContext"/>, used to strongly type calls for setting and getting entities,
/// should usually be static readonly fields as anchors.
/// </summary>
/// <typeparam name="T">Type of value to store/extract to/from context.</typeparam>
/// <param name="Key">
/// String value under which data will be stored inside. They should not have collisions, otherwise data will be rewritten.
/// </param>
public readonly record struct EntityTableContextKey<T>(string Key);
