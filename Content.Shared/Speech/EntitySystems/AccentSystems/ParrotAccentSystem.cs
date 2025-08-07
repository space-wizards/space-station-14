using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.Speech.Components.AccentComponents;
using Robust.Shared.Random;

namespace Content.Shared.Speech.EntitySystems.AccentSystems;

public sealed class ParrotAccentSystem : AccentSystem<ParrotAccentComponent>
{
    private static readonly Regex WordCleanupRegex = new Regex("[^A-Za-z0-9 -]");

    [Dependency] private readonly IRobustRandom _random = default!;

    public override string Accentuate(Entity<ParrotAccentComponent>? entity, string message)
    {
        // TODO: Handle accentuating if no component is supplied
        if (entity == null)
            return message;

        // Sometimes repeat the longest word at the end of the message, after a squawk! SQUAWK! Sometimes!
        if (_random.Prob(entity.Value.Comp.LongestWordRepeatChance))
        {
            // Don't count non-alphanumeric characters as parts of words
            var cleaned = WordCleanupRegex.Replace(message, string.Empty);
            // Split on whitespace and favor words towards the end of the message
            var words = cleaned.Split(null).Reverse();
            // Find longest word
            var longest = words.MaxBy(word => word.Length);
            if (longest?.Length >= entity.Value.Comp.LongestWordMinLength)
            {
                message = EnsurePunctuation(message);

                // Capitalize the first letter of the repeated word
                longest = string.Concat(longest[0].ToString().ToUpper(), longest.AsSpan(1));

                message = string.Format("{0} {1} {2}!", message, GetRandomSquawk(entity.Value), longest);
                return message; // No more changes, or it's too much
            }
        }

        if (_random.Prob(entity.Value.Comp.SquawkPrefixChance))
        {
            // AWWK! Sometimes add a squawk at the begining of the message
            message = string.Format("{0} {1}", GetRandomSquawk(entity.Value), message);
        }
        else
        {
            // Otherwise add a squawk at the end of the message! RAWWK!
            message = EnsurePunctuation(message);
            message = string.Format("{0} {1}", message, GetRandomSquawk(entity.Value));
        }

        return message;
    }

    /// <summary>
    /// Adds a "!" to the end of the string, if there isn't already a sentence-ending punctuation mark.
    /// </summary>
    private string EnsurePunctuation(string message)
    {
        if (!message.EndsWith('!') && !message.EndsWith('?') && !message.EndsWith('.'))
            return message + '!';
        return message;
    }

    /// <summary>
    /// Returns a random, localized squawk sound.
    /// </summary>
    private string GetRandomSquawk(Entity<ParrotAccentComponent> entity)
    {
        return Loc.GetString(_random.Pick(entity.Comp.Squawks));
    }
}
