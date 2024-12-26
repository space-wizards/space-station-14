using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.StoryGen;

/// <summary>
/// Provides functionality to generate a story from a <see cref="StoryTemplatePrototype"/>.
/// </summary>
public sealed partial class StoryGeneratorSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Tries to generate a random story using the given template, picking a random word from the referenced
    /// datasets for each variable and passing them into the localization system with template.
    /// If <paramref name="seed"/> is specified, the randomizer will be seeded with it for consistent story generation;
    /// otherwise the variables will be randomized.
    /// Fails if the template prototype cannot be loaded.
    /// </summary>
    /// <returns>true if the template was loaded, otherwise false.</returns>
    public bool TryGenerateStoryFromTemplate(ProtoId<StoryTemplatePrototype> template, [NotNullWhen(true)] out string? story, int? seed = null)
    {
        // Get the story template prototype from the ID
        if (!_protoMan.TryIndex(template, out var templateProto))
        {
            story = null;
            return false;
        }

        // If given a seed, use it
        if (seed != null)
            _random.SetSeed(seed.Value);

        // Pick values for all of the variables in the template
        var variables = new ValueList<(string, object)>(templateProto.Variables.Count);
        foreach (var (name, list) in templateProto.Variables)
        {
            // Get the prototype for the world list dataset
            if (!_protoMan.TryIndex(list, out var listProto))
                continue; // Missed one, but keep going with the rest of the story

            // Pick a random word from the dataset and localize it
            var chosenWord = Loc.GetString(_random.Pick(listProto.Values));
            variables.Add((name, chosenWord));
        }

        // Pass the variables to the localization system and build the story
        story = Loc.GetString(templateProto.LocId, variables.ToArray());
        return true;
    }
}
