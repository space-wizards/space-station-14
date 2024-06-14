using System.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Dataset;

/// <summary>
/// A variant of <see cref="DatasetPrototype"/> intended to specify a sequence of LocId strings
/// without having to copy-paste a ton of LocId strings into the YAML.
/// </summary>
[Prototype]
public sealed partial class LocalizedDatasetPrototype : IPrototype
{
    /// <summary>
    /// Identifier for this prototype.
    /// </summary>
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Collection of LocId strings.
    /// </summary>
    [DataField]
    public LocalizedDatasetValues Values { get; private set; } = [];
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class LocalizedDatasetValues : IReadOnlyList<string>
{
    /// <summary>
    /// String prepended to the index number to generate each LocId string.
    /// For example, a prefix of <c>tips-dataset-</c> will generate <c>tips-dataset-1</c>,
    /// <c>tips-dataset-2</c>, etc.
    /// </summary>
    [DataField(required: true)]
    public string Prefix { get; private set; } = default!;

    /// <summary>
    /// How many values are in the dataset.
    /// </summary>
    [DataField(required: true)]
    public int Count { get; private set; }

    public string this[int index]
    {
        get
        {
            if (index >= Count || index < 0)
                throw new IndexOutOfRangeException();
            return Prefix + (index + 1);
        }
    }

    public IEnumerator<string> GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public sealed class Enumerator : IEnumerator<string>
    {
        private int _index = 0; // Whee, 1-indexing

        private readonly LocalizedDatasetValues _values;

        public Enumerator(LocalizedDatasetValues values)
        {
            _values = values;
        }

        public string Current => _values.Prefix + _index;

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext()
        {
            _index++;
            return _index <= _values.Count;
        }

        public void Reset()
        {
            _index = 0;
        }
    }
}
