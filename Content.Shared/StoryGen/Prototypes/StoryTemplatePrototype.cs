using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared.StoryGen;

/// <summary>
/// Prototype for a story template that can be filled in with words chosen from <see cref="DatasetPrototype"/>s.
/// </summary>
[Serializable, Prototype("storyTemplate")]
public sealed partial class StoryTemplatePrototype : IPrototype
{
    /// <summary>
    /// Identifier for this prototype instance.
    /// </summary>
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Localization ID of the Fluent string that forms the structure of this story.
    /// </summary>
    [DataField(required: true)]
    public LocId LocId;

    /// <summary>
    /// Dictionary containing the name of each variable to pass to the template and the ID of the
    /// <see cref="LocalizedDatasetPrototype"/> from which a random entry will be selected as its value.
    /// For example, <c>name: book_character</c> will pick a random entry from the book_character
    /// dataset which can then be used in the template by <c>{$name}</c>.
    /// </summary>
    [DataField]
    public Dictionary<string, ProtoId<LocalizedDatasetPrototype>> Variables = [];
}
