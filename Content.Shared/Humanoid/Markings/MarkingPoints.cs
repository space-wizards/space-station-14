using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Humanoid.Markings;

[DataDefinition]
[Serializable, NetSerializable]
public readonly record struct MarkingPoints
{
    [DataField("points", required: true)] public readonly int Points;
    [DataField("required", required: true)]
    public readonly bool Required;
    // Default markings for this layer.
    [DataField("defaultMarkings", customTypeSerializer:typeof(PrototypeIdListSerializer<MarkingPrototype>))]
    public readonly List<string> DefaultMarkings = new();

    public static Dictionary<MarkingCategories, MarkingPoints> CloneMarkingPointDictionary(Dictionary<MarkingCategories, MarkingPoints> self)
    {
        var clone = new Dictionary<MarkingCategories, MarkingPoints>();

        foreach (var (category, points) in self)
        {
            clone[category] = new MarkingPoints()
            {
                Points = points.Points,
                Required = points.Required,
                DefaultMarkings = points.DefaultMarkings
            };
        }

        return clone;
    }
}

[Prototype("markingPoints")]
public readonly record struct MarkingPointsPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    ///     If the user of this marking point set is only allowed to
    ///     use whitelisted markings, and not globally usable markings.
    ///     Only used for validation and profile construction. Ignored anywhere else.
    /// </summary>
    [DataField("onlyWhitelisted")] public readonly bool OnlyWhitelisted;

    [DataField("points", required: true)]
    public Dictionary<MarkingCategories, MarkingPoints> Points { get; } = default!;
}
