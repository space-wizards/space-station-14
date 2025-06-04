using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid.Markings;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class MarkingPoints
{
    [DataField(required: true)]
    public int Points = 0;

    [DataField(required: true)]
    public bool Required;

    /// <summary>
    ///     If the user of this marking point set is only allowed to
    ///     use whitelisted markings, and not globally usable markings.
    ///     Only used for validation and profile construction. Ignored anywhere else.
    /// </summary>
    [DataField]
    public bool OnlyWhitelisted;

    // Default markings for this layer.
    [DataField]
    public List<ProtoId<MarkingPrototype>> DefaultMarkings = new();

    public static Dictionary<MarkingCategories, MarkingPoints> CloneMarkingPointDictionary(Dictionary<MarkingCategories, MarkingPoints> self)
    {
        var clone = new Dictionary<MarkingCategories, MarkingPoints>();

        foreach (var (category, points) in self)
        {
            clone[category] = new MarkingPoints()
            {
                Points = points.Points,
                Required = points.Required,
                OnlyWhitelisted = points.OnlyWhitelisted,
                DefaultMarkings = points.DefaultMarkings
            };
        }

        return clone;
    }
}

[Prototype]
public sealed partial class MarkingPointsPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    ///     If the user of this marking point set is only allowed to
    ///     use whitelisted markings, and not globally usable markings.
    ///     Only used for validation and profile construction. Ignored anywhere else.
    /// </summary>
    [DataField]
    public bool OnlyWhitelisted;

    [DataField(required: true)]
    public Dictionary<MarkingCategories, MarkingPoints> Points { get; private set; } = default!;
}
